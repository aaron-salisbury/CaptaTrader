using CaptaTrader.Data;
using MassTransit;
using Quartz;

namespace CaptaTrader.Business;

public class DomainInitializer
{
    private readonly IBusControl _busControl;
    private readonly IScheduler _scheduler;
    private readonly DataInitializer _dataInitializer;

    public DomainInitializer(IBusControl busControl, IScheduler scheduler, DataInitializer dataInitializer)
    {
        _busControl = busControl;
        _scheduler = scheduler;
        _dataInitializer = dataInitializer;
    }

    public async Task StartAsync()
    {
        await _dataInitializer.StartAsync();
        await _busControl.StartAsync();
        await _scheduler.Start();
    }

    public async Task ShutdownAsync()
    {
        await _scheduler.Shutdown();
        await _busControl.StopAsync();
    }
}
