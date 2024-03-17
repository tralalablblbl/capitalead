using System.Net;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Capitalead.Services;

public class LobstrService
{
    private ILogger<LobstrService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string LOBSTR_GET_RESULT_URL = "v1/results";

    private const string LOBSTR_LIST_CLUSTERS_URL = "v1/clusters";

    private const string LOBSTR_LIST_RUNS_URL = "v1/runs";
    public const string LOBSTR_BASE_URL = "https://api.lobstr.io/";

    public LobstrService(ILogger<LobstrService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string[]> GetRunsFromCluster(string clusterId)
    {
        var runIds = await ListRunsByClusterId(clusterId);

        _logger.LogInformation("Successfully fetch all runs from cluster {ClusterId} runs count {Count}", clusterId,
            runIds.Length);
        return runIds;
    }

    public async Task<IDictionary<string, string>> GetClusterIdsAndNames()
    {
        var clusters = await ListClusters();

        _logger.LogInformation("Successfully fetch clusters id. Size: {Size}", clusters.Count);
        return clusters;
    }

    public async Task<JsonNode[]> GetRecordsFromRun(string runId)
    {
        var records = await ListAllDataFromRun(runId);

        _logger.LogInformation("Successfully fetch all data from run {RunId} size {Size}", runId, records.Length);
        return records;
    }

    private async Task<JsonNode[]> ListAllDataFromRun(string runId)
    {
        var client = GetClient();

        try
        {
            var result = new List<JsonNode>();
            var page = 0;
            long totalPages;
            do
            {
                page++;
                var response = await client.GetAsync($"{LOBSTR_GET_RESULT_URL}?page={page}&run={runId}&page_size=1000000");
                response.EnsureSuccessStatusCode();
                var runs = await response.Content.ReadFromJsonAsync<ListData<JsonNode>>() ??
                           throw new ArgumentNullException();
                totalPages = runs.TotalPages ?? 0;
                result.AddRange(runs.Data);
            } while (page < totalPages);

            return result.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while request clusters list");
            return Array.Empty<JsonNode>();
        }
    }

    private async Task<IDictionary<string, string>> ListClusters()
    {
        var client = GetClient();

        try
        {
            var response = await client.GetAsync(LOBSTR_LIST_CLUSTERS_URL);
            response.EnsureSuccessStatusCode();
            var clusters = await response.Content.ReadFromJsonAsync<ListData<Cluster>>() ??
                       throw new ArgumentNullException();
            return clusters.Data.Where(c => c.IsActive).ToDictionary(r => r.Id, r => r.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while request clusters list");
            return new Dictionary<string, string>();
        }
    }

    public async Task<Cluster?> GetCluster(string clusterId)
    {
        var client = GetClient();

        var response = await client.GetAsync($"{LOBSTR_LIST_CLUSTERS_URL}/{clusterId}");
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorsModel>() ??
                throw new ArgumentNullException();
            if (error.Errors.Type == "ClusterDoesNotExistException")
                return null;
        }
        response.EnsureSuccessStatusCode();
        var cluster = await response.Content.ReadFromJsonAsync<Cluster>() ??
                   throw new ArgumentNullException();
        return cluster;
    }

    private async Task<string[]> ListRunsByClusterId(string id)
    {
        var client = GetClient();

        try
        {
            var result = new List<string>();
            var page = 0;
            long totalPages = 0;
            do
            {
                page++;
                var response = await client.GetAsync($"{LOBSTR_LIST_RUNS_URL}?cluster={id}&limit=120&page={page}");
                response.EnsureSuccessStatusCode();
                var runs = await response.Content.ReadFromJsonAsync<ListData<Run>>() ??
                           throw new ArgumentNullException();
                totalPages = runs.TotalPages ?? 0;
                var ids = runs.Data.Where(r => r.Status == "done").Select(r => r.Id).ToArray();
                result.AddRange(ids);
            } while (page < totalPages);

            return result.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while request runs by cluster id {Id}! List with this id does not exist", id);
            return Array.Empty<string>();
        }
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(nameof(LobstrService));
}

public record Run(string Id, string Status);
public record Cluster(string Id, string Name, [property: JsonPropertyName("is_active")] bool IsActive);
public record ListData<T>(long Count, long Page, long Limit, T[] Data, [property: JsonPropertyName("total_pages")] long? TotalPages);
public record ErrorsModel(ErrorModel Errors);
public record ErrorModel(string Message, string Type, int Code);