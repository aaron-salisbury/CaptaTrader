using CaptaTrader.Business.Modules.Trading.DTOs;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.DomainServices;

public interface IBotTrader
{
    Task<bool> UpdateBalancesAsync();
    Task<bool> UpdatePriceIntervalsAsync();
    Task<TradeOrderDto?> AttemptToInitiateTradeAsync();
}
