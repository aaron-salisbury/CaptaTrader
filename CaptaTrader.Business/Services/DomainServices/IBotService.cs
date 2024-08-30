using CaptaTrader.Business.Entities;

namespace CaptaTrader.Business.Services
{
    public interface IBotService
    {
        event EventHandler<TradeOrder>? TradeExecuted;

        bool Startup();

        void Shutdown();
    }
}
