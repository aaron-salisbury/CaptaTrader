using CaptaTrader.Business.DTOs;
using System;

namespace CaptaTrader.Business.Services
{
    public interface IBotService
    {
        event EventHandler<Trade>? TradeExecuted;

        bool Startup();

        void Shutdown();
    }
}
