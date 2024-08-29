using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI.Base.Commands
{
    internal sealed class MainCommand : AsyncCommand<MainCommand.Settings>
    {
        private ILoggerProvider _loggerProvider;

        public MainCommand(ILoggerProvider loggerProvider)
        {
            _loggerProvider = loggerProvider;
        }

        public class Settings : LogCommandSettings
        {
            // https://spectreconsole.net/cli/settings

            [CommandOption("-m|--mock-data")]
            [Description("Mock data and fake API calls")]
            [DefaultValue(false)]
            public bool MockData { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            //TODO: Temporary for testing.
            settings.MockData = true;

            return await new Application(_loggerProvider, settings.MockData).RunAsync();
        }
    }
}
