using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace Capitalead.Services;

public class MainService
{
    private readonly LobstrService _lobstrService;
    private readonly NoCRMService _crmService;
    private readonly ILogger<MainService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MainService(LobstrService lobstrService, NoCRMService crmService, ILogger<MainService> logger, IServiceProvider serviceProvider)
    {
        _lobstrService = lobstrService;
        _crmService = crmService;
        _logger = logger;
        _serviceProvider = serviceProvider;
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
            async (listId, _) =>
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var crmDataProcessingService = scope.ServiceProvider.GetRequiredService<CrmDataProcessingService>();
                await crmDataProcessingService.Run(listId);
            });
        //await _crmDataProcessingService.Run(lists.Keys.First());
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