using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using System.ComponentModel;

namespace CaptaTrader.Presentation.CLI.Commands
{
    internal sealed class MainCommand : Command<MainCommand.Settings>
    {
        public class Settings : LogCommandSettings
        {
            // https://spectreconsole.net/cli/settings

            [CommandOption("-m|--mock-data")]
            [Description("Mock data and fake API calls")]
            [DefaultValue(false)]
            public bool? MockData { get; set; }
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            AnsiConsole.WriteLine($"Log Level: {settings.LogLevel}");
            AnsiConsole.WriteLine($"Mock Data: {settings.MockData}");

            //TODO:

            return 0;
        }
    }
}
