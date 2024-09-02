using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI.Base.Commands
{
    internal sealed class MainCommand : AsyncCommand<MainCommand.Settings>
    {
        private ILoggerFactory _loggerFactory;

        public MainCommand(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public class Settings : LogCommandSettings
        {
            // https://spectreconsole.net/cli/settings

            [CommandOption("-m|--mock-data")]
            [Description("Mock external API calls")]
            [DefaultValue(false)]
            public bool MockExternalAPICalls { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            //TODO: Temporary for testing.
            settings.MockExternalAPICalls = true;

            return await new Application(_loggerFactory.CreateLogger(nameof(Application)), settings.MockExternalAPICalls).RunAsync();
        }
    }
}
