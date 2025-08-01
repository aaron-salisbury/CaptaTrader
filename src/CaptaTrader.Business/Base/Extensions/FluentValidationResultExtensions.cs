using FluentValidation.Results;
using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Linq;

namespace CaptaTrader.Business.Base.Extensions
{
    public static class FluentValidationResultExtensions
    {
        public static ProcessResult ToProcessResult(this ValidationResult validationResult)
        {
            return new ProcessResult(validationResult.Errors.Select(e => new Exception(e.ErrorMessage)));
        }
    }
}
