using Serilog.Events;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace CaptaTrader.Presentation.CLI.Base.Commands;

internal class LogCommandSettings : CommandSettings
{
    [CommandOption("-l|--loglevel")]
    [Description("Minimum level for logging")]
    [TypeConverter(typeof(VerbosityConverter))]
    [DefaultValue(LogEventLevel.Information)]
    public LogEventLevel LogLevel { get; set; }
}

public sealed class VerbosityConverter : TypeConverter
{
    private readonly Dictionary<string, LogEventLevel> _lookup;

    public VerbosityConverter()
    {
        _lookup = new Dictionary<string, LogEventLevel>(StringComparer.OrdinalIgnoreCase)
        {
            {"v", LogEventLevel.Verbose},
            {"d", LogEventLevel.Debug},
            {"i", LogEventLevel.Information},
            {"w", LogEventLevel.Warning},
            {"e", LogEventLevel.Error},
            {"f", LogEventLevel.Fatal}
        };
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            bool result = _lookup.TryGetValue(stringValue, out var verbosity);
            if (!result)
            {
                const string format = "The value '{0}' is not a valid verbosity.";
                string message = string.Format(CultureInfo.InvariantCulture, format, value);
                throw new InvalidOperationException(message);
            }
            return verbosity;
        }
        throw new NotSupportedException("Can't convert value to verbosity.");
    }
}
