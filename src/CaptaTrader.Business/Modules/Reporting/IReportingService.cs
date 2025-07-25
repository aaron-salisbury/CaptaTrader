using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules;

internal interface IReportingService
{
    Task WriteTradeOrdersAsync();
}
