using System.Diagnostics;
using Hangfire;

namespace Capitalead.Services;

public class Scheduler : IHostedService
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Scheduler> _logger;

    public Scheduler(IRecurringJobManager recurringJobManager, IConfiguration configuration, ILogger<Scheduler> logger)
    {
        _recurringJobManager = recurringJobManager;
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ScheduleMigrationJob();
        ScheduleCalculateKpi();
        return Task.CompletedTask;
    }

    private void ScheduleMigrationJob()
    {
        var cron = _configuration["run_migration_cron"];
        _logger.LogInformation("Run migration cron: {Cron}", cron);
        if (!string.IsNullOrEmpty(cron) && cron.ToLower() != "none")
        {
            _recurringJobManager.AddOrUpdate<MainService>("easyjob", mainService => mainService.StartMigration(), cron);
            _logger.LogInformation("Scheduled run migration job with cron {Cron}", cron);
        }
        else
        {
            _logger.LogInformation("Run migration job was not scheduled because 'run-migration-cron' is not configured");
        }
    }

    private void ScheduleCalculateKpi()
    {
        var cron = _configuration["run_calculate_kpi"];
        _logger.LogInformation("Run calculate kpi cron: {Cron}", cron);
        if (!string.IsNullOrEmpty(cron) && cron.ToLower() != "none")
        {
            _recurringJobManager.AddOrUpdate<MainService>("calculatekpijob", mainService => mainService.CalculateKpi(), cron);
            _logger.LogInformation("Scheduled run calculate kpi job with cron {Cron}", cron);
        }
        else
        {
            _logger.LogInformation("Run calculate kpi job was not scheduled because 'run_calculate_kpi' is not configured");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}