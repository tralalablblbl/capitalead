using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Capitalead.Services;

public class NoCrmService
{
    private readonly ILogger<NoCrmService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    private const string PROSPECTING_LIST_TITLE = "Apartments";

    private const int MAX_ROW_NUMBER_PER_REQUEST = 100;
    private const int CLUSTER_ID_LENGTH = 32;
    private const int CLUSTER_INDEX_TAG_POSITION = 2;

    public const string NOCRM_API_URL = "https://capitalead26.nocrm.io/";
    private const string SPREADSHEETS_URL = "api/v2/spreadsheets";
    private const string ROWS_URL = "api/v2/rows";

    public NoCrmService(ILogger<NoCrmService> logger, IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task UploadDataToCRM(JsonNode[] prospects, string clusterId, Spreadsheet[] sheets)
    {
        _logger.LogInformation("Find unloaded data from cluster {ClusterId}, rows count {Count}", clusterId, prospects.Length);
        var unloadedProspects = prospects.ToList();
        // Load data to existing sheets
        foreach (var sheet in sheets)
        {
            if (unloadedProspects.Count <= 0)
                break;
            if (sheet.TotalRowCount >= 4999)
                continue;
            var canUpload = 4999 - sheet.TotalRowCount;
            var toUpload = unloadedProspects.Take((int)Math.Min(canUpload, unloadedProspects.Count)).ToArray();
            await UploadToSheet(toUpload, sheet.Id);
            unloadedProspects = unloadedProspects.Skip(toUpload.Length).ToList();
        }

        var lastSheet = GetLastSheet();
        // Load data to new sheets
        while (unloadedProspects.Any())
        {
            var index = lastSheet.Tags.Length == 2 ? 0 : int.Parse(lastSheet.Tags[CLUSTER_INDEX_TAG_POSITION]);
            index++;
            var canUpload = 4999;
            var title = lastSheet.Tags[1];
            var toUpload = unloadedProspects.Take((int)Math.Min(canUpload, unloadedProspects.Count)).ToArray();
            var sheet = await CreateNewProspectingList($"V3 - {title} {index:000}", new string[] { clusterId, title, index.ToString() }, toUpload);
            unloadedProspects = unloadedProspects.Skip(toUpload.Length).ToList();
            lastSheet = sheet;
        }

        Spreadsheet GetLastSheet()
        {
            return sheets.OrderByDescending(s => s.Tags.Length == 2 ? 0 : int.Parse(s.Tags[CLUSTER_INDEX_TAG_POSITION])).First();
        }
    }

    public async Task<Spreadsheet> CreateNewProspectingList(string listTitle, string[] tags, JsonNode[]? prospects)
    {
        _logger.LogInformation("Creating new prospecting list {ListTitle}", listTitle);
        var body = Helper.BuildJsonBodyForCreatingProspList(listTitle, tags, _configuration["nocrm_user_email"], prospects);
        return await CreateProspectingList(body);
    }

    public async Task<IDictionary<long, (Spreadsheet sheet, string clusterId)>> ListTheProspectingLists()
    {
        var client = GetClient();
        var response = await client.GetAsync($"{SPREADSHEETS_URL}?limit=1000");

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully listed all prospecting lists!");
            var sheets = await response.Content.ReadFromJsonAsync<Spreadsheet[]>() ?? throw new ArgumentNullException();
            var listsNames = new Dictionary<long, (Spreadsheet sheet, string clusterId)>();
            foreach (var sheet in sheets)
            {
                var clusterIdTag = sheet.Tags.FirstOrDefault();
                // new sheets has index tag
                if (sheet.Tags.Length == 3 && clusterIdTag?.Length == CLUSTER_ID_LENGTH && !clusterIdTag.Contains(" "))
                {
                    listsNames.Add(sheet.Id, (sheet, clusterIdTag));
                }
            }

            return listsNames;
        }

        _logger.LogError("Error occurred while listing all prospecting lists!");
        throw new ApplicationException(
            $"Error occurred while listing all prospecting lists!, status: {response.StatusCode}, error: {await response.Content.ReadAsStringAsync()}");
    }

    public async Task<IDictionary<long, (Spreadsheet sheet, string clusterId)>> ListTheProspectingListsToMigrate()
    {
        var client = GetClient();
        var response = await client.GetAsync($"{SPREADSHEETS_URL}?limit=1000");

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully listed all prospecting lists ToMigrate!");
            var sheets = await response.Content.ReadFromJsonAsync<Spreadsheet[]>() ?? throw new ArgumentNullException();
            var listsNames = new Dictionary<long, (Spreadsheet sheet, string clusterId)>();
            foreach (var sheet in sheets)
            {
                var clusterIdTag = sheet.Tags.FirstOrDefault();
                // Old sheet without index tag
                if (sheet.Tags.Length == 2 && clusterIdTag?.Length == CLUSTER_ID_LENGTH && !clusterIdTag.Contains(" "))
                {
                    listsNames.Add(sheet.Id, (sheet, clusterIdTag));
                }
            }

            return listsNames;
        }

