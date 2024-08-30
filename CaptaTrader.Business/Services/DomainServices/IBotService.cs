using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    public interface IBotService
    {
        event EventHandler<TradeOrder>? TradeExecuted;

        bool Startup();

        void Shutdown();
    }
}
