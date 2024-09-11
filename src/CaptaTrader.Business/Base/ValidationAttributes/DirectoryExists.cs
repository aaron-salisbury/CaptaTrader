using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Business.Base.ValidationAttributes;

internal class DirectoryExists : ValidationAttribute
{
    public DirectoryExists() : base("{0} doesn't exist.") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return null;
        }

        bool valid = Directory.Exists(value.ToString());

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
