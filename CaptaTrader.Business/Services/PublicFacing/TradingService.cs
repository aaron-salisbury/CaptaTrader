using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services.Domain
{
    public class TradingService : ITradingService
    {
        public Portfolio RunningPortfolio { get; set; }

        private readonly IBotTrader _botTrader;

        public TradingService(IBotTrader botTrader)
        {
            _botTrader = botTrader;

            RunningPortfolio = _botTrader.Portfolio;
        }

        public async Task<bool> InitializeTradingAsync()
        {
            return await _botTrader.InitializeAsync();
        }

        public async Task ShutdownTradingAsync()
        {
            await _botTrader.ShutdownAsync();
        }
    }
}
