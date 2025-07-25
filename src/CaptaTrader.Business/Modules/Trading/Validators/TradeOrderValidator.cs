using CaptaTrader.Business.Modules.Trading.DTOs;
using FluentValidation;
using System;

namespace CaptaTrader.Business.Modules.Trading.Validators
{
    public sealed class TradeOrderValidator : AbstractValidator<TradeOrderDto>
    {
        public TradeOrderValidator()
        {
            //RuleFor(dto => dto.TimeStamp).LessThan(DateTime.Now.AddSeconds(1)).WithMessage("Trades must be made for the present.");

            RuleFor(or => or.Price).GreaterThan(0.0M);

            RuleFor(or => or.Amount).GreaterThan(0.0M);

            //TODO: Seems that Binance requires buys to spend at least 1 USDT and at 2 decimal places.
        }
    }
}
