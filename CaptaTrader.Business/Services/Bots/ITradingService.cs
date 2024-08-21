using System;

namespace CaptaTrader.Business.Services
{
    public interface ITradingService
    {
        void Initialize();

        Task UpdatePriceIntervalsAsync();

        IEnumerable<string> Activate();
    }
}
