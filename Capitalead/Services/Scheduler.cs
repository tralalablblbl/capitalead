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
        var cron = _configuration["run-migration-cron"];
        if (!string.IsNullOrEmpty(cron) && cron.ToLower() != "none")
        {
            _recurringJobManager.AddOrUpdate<MainService>("easyjob", mainService => mainService.Start(), cron);
            _logger.LogInformation("Scheduled run migration job with cron {Cron}", cron);
        }
        else
        {
            _logger.LogInformation("Run migration job was not scheduled because 'run-migration-cron' is not configured");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}