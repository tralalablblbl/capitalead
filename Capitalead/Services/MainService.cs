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
    private readonly AppDatabase _database;

    public MainService(LobstrService lobstrService, NoCrmService crmService, ILogger<MainService> logger,
        IServiceProvider serviceProvider, IConfiguration configuration, IMemoryCache memoryCache, AppDatabase database)
    {
        _lobstrService = lobstrService;
        _crmService = crmService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _memoryCache = memoryCache;
        _database = database;
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

        var import = new Import()
        {
            Id = Guid.NewGuid(),
            Started = DateTime.UtcNow,
            Status = RunStatus.InProgress
        };
        await _database.Imports.AddAsync(import);
        await _database.SaveChangesAsync();
        runInfo.Import = import;
        runInfo.CompletedClusters.Clear();
        runInfo.Status = RunStatus.InProgress;
        runInfo.ClustersCount = 0;
        try
        {
            var uncreatedClustersIdsAndNames = await GetUncreatedCrmListsForClusters();
            var isUncreatedListsExists = uncreatedClustersIdsAndNames.Any();

            if (isUncreatedListsExists)
            {
                foreach (var keyvalue in uncreatedClustersIdsAndNames)
                {
                    var sheet = await _crmService.CreateNewProspectingList($"V3 - {keyvalue.Value}  001",
                        [keyvalue.Key, keyvalue.Value.Substring(0, Math.Min(keyvalue.Value.Length, 50)), "1"], null);
                    var dbSheet = new Spreadsheet()
                    {
                        Id = sheet.Id,
                        ClusterId = sheet.Tags[0],
                        ClusterName = sheet.Tags[1],
                        Title = sheet.Title
                    };
                    await _database.Spreadsheets.AddAsync(dbSheet);
                }

                await _database.SaveChangesAsync();
            }

            var lists = await _crmService.ListTheProspectingLists();
            var sheetsByClusters = lists.Values.GroupBy(s => s.clusterId).ToList();
            var runThreadsCount = _configuration.GetValue<int>("run_threads_count", 1);
            runInfo.ClustersCount = sheetsByClusters.Count;
            await Parallel.ForEachAsync(sheetsByClusters,
                new ParallelOptions { MaxDegreeOfParallelism = runThreadsCount },
                async (group, _) =>
                {
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    var loggingFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                    var logger = loggingFactory.CreateLogger("Cluster " + group.Key);
                    logger.LogInformation("Cluster {ClusterId}, {Completed} of {Total}. Started", group.Key,
                        runInfo.CompletedClusters.Count, runInfo.ClustersCount);
                    var service = scope.ServiceProvider.GetRequiredService<CrmDataProcessingService>();
                    var created = await service.RunMigration(group.Key, group.Select(g => g.sheet).ToArray(),
                        runInfo.Import.Id);
                    runInfo.CompletedClusters.TryAdd(group.Key, created);
                    logger.LogInformation(
                        "Cluster {ClusterId}, {Completed} of {Total}. Completed, Added new {ProspectsCreated} prospects",
                        group.Key, runInfo.CompletedClusters.Count, runInfo.ClustersCount, created);
                });

            runInfo.Status = RunStatus.Completed;
            import.Completed = DateTime.UtcNow;
            import.Status = RunStatus.Completed;
            import.AddedCount = runInfo.CompletedClusters.Values.Sum();
            _database.Update(import);
            await _database.SaveChangesAsync();
            _logger.LogInformation("Main service work done");
            _logger.LogInformation("Successfully uploaded all data to new prospecting lists!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to complete import {ImportId}", import.Id);
            var error = ex.ToString();
            import.Error = error;
            import.Completed = DateTime.UtcNow;
            import.Status = RunStatus.Error;
            import.AddedCount = runInfo.CompletedClusters.Values.Sum();
            _database.Update(import);
            await _database.SaveChangesAsync();
            runInfo.Status = RunStatus.Error;
            // restart import with retries
            throw;
        }
    }

    private async Task<IDictionary<string, string>> GetUncreatedCrmListsForClusters()
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

    public async Task ImportSheetsToDatabase()
    {
        _logger.LogInformation("Started import sheets to database script...");
        var lists = await _crmService.ListTheProspectingLists();
        var crmDataProcessingService = _serviceProvider.GetRequiredService<CrmDataProcessingService>();

        await crmDataProcessingService.ImportSheets(lists);
        _logger.LogInformation("Import sheets to database work done");
    }
}