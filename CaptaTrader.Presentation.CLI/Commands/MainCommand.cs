using CaptaTrader.Data.Base;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI.Commands
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
            public bool? MockData { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            settings.MockData ??= false;

            //TODO: Temporary for development.
            settings.MockData = true;

            string appFolderPath = IO.GetAppDirectoryPath();
            string appName = new DirectoryInfo(appFolderPath).Name;
            ILogger logger = _loggerProvider.CreateLogger(appName);

            StringBuilder stringBuilder = new($"Launching {appName}...{Environment.NewLine}");
            stringBuilder.AppendLine($"    Log Level: {settings.LogLevel}");
            stringBuilder.AppendLine($"    Mock Data: {settings.MockData}");
            stringBuilder.AppendLine($"    App Directory: {appFolderPath}");
            logger.LogDebug(stringBuilder.ToString());

            return await new Application(_loggerProvider.CreateLogger(appName), settings.MockData.Value).RunAsync();
        }
    }
}
