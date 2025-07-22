using CaptaTrader.Business.Modules.UserAccess.DTOs;
using FluentValidation;

namespace CaptaTrader.Business.Modules.Trading.Validators
{
    public sealed class UserConfigValidator : AbstractValidator<UserConfigDto>
    {
        public UserConfigValidator()
        {
            RuleFor(dto => dto.Email).EmailAddress();
        }
    }
}
