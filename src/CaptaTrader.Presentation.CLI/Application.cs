using CaptaTrader.Business;
using CaptaTrader.Presentation.CLI.Base.Extensions;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Presenters;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI;

public sealed class Application
{
    internal static TradingBot? TradingBot { get; private set; }
    internal static FigletText AppTitle { get; } = SetTitle();
    internal static int ExitCode { get; set; } = 0;

    private readonly Navigator _navigator;
    private readonly ILogger _logger;

    public Application(Navigator navigator, ILogger logger)
    {
        _navigator = navigator;
        _logger = logger;

        AppDomain.CurrentDomain.ProcessExit += Application_ProcessExit;
    }

    public async Task<int> RunAsync(bool mockExternalAPICalls)
    {
        TradingBot = new TradingBot(new TradingBotConfig() { UseMockTransactions = mockExternalAPICalls }, _logger);

        await TradingBot.StartAsync();

        _navigator.NavigateTo(typeof(LoginPresenter));

        return ExitCode;
    }

    private void Application_ProcessExit(object? sender, EventArgs e)
    {
        ValueTask? valueTask = TradingBot?.DisposeAsync();
        AppDomain.CurrentDomain.ProcessExit -= Application_ProcessExit;

        //AnsiConsole.Clear();
        AnsiConsole.WriteLine("Capta Trader application exited.");
    }

    internal static IEnumerable<Type> GetPresenterTypes()
    {
        foreach (Type assemblyType in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (assemblyType.Name.EndsWith("Presenter") && !assemblyType.Name.Equals("Presenter"))
            {
                yield return assemblyType;
            }
        }
    }

    private static FigletText SetTitle()
    {
        string? font = Assembly.GetExecutingAssembly().ReadResource("Ogre.flf");
        FigletFont figletFont = font != null ? FigletFont.Parse(font) : FigletFont.Default;

        return new FigletText(figletFont, "Capta Trader").Color(Color.Green);
    }
}
