using Capitalead.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Capitalead.Services;

public class MainService
{
    private readonly LobstrService _lobstrService;
    private readonly NoCrmService _crmService;
    private readonly ILogger<MainService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;

    public MainService(LobstrService lobstrService, NoCrmService crmService, ILogger<MainService> logger,
        IServiceProvider serviceProvider, IConfiguration configuration, IMemoryCache memoryCache)
    {
        _lobstrService = lobstrService;
        _crmService = crmService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _memoryCache = memoryCache;
    }

    public async Task StartMigration()
    {
        _logger.LogInformation("Started uploading script...");

        var runInfo = _memoryCache.GetOrCreate<RunInfo>("runInfo", entry =>
        {
            var info = new RunInfo()
            {
                Status = RunStatus.None
            };
            return info;
        });
        if (runInfo!.Status == RunStatus.InProgress)
        {
            _logger.LogInformation("Migration already in progress, wait for results");
            return;
        }
        var uncreatedClustersIdsAndNames = await GetUncreatedCRMListsForClusters();
        var isUncreatedListsExists = uncreatedClustersIdsAndNames.Any();
        
        if (isUncreatedListsExists)
        {
            foreach (var keyvalue in uncreatedClustersIdsAndNames)
            {
                await _crmService.CreateNewProspectingList($"V3 - {keyvalue.Value}  001",
                    new string[] { keyvalue.Key, keyvalue.Value, "1" }, null);
            }
        }

        var lists = await _crmService.ListTheProspectingLists();
        var sheetsByClusters = lists.Values.GroupBy(s => s.clusterId).ToList();
        var runThreadsCount = _configuration.GetValue<int>("run_threads_count", 1);
        runInfo.CompletedClusters.Clear();
        runInfo.Sheets.Clear();
        runInfo.Status = RunStatus.InProgress;
        foreach (var group in sheetsByClusters)
        {
            runInfo.Sheets[group.Key] = group.Select(sheet => (sheet.sheet.Id, sheet.sheet.Title)).ToArray();
        }
        await Parallel.ForEachAsync(sheetsByClusters, new ParallelOptions(){ MaxDegreeOfParallelism = runThreadsCount },
            async (group, _) =>
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var loggingFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggingFactory.CreateLogger("Cluster " + group.Key);
                logger.LogInformation("Cluster {ClusterId}, {Completed} of {Total}. Started", group.Key, runInfo.CompletedClusters.Count, runInfo.Sheets.Count);
                var crmDataProcessingService = scope.ServiceProvider.GetRequiredService<CrmDataProcessingService>();
                await crmDataProcessingService.RunMigration(group.Key, group.Select(g => g.sheet).ToArray());
                runInfo.CompletedClusters.Add(group.Key);
                logger.LogInformation("Cluster {ClusterId}, {Completed} of {Total}. Completed", group.Key, runInfo.CompletedClusters.Count, runInfo.Sheets.Count);
                
            });
        _logger.LogInformation("Main service work done");
        _logger.LogInformation("Successfully uploaded all data to new prospecting lists!");
    }

    private async Task<IDictionary<string, string>> GetUncreatedCRMListsForClusters()
    {
        var clusters = await _lobstrService.GetClusterIdsAndNames();
        var listsClusterIdTags = (await _crmService.ListTheProspectingLists()).Values.Select(c => c.clusterId);
        var clustersIds = clusters.Keys;

        var clustersIdOfUncreatedLists = clustersIds.Except(listsClusterIdTags).ToList();
        var uncreatedClustersIdsAndNames = new Dictionary<string, string>();

        foreach (var id in clustersIdOfUncreatedLists)
        {
            uncreatedClustersIdsAndNames.Add(id, clusters[id]);
        }

        return uncreatedClustersIdsAndNames;
    }

    public async Task FindDuplicates()
    {
        _logger.LogInformation("Started find duplicates script...");
        var lists = await _crmService.ListTheProspectingLists();
        var crmDataProcessingService = _serviceProvider.GetRequiredService<CrmDataProcessingService>();

        await crmDataProcessingService.FindDuplicates(lists);
        _logger.LogInformation("Find duplicates work done");
        _logger.LogInformation("Successfully found duplicates!");
    }

    public async Task MigrateSheets()
    {
        _logger.LogInformation("Started spreadsheets migration...");
        var lists = await _crmService.ListTheProspectingListsToMigrate();
        var crmDataProcessingService = _serviceProvider.GetRequiredService<CrmDataProcessingService>();

        await crmDataProcessingService.MigrateSheets(lists);
        _logger.LogInformation("Migrate sheets work done");
        _logger.LogInformation("Successfully migrated spreadsheets!");
    }
}