using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Business.Base.ValidationAttributes;

internal class PositiveNumber : ValidationAttribute
{
    public PositiveNumber() : base("{0} must be greater than zero.") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            // Required validation will hit if it's required.
            return null;
        }

        double valueAsDoube;
        try
        {
            valueAsDoube = Convert.ToDouble(value);
        }
        catch
        {
            // Format validation will hit if it cant convert to a double.
            return null;
        }

        bool valid = valueAsDoube > 0.0D;

        if (valid)
        {
            return null;
        }
        else
        {
            List<string>? memberNames = validationContext.MemberName != null ? [validationContext.MemberName] : null;

            return new ValidationResult(base.FormatErrorMessage(validationContext.DisplayName), memberNames);
        }
    }
}
