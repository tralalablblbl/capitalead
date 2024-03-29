using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Capitalead.Data;

namespace Capitalead.Services;

public class NoCrmService
{
    private readonly ILogger<NoCrmService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly AppDatabase _database;

    private const string PROSPECTING_LIST_TITLE = "Apartments";

    private const int MAX_ROW_NUMBER_PER_REQUEST = 100;
    private const int CLUSTER_ID_LENGTH = 32;
    private const int CLUSTER_INDEX_TAG_POSITION = 2;

    public const string NOCRM_API_URL = "https://capitalead26.nocrm.io/";
    private const string SPREADSHEETS_URL = "api/v2/spreadsheets";
    private const string ROWS_URL = "api/v2/rows";

    public NoCrmService(ILogger<NoCrmService> logger, IHttpClientFactory httpClientFactory,
        IConfiguration configuration, AppDatabase database)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _database = database;
    }

    public async Task<(long sheetId, JsonNode[] prospects)[]> UploadDataToCRM(JsonNode[] prospects, string clusterId, string clusterName, NoCrmSpreadsheet[] sheets)
    {
        _logger.LogInformation("Find unloaded data from cluster {ClusterId}, rows count {Count}", clusterId, prospects.Length);
        var unloadedProspects = prospects.ToList();
        var list = new List<(long sheetId, JsonNode[] prospects)>();
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
            list.Add((sheet.Id, toUpload));
        }

        var lastSheet = GetLastSheet();
        // Load data to new sheets
        while (unloadedProspects.Any())
        {
            var index = lastSheet.Tags.Length == 2 ? 0 : int.Parse(lastSheet.Tags[CLUSTER_INDEX_TAG_POSITION]);
            index++;
            var canUpload = 4999;
            var toUpload = unloadedProspects.Take(Math.Min(canUpload, unloadedProspects.Count)).ToArray();
            var sheet = await CreateNewProspectingList($"V3 - {clusterName} {index:000}",
                [clusterId, clusterName.Substring(0, Math.Min(clusterName.Length, 50)), index.ToString()], toUpload);
            unloadedProspects = unloadedProspects.Skip(toUpload.Length).ToList();
            lastSheet = sheet;
            list.Add((sheet.Id, toUpload));
            var dbSheet = new Spreadsheet()
            {
                Id = sheet.Id,
                ClusterId = clusterId,
                ClusterName = clusterName,
                Title = sheet.Title
            };
            await _database.Spreadsheets.AddAsync(dbSheet);
        }

        return list.ToArray();

        NoCrmSpreadsheet GetLastSheet()
        {
            return sheets.OrderByDescending(s => s.Tags.Length == 2 ? 0 : int.Parse(s.Tags[CLUSTER_INDEX_TAG_POSITION])).First();
        }
    }

    public async Task<NoCrmSpreadsheet> CreateNewProspectingList(string listTitle, string[] tags, JsonNode[]? prospects)
    {
        _logger.LogInformation("Creating new prospecting list {ListTitle}", listTitle);
        var body = Helper.BuildJsonBodyForCreatingProspList(listTitle, tags, _configuration["nocrm_user_email"], prospects);
        return await CreateProspectingList(body);
    }

    public async Task<IDictionary<long, (NoCrmSpreadsheet sheet, string clusterId)>> ListTheProspectingLists()
    {
        var client = GetClient();
        var response = await client.GetAsync($"{SPREADSHEETS_URL}?limit=1000");

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully listed all prospecting lists!");
            var sheets = await response.Content.ReadFromJsonAsync<NoCrmSpreadsheet[]>() ?? throw new ArgumentNullException();
            var listsNames = new Dictionary<long, (NoCrmSpreadsheet sheet, string clusterId)>();
            foreach (var sheet in sheets)
            {
                var clusterIdTag = sheet.Tags.FirstOrDefault();
                // new sheets has index tag
                if (sheet.Tags.Length >= 3 && clusterIdTag?.Length == CLUSTER_ID_LENGTH && !clusterIdTag.Contains(" "))
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

    public async Task<IDictionary<long, (NoCrmSpreadsheet sheet, string clusterId)>> ListTheProspectingListsToMigrate()
    {
        var client = GetClient();
        var response = await client.GetAsync($"{SPREADSHEETS_URL}?limit=1000");

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully listed all prospecting lists ToMigrate!");
            var sheets = await response.Content.ReadFromJsonAsync<NoCrmSpreadsheet[]>() ?? throw new ArgumentNullException();
            var listsNames = new Dictionary<long, (NoCrmSpreadsheet sheet, string clusterId)>();
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

    public async Task<NoCrmSpreadsheet> RetrieveTheProspectingList(long listId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"{SPREADSHEETS_URL}/{listId}");
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully retrieve prospecting list {ListId}!", listId);
            return await response.Content.ReadFromJsonAsync<NoCrmSpreadsheet>() ?? throw new ArgumentNullException();
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

    private async Task<NoCrmSpreadsheet> CreateProspectingList(JsonObject body)
    {
        var client = GetClient();

        var response = await client.PostAsJsonAsync(SPREADSHEETS_URL, body);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully created new prospecting list!");
            return await response.Content.ReadFromJsonAsync<NoCrmSpreadsheet>() ?? throw new ArgumentNullException();
        }
        _logger.LogError("Error occurred while creating prospecting list {List}! Body: {Body}, Error: {Error}",
            PROSPECTING_LIST_TITLE, body.ToJsonString(), await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();
        throw new NotSupportedException();
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(nameof(NoCrmService));
}

public record NoCrmSpreadsheet(
    long Id,
    string[] Tags,
    string Title,
    [property: JsonPropertyName("spreadsheet_rows")] NoCrmProspect[]? SpreadsheetRows,
    [property: JsonPropertyName("column_names")] string[] ColumnNames,
    [property: JsonPropertyName("user")] NoCrmUser User,
    [property: JsonPropertyName("total_row_count")] long TotalRowCount);
public record NoCrmProspect(
    long Id,
    [property: JsonPropertyName("is_active")]bool IsActive,
    [property: JsonPropertyName("lead_id")]long? LeadId,
    JsonNode[] Content,
    [property: JsonPropertyName("spreadsheet_id")] long? SpreadsheetId);
public record NoCrmUser(
    long Id,
    [property: JsonPropertyName("lastname")]string Lastname,
    [property: JsonPropertyName("firstname")]string Firstname,
    [property: JsonPropertyName("email")]string Email,
    [property: JsonPropertyName("phone")]string Phone,
    [property: JsonPropertyName("mobile_phone")]string MobilePhone);