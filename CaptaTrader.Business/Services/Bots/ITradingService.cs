using System;

namespace CaptaTrader.Business.Services
{
    public interface ITradingService
    {
        Task Initialize();

        Task UpdatePriceIntervalsAsync();

        IEnumerable<string> Activate();
    }
}
