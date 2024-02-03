namespace Capitalead.Services;

public class MainService
{
    private readonly LobstrService _lobstrService;
    private readonly NoCrmService _crmService;
    private readonly ILogger<MainService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MainService(LobstrService lobstrService, NoCrmService crmService, ILogger<MainService> logger, IServiceProvider serviceProvider)
    {
        _lobstrService = lobstrService;
        _crmService = crmService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartMigration()
    {
        var uncreatedClustersIdsAndNames = await GetUncreatedCRMListsForClusters();
        var isUncreatedListsExists = uncreatedClustersIdsAndNames.Any();
        
        if (isUncreatedListsExists)
        {
            foreach (var keyvalue in uncreatedClustersIdsAndNames)
            {
                await _crmService.CreateNewProspectingList(keyvalue.Value,
                    new string[] { keyvalue.Key, keyvalue.Value }, null);
            }
        }

        var lists = await _crmService.ListTheProspectingLists();
        var sheetsByClusters = lists.Values.GroupBy(s => s.clusterId).ToList();
        await Parallel.ForEachAsync(sheetsByClusters, new ParallelOptions(){ MaxDegreeOfParallelism = 5 },
            async (group, _) =>
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var crmDataProcessingService = scope.ServiceProvider.GetRequiredService<CrmDataProcessingService>();
                await crmDataProcessingService.RunMigration(group.Key, group.Select(g => g.sheet).ToArray());
            });
        _logger.LogInformation("Main service work done");
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
        var lists = await _crmService.ListTheProspectingLists();
        var crmDataProcessingService = _serviceProvider.GetRequiredService<CrmDataProcessingService>();

        await crmDataProcessingService.FindDuplicates(lists);
        _logger.LogInformation("Find duplicates work done");
    }
}