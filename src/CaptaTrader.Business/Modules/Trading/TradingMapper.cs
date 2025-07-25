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

        [MapperIgnoreSource(nameof(TradeOrder.TradeOrderId)), MapperIgnoreSource(nameof(TradeOrder.EndUserId)), MapperIgnoreSource(nameof(TradeOrder.EndUser))]
        private partial TradeOrderDto MapToDto(TradeOrder source);
        [MapperIgnoreTarget(nameof(TradeOrder.TradeOrderId)), MapperIgnoreTarget(nameof(TradeOrder.EndUserId)), MapperIgnoreTarget(nameof(TradeOrder.EndUser))]
        private partial Data.Entities.TradeOrder MapToEntity(TradeOrderDto source);
    }
}
