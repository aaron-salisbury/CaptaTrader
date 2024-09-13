using FluentValidation;

namespace CaptaTrader.Business.DTOs.Validators
{
    public class TradeOrderValidator : AbstractValidator<TradeOrder>
    {
        public TradeOrderValidator()
        {
            RuleFor(dto => dto.TimeStamp).LessThan(DateTime.Now.AddSeconds(1)).WithMessage("Trades must be made for the present.");

            RuleFor(or => or.PriceUSDT).GreaterThan(0.0M);

            RuleFor(or => or.Amount).GreaterThan(0.0M);
        }
    }
}
