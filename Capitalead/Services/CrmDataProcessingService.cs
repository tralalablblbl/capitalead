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
    private readonly NoCrmService _crmService;
    private readonly AppDatabase _database;
    private readonly ILogger<CrmDataProcessingService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CrmDataProcessingService(LobstrService lobstrService, NoCrmService crmService, AppDatabase database,
        ILogger<CrmDataProcessingService> logger, IServiceProvider serviceProvider)
    {
        _lobstrService = lobstrService;
        _crmService = crmService;
        _database = database;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task RunMigration(string clusterId, Spreadsheet[] sheets)
    {
        _logger.LogDebug("Thread with clusterId {ClusterId} starting", clusterId);

        var unloadedApartments = await FindUnloadedClusterDataForList(clusterId);

        if (unloadedApartments.Any())
        {
            await _crmService.UploadDataToCRM(unloadedApartments, clusterId, sheets);
            await SaveApartmentsToDatabase(unloadedApartments, clusterId);
            _logger.LogInformation("Cluster {ClusterId} stored to nocrm", clusterId);
        }
        else
        {
            _logger.LogInformation("New data from cluster {ClusterId} not found", clusterId);
        }
    }

    private async Task<JsonNode[]> FindUnloadedClusterDataForList(string clusterId)
    {
        var unloadedData = new Dictionary<string, JsonNode>();

        var runIds = await _lobstrService.GetRunsFromCluster(clusterId);
        var dbRuns = await _database.ProcessedRuns.Where(r => runIds.Contains(r.RunId)).Select(r => r.RunId)
            .ToArrayAsync();
        var newRuns = runIds.Except(dbRuns).ToArray();

        //newRuns = newRuns.Take(3).ToArray();

        var newProcessedRuns = new List<ProcessedRun>();
        foreach (var runId in newRuns)
        {
            try
            {
                var records = await _lobstrService.GetRecordsFromRun(runId);
                var clearDataCount = 0;
                foreach (var chunk in records.Chunk(200))
                {
                    var runData = new Dictionary<string, JsonNode>();
                    foreach (var json in records)
                    {
                        var phone = Helper.GetPhone(json["phone"]?.GetValue<string>());
                        if (string.IsNullOrEmpty(phone))
                        {
                            continue;
                        }
 
                        runData.TryAdd(phone, json);
                    }

                    var allPhones = runData.Keys;
                    var dbPhones = await _database.Prospects
                        .Where(u => allPhones.Contains(u.Phone))
                        .Select(u => u.Phone)
                        .Distinct()
                        .ToDictionaryAsync(u => u);
                    var clearData = runData
                        .Where(kv => !dbPhones.ContainsKey(kv.Key))
                        .ToList();
                    foreach (var pair in clearData)
                    {
                        unloadedData.TryAdd(pair.Key, pair.Value);
                    }

                    clearDataCount += clearData.Count;
                }

                newProcessedRuns.Add(new ProcessedRun()
                {
                    Id = Guid.NewGuid(),
                    ProcessedDate = DateTime.UtcNow,
                    ProspectsCount = clearDataCount,
                    RunId = runId
                });
            }
            catch (Exception e)
            {
                // Handle the error, e.g., log it, but continue with the next run
                _logger.LogError(e, "Error processing run: {RunId}, \nMaybe run is not valid", runId);
            }
        }

        await _database.ProcessedRuns.AddRangeAsync(newProcessedRuns);
        await _database.SaveChangesAsync();

        _logger.LogInformation("return data");
        return Helper.TransformJSON(unloadedData.Values.ToArray());
    }

    private async Task SaveApartmentsToDatabase(JsonNode[] unloadedApartments, string clusterId)
    {
        _logger.LogInformation("Start saving unloaded data from cluster {ClusterId} in database", clusterId);
        foreach (var chunk in unloadedApartments.Chunk(200))
        {
            var prospects = new List<Prospect>();

            foreach (var node in chunk)
            {
                var apart = node.AsArray();
                var date = apart[1].GetValue<DateTime?>() ?? DateTime.UtcNow;
                var apartment = new Prospect();
                apartment.Id = Guid.NewGuid();
                apartment.Neighbourhood = apart[0].GetValue<string>();
                apartment.ParsingDate = date;
                apartment.RealEstateType = apart[2].GetValue<string>();
                apartment.Phone = apart[3].GetValue<string>();
                apartment.Rooms = apart[4].ToString();
                apartment.Size = apart[5].ToString();
                apartment.Energy = apart[6].GetValue<string>();
                prospects.Add(apartment);
            }


            await _database.Prospects.AddRangeAsync(prospects);
            await _database.SaveChangesAsync();
            _logger.LogInformation("Successfully uploaded slice of data in database");
        }
    }

    public async Task FindDuplicates(IDictionary<long, (Spreadsheet sheet, string clusterId)> allList)
    {
        var allSheets = new Dictionary<long, Spreadsheet>();
        foreach (var listId in allList.Keys)
        {
            var sheet = await _crmService.RetrieveTheProspectingList(listId);
            allSheets.Add(sheet.Id, sheet);
        }

        var groupedByPhone = allSheets.Values
            .Where(s => s.SpreadsheetRows?.Any() == true)
            .SelectMany(s => s.SpreadsheetRows?.Select(p => p with {SpreadsheetId = s.Id}) ?? new List<NoCrmProspect>())
            .GroupBy(prospect =>
            {
                var phone = prospect.Content[TELEPHONE_FIELD_POSITION]?.ToString() ?? string.Empty;
                return phone;
            })
            .Chunk(100)
            .ToList();
        await Parallel.ForEachAsync(groupedByPhone, async (groups, token) =>
        {
            var newProspects = new List<Prospect>();
            var duplicates = new List<DuplicateProspect>();
            foreach (var group in groups)
            {
                var phone = group.Key;
                var prospect = group.First();
                foreach (var duplicate in group.Skip(1))
                {
                    var duplicatePhone =
                        duplicate.Content[TELEPHONE_FIELD_POSITION]?.ToString() ?? string.Empty;
                    if (duplicate.Id == prospect.Id || duplicatePhone != phone)
                        continue;
                    duplicates.Add(new DuplicateProspect()
                    {
                        Id = Guid.NewGuid(),
                        Phone = phone,
                        Content = duplicate.Content.Select(c => c?.ToString() ?? string.Empty).ToArray(),
                        Deleted = false,
                        SheetId = duplicate.SpreadsheetId ?? 0,
                        ProspectId = duplicate.Id
                    });
                }

                var parsingDate = DateTime.TryParseExact(prospect.Content[1].ToString(), "dd/MM/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
                    ? date.ToUniversalTime()
                    : DateTime.UtcNow;
                var apartment = new Prospect();
                apartment.Id = Guid.NewGuid();
                apartment.Neighbourhood = prospect.Content[0]?.ToString() ?? string.Empty;
                apartment.ParsingDate = parsingDate;
                apartment.RealEstateType = prospect.Content[2]?.ToString() ?? string.Empty;
                apartment.Phone = phone;
                apartment.Rooms = prospect.Content[4]?.ToString() ?? string.Empty;
                apartment.Size = prospect.Content[5]?.ToString() ?? string.Empty;
                apartment.Energy = prospect.Content[6]?.ToString() ?? string.Empty;
                newProspects.Add(apartment);
            }

            using var scope = _serviceProvider.CreateScope();
            await using var database = scope.ServiceProvider.GetRequiredService<AppDatabase>();
            var allPhones = newProspects.Select(p => p.Phone).ToArray();
            var dbPhones = await database.Prospects
                .Where(u => allPhones.Contains(u.Phone))
                .Select(u => u.Phone)
                .Distinct()
                .ToDictionaryAsync(u => u);
            var clearData = newProspects
                .Where(p => !dbPhones.ContainsKey(p.Phone))
                .ToList();
            var updated = false;
            if (clearData.Any())
            {
                await database.Prospects.AddRangeAsync(clearData, token);
                updated = true;
            }

            if (duplicates.Any())
            {
                await database.DuplicateProspects.AddRangeAsync(duplicates, token);
                updated = true;
            }

            if (updated)
            {
                await database.SaveChangesAsync(token);
            }
        });
    }

    public async Task MigrateSheets(IDictionary<long, (Spreadsheet sheet, string clusterId)> allList)
    {
        foreach (var listId in allList.Keys)
        {
            using var scope = _serviceProvider.CreateScope();
            await using var database = scope.ServiceProvider.GetRequiredService<AppDatabase>();
            var oldSheet = await _crmService.RetrieveTheProspectingList(listId);
            var clusterId = oldSheet.Tags.First();
            var prospects = oldSheet.SpreadsheetRows ?? Array.Empty<NoCrmProspect>();
            var newSheet = await _crmService.CreateNewProspectingList($"V3 - {oldSheet.Title} 001", new string[] {clusterId , oldSheet.Title, "1" }, null);
            if (!prospects.Any())
            {
                _logger.LogInformation("Shpeadsheet {Title} with id {ListId} was migrated to {NewTitle}", oldSheet.Title, oldSheet.Id, newSheet.Title);
                continue;
            }
            var duplicatesId = new HashSet<long>();
            var prospectIds = prospects.Select(p => p.Id).ToList();
            foreach (var chunk in prospectIds.Chunk(200))
            {
                var dbDuplicates = await database.DuplicateProspects
                    .Where(u => chunk.Contains(u.ProspectId))
                    .Select(u => u.ProspectId)
                    .Distinct()
                    .ToDictionaryAsync(u => u);

                foreach (var prospectId in dbDuplicates.Keys)
                {
                    duplicatesId.Add(prospectId);
                }
            }

            var clearProspects = prospects.Where(p => !duplicatesId.Contains(p.Id)).ToList();
            if (clearProspects.Any())
            {
                var array = clearProspects.Select(p => (JsonNode)new JsonArray(p.Content)).ToArray();
                await _crmService.UploadDataToCRM(array, clusterId, [newSheet]); 
            }

            foreach (var ids in duplicatesId.Chunk(200))
            {
                await database.DuplicateProspects
                    .Where(p => ids.Contains(p.ProspectId))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.Deleted, true));
                await database.SaveChangesAsync();
            }
            _logger.LogInformation("Spreadsheet {Title} with id {ListId} was migrated to new lists without duplicates. Migrated count: {MigratedCount}, deleted duplicates: {DuplicatesCount}", oldSheet.Title, oldSheet.Id, clearProspects.Count, duplicatesId.Count);
        }
    }
}