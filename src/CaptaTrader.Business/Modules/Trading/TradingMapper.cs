using CaptaTrader.Business.Modules.Trading.DTOs;
using CaptaTrader.Data.Entities;
using Riok.Mapperly.Abstractions;

namespace CaptaTrader.Business.Modules.Trading
{
    [Mapper]
    internal partial class TradingMapper
    {
        internal partial TTarget MapToDto<TTarget>(object source);
        internal partial TTarget MapToEntity<TTarget>(object source);

        private partial TradeOrderDto MapToDto(TradeOrder source);
        private partial TradeOrder MapToEntity(TradeOrderDto source);
    }
}
