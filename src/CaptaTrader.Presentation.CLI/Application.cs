using CaptaTrader.Business;
using CaptaTrader.Presentation.CLI.Base.Extensions;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Presenters;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI;

public class Application
{
    internal static FigletText AppTitle { get; } = SetTitle();
    internal static int ExitCode { get; set; } = 0;

    private readonly DomainInitializer _domainInitializer;
    private readonly Navigator _navigator;

    public Application(DomainInitializer domainInitializer, Navigator navigator)
    {
        _domainInitializer = domainInitializer;
        _navigator = navigator;

        AppDomain.CurrentDomain.ProcessExit += Application_ProcessExit;
    }

    public async Task<int> RunAsync()
    {
        await _domainInitializer.StartAsync();

        _navigator.NavigateTo(typeof(LoginPresenter));

        return ExitCode;
    }

    private void Application_ProcessExit(object? sender, EventArgs e)
    {
        _domainInitializer.ShutdownAsync().Wait();

        //AnsiConsole.Clear();
        AnsiConsole.WriteLine("Capta Trader application exited.");
    }

    internal static void AddPresentersAndConsumers(IServiceCollection services)
    {
        foreach (Type assemblyType in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (typeof(IConsumer).IsAssignableFrom(assemblyType))
            {
                // Consumers have to be singletons if their Consume method uses instance values.
                services.AddSingleton(assemblyType);
            }
            else if (assemblyType.Name.EndsWith("Presenter") && !assemblyType.Name.Equals("Presenter"))
            {
                services.AddScoped(assemblyType);
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
