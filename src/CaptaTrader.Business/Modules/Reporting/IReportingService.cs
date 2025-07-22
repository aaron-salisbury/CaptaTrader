using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules;

public interface IReportingService
{
    Task WriteTradeOrdersAsync();
}
