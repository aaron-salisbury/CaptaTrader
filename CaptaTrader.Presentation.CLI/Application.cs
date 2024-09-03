using CaptaTrader.Business;
using CaptaTrader.Business.Base;
using CaptaTrader.Presentation.CLI.Base;
using CaptaTrader.Presentation.CLI.Base.Extensions;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Presenters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI
{
    public class Application
    {
        internal static IServiceProvider? DomainServices { get; set; }
        internal static int ExitCode { get; set; } = 0;

        private static readonly FigletText _title = SetTitle();

        public Application(ILogger logger, bool useMockExternalAPICalls)
        {
            DomainServices = new ServiceCollection()
                .AddSingleton(logger)
                .AddSingleton<ISessionValueProvider, Session>()
                .AddBusinessServices(useMockExternalAPICalls)
                .BuildServiceProvider();
        }

        public async Task<int> RunAsync()
        {
            await Task.Run(() => new Navigator().NavigateTo(typeof(LoginPresenter)));

            AnsiConsole.Clear();
            AnsiConsole.WriteLine("Capta Trader application exited.");
            return ExitCode;
        }

        private static FigletText SetTitle()
        {
            // https://github.com/xero/figlet-fonts
            string? font = Assembly.GetExecutingAssembly().ReadResource("Ogre.flf");
            FigletFont figletFont = font != null ? FigletFont.Parse(font) : FigletFont.Default;

            return new FigletText(figletFont, "Capta Trader").Color(Color.Green);
        }

        internal static void ClearConsole()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(_title);
            AnsiConsole.Write(Environment.NewLine);
        }
    }
}
