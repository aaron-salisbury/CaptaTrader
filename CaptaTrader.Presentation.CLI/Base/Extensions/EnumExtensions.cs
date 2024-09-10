using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace CaptaTrader.Presentation.CLI.Base;

public static class EnumExtensions
{
    /// <summary>
    /// Retrieve attribute that is applied to an `Enum`.
    /// </summary>
    public static TAttribute? GetAttribute<TAttribute>(this Enum enumValue) where TAttribute : Attribute
    {
        return enumValue
            .GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttribute<TAttribute>();
    }

    /// <summary>
    /// Use DisplayAttribute if exists. Otherwise, use the standard string representation.
    /// </summary>
    public static string GetDisplayName(this Enum enumValue)
    {
        DisplayAttribute? displayAttribute = enumValue.GetAttribute<DisplayAttribute>();

        return displayAttribute?.Name ?? enumValue.ToString();
    }
}