        _logger.LogError("Error occurred while listing all prospecting lists ToMigrate!");
        throw new ApplicationException(
            $"Error occurred while listing all prospecting lists ToMigrate!, status: {response.StatusCode}, error: {await response.Content.ReadAsStringAsync()}");
    }

    public async Task<Spreadsheet> RetrieveTheProspectingList(long listId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"{SPREADSHEETS_URL}/{listId}");
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully retrieve prospecting list {ListId}!", listId);
            return await response.Content.ReadFromJsonAsync<Spreadsheet>() ?? throw new ArgumentNullException();
        }
        else
        {
            _logger.LogError("Error occurred while retrieving prospecting list {ListId}!", listId);
            throw new ApplicationException(
                $"Error occurred while retrieving prospecting list  {listId} !, status: {response.StatusCode}, error: {await response.Content.ReadAsStringAsync()}");
        }
    }

    public async Task<NoCrmProspect[]> RetrieveProspectDuplicates(string phone, string fieldName)
    {
        var client = GetClient();
        var response = await client.GetAsync($"{ROWS_URL}?field_key={fieldName}&field_value={phone}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<NoCrmProspect[]>() ?? throw new ArgumentNullException();
        }
        else
        {
            _logger.LogError("Error occurred while retrieving duplicate prospects for phone {Phone}", phone);
            throw new ApplicationException(
                $"Error occurred while retrieving duplicate prospects for phone {phone} !, status: {response.StatusCode}, error: {await response.Content.ReadAsStringAsync()}");
        }
    }

    public async Task<bool> DeleteProspectInList(string listId, long prospectId)
    {
        var client = GetClient();
        var response = await client.DeleteAsync($"{SPREADSHEETS_URL}/{listId}/rows/{prospectId}");
        response.EnsureSuccessStatusCode();
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully deleted prospect {ProspectId} in list {ListId}", prospectId, listId);
            return true;
        }
        else
        {
            _logger.LogError("Error occurred while delete prospect {ProspectId} in list {ListId}", prospectId, listId);
            return false;
        }
    }

    private async Task UploadToSheet(JsonNode[] prospects, long sheetId)
    {
        //return;
        if (!prospects.Any())
            return;

        foreach (var chunk in prospects.Chunk(MAX_ROW_NUMBER_PER_REQUEST))
        {
            JsonNode jsonObject = new JsonObject();
            jsonObject["content"] = new JsonArray(chunk);
            await UploadData(jsonObject, sheetId);
        }
    }

    private async Task UploadData(JsonNode body, long listId)
    {
        var client = GetClient();

        try
        {
            var response = await client.PostAsJsonAsync($"{SPREADSHEETS_URL}/{listId}/rows", body);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully uploaded runs/partition of runs to prospecting list with id {ListId}", listId);
            }
            else
            {
                _logger.LogError(
                    "Error occurred while uploading runs/partition of runs to prospecting list with id {ListId}! {Error}",
                    listId, await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while uploading runs/partition of runs to prospecting list with id {ListId}!", listId);
        }
    }

    private async Task<Spreadsheet> CreateProspectingList(JsonObject body)
    {
        // return new Spreadsheet(1999999, body["tags"].AsArray().Select(t => t.ToString()).ToArray(),
        //     body["title"]!.ToString(), null,
        //     new[] { "Neighborhood", "Parsing Date", "Type", "Téléphone", "Rooms", "Size", "Energy" }, 0);
        var client = GetClient();

        var response = await client.PostAsJsonAsync(SPREADSHEETS_URL, body);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully created new prospecting list!");
            return await response.Content.ReadFromJsonAsync<Spreadsheet>() ?? throw new ArgumentNullException();
        }
        _logger.LogError("Error occurred while creating prospecting list {List}! Body: {Body}, Error: {Error}",
            PROSPECTING_LIST_TITLE, body.ToJsonString(), await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();
        throw new NotSupportedException();
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(nameof(NoCrmService));
}

public record Spreadsheet(
    long Id,
    string[] Tags,
    string Title,
    [property: JsonPropertyName("spreadsheet_rows")] NoCrmProspect[]? SpreadsheetRows,
    [property: JsonPropertyName("column_names")] string[] ColumnNames,
    [property: JsonPropertyName("total_row_count")] long TotalRowCount);
public record NoCrmProspect(
    long Id,
    [property: JsonPropertyName("is_active")]bool IsActive,
    [property: JsonPropertyName("is_archived")]bool IsArchived,
    JsonNode[] Content,
    [property: JsonPropertyName("spreadsheet_id")] long? SpreadsheetId);