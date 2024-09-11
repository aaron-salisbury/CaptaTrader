using CaptaTrader.Presentation.CLI.Base.Commands;
using Serilog.Core;
using Spectre.Console.Cli;

namespace CaptaTrader.Presentation.CLI.Base.Infrastructure;

public class CommandInterceptor : ICommandInterceptor
{
    public static readonly LoggingLevelSwitch LogLevel = new();

    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is MainCommand.Settings mainCommandSettings)
        {
            LogLevel.MinimumLevel = mainCommandSettings.LogLevel;
        }
    }
}
