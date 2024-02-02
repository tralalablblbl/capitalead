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

    public async Task UploadDataToCRM(JsonNode[] apartments, string listId)
    {
        _logger.LogInformation("Find unloaded data for list {ListId}, rows count {Count}", listId, apartments.Length);
        foreach (var chunk in apartments.Chunk(MAX_ROW_NUMBER_PER_REQUEST))
        {
            JsonNode jsonObject = new JsonObject();
            jsonObject["content"] = new JsonArray(chunk);
            await UploadData(jsonObject, listId);
        }
    }

    public async Task<bool> CreateNewProspectingList(string listTitle, string[] tags)
    {
        _logger.LogInformation("Creating new prospecting list {ListTitle}", listTitle);
        var body = Helper.BuildJsonBodyForCreatingProspList(listTitle, tags, _configuration["nocrm-user-email"]);
        return await CreateProspectingList(body);
    }

    public async Task<IDictionary<string, string>> ListTheProspectingLists()
    {
        var client = GetClient();
        var response = await client.GetAsync($"{SPREADSHEETS_URL}?limit=1000");
        response.EnsureSuccessStatusCode();
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully listed all prospecting lists!");
            var sheets = await response.Content.ReadFromJsonAsync<Spreadsheet[]>() ?? throw new ArgumentNullException();
            var listsNames = new Dictionary<string, string>();
            foreach (var sheet in sheets)
            {
                var clusterIdTag = sheet.Tags.FirstOrDefault();
                if (clusterIdTag?.Length == CLUSTER_ID_LENGTH && !clusterIdTag.Contains(" "))
                {
                    listsNames.Add(sheet.Id.ToString(), clusterIdTag);
                }
            }

            return listsNames;
        }

        _logger.LogError("Error occurred while listing all prospecting lists!");
        throw new ApplicationException(
            $"Error occurred while listing all prospecting lists!, status: {response.StatusCode}, error: {await response.Content.ReadAsStringAsync()}");
    }

    public async Task<Spreadsheet> RetrieveTheProspectingList(string listId)
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

    private async Task UploadData(JsonNode body, string listId)
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

    private async Task<bool> CreateProspectingList(JsonObject body)
    {
        var client = GetClient();

        try
        {
            var response = await client.PostAsJsonAsync(SPREADSHEETS_URL, body);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully created new prospecting list!");
                return true;
            }
            else
            {
                _logger.LogError("Error occurred while creating prospecting list {List}! Body: {Body}, Error: {Error}",
                    PROSPECTING_LIST_TITLE, body.ToJsonString(), await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating prospecting list {List}! Body: {Body}",
                PROSPECTING_LIST_TITLE, body.ToJsonString());
        }

        return false;
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(nameof(NoCrmService));
}

public record Spreadsheet(long Id, string[] Tags, string Title, [property: JsonPropertyName("spreadsheet_rows")] NoCrmProspect[]? SpreadsheetRows, [property: JsonPropertyName("column_names")] string[] ColumnNames);
public record NoCrmProspect(long Id, [property: JsonPropertyName("is_active")]bool IsActive, [property: JsonPropertyName("is_archived")]bool IsArchived, JsonNode[] Content, [property: JsonPropertyName("spreadsheet_id")] long? SpreadsheetId);