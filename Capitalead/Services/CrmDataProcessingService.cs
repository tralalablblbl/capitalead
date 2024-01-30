using System.Globalization;
using System.Text.Json.Nodes;
using Capitalead.Data;
using Microsoft.EntityFrameworkCore;

namespace Capitalead.Services;

public class CrmDataProcessingService
{
    private const int CLUSTER_ID_TAG_POSITION = 0;
    private const int TELEPHONE_FIELD_POSITION = 3;

    private readonly LobstrService _lobstrService;
    private readonly NoCRMService _crmService;
    private readonly AppDatabase _database;
    private readonly ILogger<CrmDataProcessingService> _logger;

    public CrmDataProcessingService(LobstrService lobstrService, NoCRMService crmService,
        AppDatabase database, ILogger<CrmDataProcessingService> logger)
    {
        _lobstrService = lobstrService;
        _crmService = crmService;
        _database = database;
        _logger = logger;
    }

    public async Task Run(string listId)
    {
        _logger.LogDebug("Thread with listID {ListId} starting", listId);

        var unloadedApartments = await FindUnloadedClusterDataForList(listId);

        if (unloadedApartments.Any())
        {
            await _crmService.UploadDataToCRM(unloadedApartments, listId);
            await SaveApartmentsToDatabase(unloadedApartments, listId);
            _logger.LogInformation("List {ListId} saved", listId);
        }
        else
        {
            _logger.LogInformation("New data for list {ListId} not found", listId);
        }
    }

    private async Task<JsonArray> FindUnloadedClusterDataForList(String crmListId)
    {
        var unloadedData = new JsonArray();
        var dataSet = new HashSet<string>();
        var prospectingListData = await _crmService.RetrieveTheProspectingList(crmListId);
        foreach (var json in prospectingListData["spreadsheet_rows"].AsArray())
        {
            dataSet.Add(json["content"].AsArray()[TELEPHONE_FIELD_POSITION].GetValue<string>());
        }

        var listTagClusterId = prospectingListData["tags"].AsArray()[CLUSTER_ID_TAG_POSITION].GetValue<string>();

        var runIds = await _lobstrService.GetRunsFromCluster(listTagClusterId);
        foreach (var runId in runIds)
        {
            try
            {
                var records = await _lobstrService.GetRecordsFromRun(runId);

                foreach (var json in records)
                {
                    var phone = json["phone"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(phone))
                    {
                        continue;
                    }

                    if (await _database.Prospects.AnyAsync(u => u.Phone == phone))
                    {
                        continue;
                    }

                    unloadedData.Add(json);

                }
            }
            catch (Exception e)
            {
                // Handle the error, e.g., log it, but continue with the next run
                _logger.LogError(e, "Error processing run: {RunId}, \nMaybe run is not valid", runId);
            }
        }

        _logger.LogInformation("return data");
        return Helper.TransformJSON(unloadedData);
    }

    private async Task SaveApartmentsToDatabase(JsonArray unloadedApartments, string listId)
    {
        _logger.LogInformation("Start saving unloaded data from list {ListId} in database", listId);
        foreach (var chunk in unloadedApartments.Chunk(200))
        {
            var prospects = new List<Prospect>();

            foreach (var node in chunk)
            {
                var apart = node.AsArray();
                var date = DateTime.ParseExact(apart[1].GetValue<string>(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var apartment = new Prospect();
                apartment.Neighbourhood = apart[0].GetValue<string>();
                apartment.ParsingDate = date;
                apartment.RealEstateType = apart[2].GetValue<string>();
                apartment.Phone = apart[3].GetValue<string>();
                apartment.Rooms = apart[4].GetValue<string>();
                apartment.Size = apart[5].GetValue<string>();
                apartment.Energy = apart[6].GetValue<string>();
                prospects.Add(apartment);
            }


            await _database.Prospects.AddRangeAsync(prospects);
            _logger.LogInformation("Successfully uploaded slice of data in database");
        }
    }
}