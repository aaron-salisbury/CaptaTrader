using CaptaTrader.Business.Entities;

namespace CaptaTrader.Business.Services
{
    public class BotService : IBotService
    {
        public event EventHandler<TradeOrder>? TradeExecuted;

        private IBotTraderService _botTraderService;

        public BotService(IBotTraderService botTraderService)
        {
            _botTraderService = botTraderService;
            _botTraderService.TradeCompleted += BotTrader_TradeCompleted;
        }

        private void BotTrader_TradeCompleted(object? sender, TradeEventArgs e)
        {
            TradeExecuted?.Invoke(sender, e.TradeOrder);
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
