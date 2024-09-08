using CaptaTrader.Business.Jobs;
using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Data;
using CaptaTrader.Data.Database;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CaptaTrader.Business
{
    public static class Startup
    {
        /// <summary>
        /// Initializes and adds public-facing business-tier services, including <see cref="IBusControl"/> for messaging.
        /// Dependent on application <see cref="ILogger"/> and <see cref="ISessionValueProvider"/>.
        /// </summary>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, bool useMockExternalAPICalls, params Type[] massTransitConsumerTypes)
        {
            //TODO: Look into:
            // https://unplu.gg/test_api.html
            // https://site.financialmodelingprep.com/developer/docs
            // https://www.browsercat.com/pricing
            // https://alternative.me/crypto/api/
            // https://stackoverflow.com/questions/249760/how-can-i-convert-a-unix-timestamp-to-datetime-and-vice-versa

            // TODO: Should be able to set non-infrastructure services as Scoped.

            // Infrastructure services.
            services.AddWebRequesting()
                .AddDbContext<ApplicationDbContext>()
                .AddSingleton<ISessionValueResolver, SessionValueResolver>()
                .AddScheduling()
                .AddMassTransit(x =>
                {
                    x.AddConsumers(massTransitConsumerTypes);
                    x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
                });

            // External API services.
            if (useMockExternalAPICalls)
            {
                services.AddSingleton<IMarketDataService, MockDataService>()
                    .AddSingleton<IExchangeService, MockExchangeService>()
                    .AddSingleton<ICryptoFearAndGreedIndex, MockCFGIService>();
            }
            else
            {
                services.AddSingleton<IMarketDataService, CoinApiDataService>() //TODO: Potential alternate if limit is reached: https://openapi.coinstats.app/
                    .AddSingleton<IExchangeService, BinanceExchangeService>()
                    .AddSingleton<ICryptoFearAndGreedIndex, AlternativeCFGI>();
            }

            // Internal business domain logic.
            services.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>() //TODO: Obviously will need a real analyzer.
                .AddSingleton<IBotTrader, BitcoinBotTrader>();

            // Orchestrated public facing services.
            services.AddSingleton<IUserService, UserService>()
                .AddSingleton<ITradingService, TradingService>();

            return services;
        }

        private static IServiceCollection AddScheduling(this IServiceCollection services)
        {
            services.AddQuartz(ConfigureScheduler)
                .AddSingleton(sp => sp.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);

            return services;
        }

        private static void ConfigureScheduler(IServiceCollectionQuartzConfigurator configure)
        {
            const int MIN_SEC_BETWEEN_TRADES = 6;

            configure.UseSimpleTypeLoader();
            configure.UseInMemoryStore();

            #region SeekTradeJob
            configure.ScheduleJob<SeekTradeJob>(trigger => trigger
                .WithIdentity($"{nameof(SeekTradeJob)}Trigger", nameof(BitcoinBotTrader))
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(MIN_SEC_BETWEEN_TRADES)
                    .RepeatForever())
                .WithDescription($"Trigger configured for a job that seeks to make a trade every {MIN_SEC_BETWEEN_TRADES} seconds."));
            #endregion

            #region BitcoinPriceIntervalsJob
            //IJobDetail bitcoinPriceIntervalsJob = JobBuilder.Create<BitcoinPriceIntervalsJob>()
            //    .WithIdentity(nameof(BitcoinPriceIntervalsJob), nameof(BitcoinBotTrader))
            //    .Build();

            //ITrigger bitcoinPriceIntervalsTrigger = TriggerBuilder.Create()
            //    .WithIdentity($"{nameof(BitcoinPriceIntervalsJob)}Trigger", nameof(BitcoinBotTrader))
            //    .StartAt(new DateTimeOffset(DateTime.Now.Date.AddDays(1)))
            //    .WithSimpleSchedule(x => x
            //        .WithIntervalInHours(24)
            //        .RepeatForever())
            //    .Build();

            //await scheduler.ScheduleJob(bitcoinPriceIntervalsJob, bitcoinPriceIntervalsTrigger);
            #endregion

            #region AttemptToInitiateTradeJob
            //IJobDetail attemptToInitiateTradeJob = JobBuilder.Create<AttemptToInitiateTradeJob>()
            //    .WithIdentity(nameof(AttemptToInitiateTradeJob), nameof(BitcoinBotTrader))
            //    .Build();

            //ITrigger attemptToInitiateTradeTrigger = TriggerBuilder.Create()
            //    .WithIdentity($"{nameof(AttemptToInitiateTradeJob)}Trigger", nameof(BitcoinBotTrader))
            //    .StartNow()
            //    .WithSimpleSchedule(x => x
            //        .WithIntervalInSeconds(MIN_SEC_BETWEEN_TRADES)
            //        .RepeatForever())
            //    .Build();

            //await scheduler.ScheduleJob(attemptToInitiateTradeJob, attemptToInitiateTradeTrigger);
            #endregion
        }
    }
}
