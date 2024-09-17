using CaptaTrader.Data.Database;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace CaptaTrader.Business
{
    public class DomainInitializer
    {
        private readonly IBusControl _busControl;
        private readonly IScheduler _scheduler;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DomainInitializer(IBusControl busControl, IScheduler scheduler, IServiceScopeFactory serviceScopeFactory)
        {
            _busControl = busControl;
            _scheduler = scheduler;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task StartAsync()
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync(); //TODO: Replace with MigrateAsync() after starting to use migrations.
            
            await _busControl.StartAsync();
            await _scheduler.Start();
        }

        public async Task ShutdownAsync()
        {
            await _scheduler.Shutdown();
            await _busControl.StopAsync();
        }
    }
}
