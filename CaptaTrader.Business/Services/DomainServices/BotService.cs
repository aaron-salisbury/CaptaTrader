using System;

namespace CaptaTrader.Business.Services
{
    public class BotService : IBotService
    {
        private IBotTraderService _botTraderService;

        internal BotService(IBotTraderService botTraderService)
        {
            _botTraderService = botTraderService;
        }

        public bool Startup()
        {
            return _botTraderService.InitializeAsync().Result;
        }

        public void Shutdown()
        {
            Task.Run(async () => await _botTraderService.ShutdownAsync()).Wait();
        }
    }
}
