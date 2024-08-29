using System;

namespace CaptaTrader.Business.Services
{
    public interface IBotService
    {
        bool Startup();

        void Shutdown();
    }
}
