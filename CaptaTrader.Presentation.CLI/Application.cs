using CaptaTrader.Business;
using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Presentation.CLI.Base;
using CaptaTrader.Presentation.CLI.Base.Extensions;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Consumers;
using CaptaTrader.Presentation.CLI.Presenters;
using MassTransit;
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
        internal static IServiceProvider? ServiceProvider { get; set; }
        internal static int ExitCode { get; set; } = 0;

        private static readonly FigletText _title = SetTitle();

        public Application(ILogger applicationLogger, bool useMockExternalAPICalls)
        {
            IServiceCollection services = new ServiceCollection()
                .AddSingleton(applicationLogger)
                .AddSingleton<ISessionValueProvider>(new Session())
                .AddBusinessServices(useMockExternalAPICalls, [typeof(PingConsumer)]);

            foreach (Type appType in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (appType.Name.EndsWith("Presenter") && !appType.Name.Equals("Presenter"))
                {
                    services.AddScoped(appType);
                }
            }

            ServiceProvider = services.BuildServiceProvider();

            AppDomain.CurrentDomain.ProcessExit += Application_ProcessExit;
        }

        public async Task<int> RunAsync()
        {
            await ServiceProvider!.GetRequiredService<IBusControl>().StartAsync();

            await Task.Run(() => new Navigator().NavigateTo(typeof(LoginPresenter)));

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

        private void Application_ProcessExit(object? sender, EventArgs e)
        {
            ServiceProvider!.GetRequiredService<ITradingService>().ShutdownTradingAsync();
            ServiceProvider!.GetRequiredService<IBusControl>().Stop();

            AnsiConsole.Clear();
            AnsiConsole.WriteLine("Capta Trader application exited.");
        }
    }
}
