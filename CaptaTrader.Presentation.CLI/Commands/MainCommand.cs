using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

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
            settings.MockData ??= false;

            //TODO: Temporary for development.
            settings.MockData = true;

            StringBuilder stringBuilder = new("LAUNCHING MAIN APPLICATION" + Environment.NewLine);
            stringBuilder.AppendLine($" Log Level: {settings.LogLevel}");
            stringBuilder.AppendLine($" Mock Data: {settings.MockData}");
            stringBuilder.AppendLine($" App Directory: {Data.Base.IO.GetAppDirectoryPath()}");
            Serilog.Log.Debug(stringBuilder.ToString());

            return await new Application(_logger, settings.MockData.Value).RunAsync();
        }
    }
}
