using Spectre.Console.Cli;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI.Base.Commands;

internal sealed class MainCommand : AsyncCommand<MainCommand.Settings>
{
    private readonly Application _app;

    public MainCommand(Application app)
    {
        _app = app;
    }

    public class Settings : LogCommandSettings
    {
        // ref: https://spectreconsole.net/cli/settings

        [CommandOption("-m|--mock-data")]
        [Description("Mock external API calls")]
        [DefaultValue(false)]
        public bool MockExternalAPICalls { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        //TODO: Temporary for testing.
        settings.MockExternalAPICalls = true;
 
        return await _app.RunAsync(settings.MockExternalAPICalls);
    }
}
