using System.Text.Json.Nodes;

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
            var hasMoreData = false;
            do
            {
                page++;
                var response = await client.GetAsync($"{LOBSTR_GET_RESULT_URL}?page={page}&run={runId}&page_size=400");
                response.EnsureSuccessStatusCode();
                var runs = await response.Content.ReadFromJsonAsync<ListData<JsonNode>>() ??
                           throw new ArgumentNullException();
                result.AddRange(runs.Data);
                hasMoreData = result.Count < runs.Count;
            } while (hasMoreData);

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
            var runs = await response.Content.ReadFromJsonAsync<ListData<Cluster>>() ??
                       throw new ArgumentNullException();
            return runs.Data.ToDictionary(r => r.Id, r => r.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while request clusters list");
            return new Dictionary<string, string>();
        }
    }

    private async Task<string[]> ListRunsByClusterId(string id)
    {
        var client = GetClient();

        try
        {
            var result = new List<string>();
            var page = 0;
            var hasMoreData = false;
            do
            {
                page++;
                var response = await client.GetAsync($"{LOBSTR_LIST_RUNS_URL}?cluster={id}&limit=120&page={page}");
                response.EnsureSuccessStatusCode();
                var runs = await response.Content.ReadFromJsonAsync<ListData<Run>>() ??
                           throw new ArgumentNullException();
                var ids = runs.Data.Select(r => r.Id).ToArray();
                result.AddRange(ids);
                hasMoreData = result.Count < runs.Count;
            } while (hasMoreData);

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

public record Run(string Id);
public record Cluster(string Id, string Name);
public record ListData<T>(long Count, long Page, long Limit, T[] Data);