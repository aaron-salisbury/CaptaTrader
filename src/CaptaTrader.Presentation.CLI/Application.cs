using CaptaTrader.Business;
using CaptaTrader.Presentation.CLI.Base.Extensions;
using CaptaTrader.Presentation.CLI.Presenters;
using Microsoft.Extensions.Logging;
using RunnethOverStudio.AppToolkit.Presentation.MVP;
using Spectre.Console;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI;

internal sealed class Application
{
    internal static TradingBot TradingBot { get { return TradingBot.Instance; } }
    internal static int ExitCode { get; set; } = 0;

    private readonly Navigator _navigator;
    private readonly ILogger _logger;

    public Application(Navigator navigator, ILogger logger)
    {
        _navigator = navigator;
        _logger = logger;

        View.AppTitleFiglet = SetTitle();

        AppDomain.CurrentDomain.ProcessExit += Application_ProcessExit;
    }

    public async Task<int> RunAsync(bool mockExternalAPICalls)
    {
        TradingBot.Initialize(new TradingBotConfig() { UseMockTransactions = mockExternalAPICalls }, _logger);

        await TradingBot.Instance.StartAsync();

        _navigator.NavigateTo(typeof(LoginPresenter));

        return ExitCode;
    }

    private void Application_ProcessExit(object? sender, EventArgs e)
    {
        ValueTask valueTask = TradingBot.DisposeAsync();
        AppDomain.CurrentDomain.ProcessExit -= Application_ProcessExit;

        AnsiConsole.Clear();
        AnsiConsole.WriteLine("Capta Trader application exited.");
    }

    private static FigletText SetTitle()
    {
        string? font = Assembly.GetExecutingAssembly().ReadResource("Ogre.flf");
        FigletFont figletFont = font != null ? FigletFont.Parse(font) : FigletFont.Default;

        return new FigletText(figletFont, "Capta Trader").Color(Color.Green);
    }
}
