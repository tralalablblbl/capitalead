using System.Diagnostics;
using Hangfire;

namespace Capitalead.Services;

public class Scheduler : IHostedService
{
    private readonly IRecurringJobManager _recurringJobManager;

    public Scheduler(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!Debugger.IsAttached)
            _recurringJobManager.AddOrUpdate<MainService>("easyjob", mainService => mainService.Start(), "0 0 9 * * *");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}