namespace Capitalead.Services;

public class MainService
{
    private readonly LobstrService _lobstrService;
    private readonly NoCRMService _crmService;
    private readonly ILogger<MainService> _logger;
    private readonly CrmDataProcessingService _crmDataProcessingService;

    public MainService(LobstrService lobstrService, NoCRMService crmService, ILogger<MainService> logger,
        CrmDataProcessingService crmDataProcessingService)
    {
        _lobstrService = lobstrService;
        _crmService = crmService;
        _logger = logger;
        _crmDataProcessingService = crmDataProcessingService;
    }

    public async Task Start()
    {
        var uncreatedClustersIdsAndNames = await GetUncreatedCRMListsForClusters();
        var isUncreatedListsExists = uncreatedClustersIdsAndNames.Any();

        if (isUncreatedListsExists)
        {
            foreach (var keyvalue in uncreatedClustersIdsAndNames)
            {
                await _crmService.CreateNewProspectingList(keyvalue.Value,
                    new string[] { keyvalue.Key, keyvalue.Value });
            }
        }

        var lists = await _crmService.ListTheProspectingLists();
        await Parallel.ForEachAsync(lists.Keys, CancellationToken.None,
            async (listId, _) => { await _crmDataProcessingService.Run(listId); });
        _logger.LogInformation("Main service work done");
    }

    private async Task<IDictionary<string, string>> GetUncreatedCRMListsForClusters()
    {
        var clusters = await _lobstrService.GetClusterIdsAndNames();
        var listsClusterIdTags = (await _crmService.ListTheProspectingLists()).Values;
        var clustersIds = clusters.Keys;

        var clustersIdOfUncreatedLists = clustersIds.Except(listsClusterIdTags).ToList();
        var uncreatedClustersIdsAndNames = new Dictionary<string, string>();

        foreach (var id in clustersIdOfUncreatedLists)
        {
            uncreatedClustersIdsAndNames.Add(id, clusters[id]);
        }

        return uncreatedClustersIdsAndNames;
    }

}