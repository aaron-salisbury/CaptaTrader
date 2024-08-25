using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CaptaTrader.Presentation.CLI.Commands
{
    internal sealed class MainCommand : AsyncCommand<MainCommand.Settings>
    {
        private readonly ILogger _logger;

        public MainCommand(ILogger logger)
        {
            _logger = logger;
        }

        public class Settings : LogCommandSettings
        {
            // https://spectreconsole.net/cli/settings

            [CommandOption("-m|--mock-data")]
            [Description("Mock data and fake API calls")]
            [DefaultValue(false)]
            public bool? MockData { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            //TODO: TEMP*****************
            settings.MockData = true;

            Serilog.Log.Debug($"Log Level: {settings.LogLevel}");
            Serilog.Log.Debug($"Mock Data: {settings.MockData}");
            Serilog.Log.Debug($"App Directory: {Data.Base.IO.GetAppDirectoryPath()}");

            return await new Application(_logger, settings.MockData ?? false).RunAsync();
        }
    }
}
