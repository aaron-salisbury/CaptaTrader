using CaptaTrader.Business;
using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Data.Database;
using CaptaTrader.Presentation.CLI.Base;
using CaptaTrader.Presentation.CLI.Base.Extensions;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Presenters;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI;

public class Application
{
    internal static readonly FigletText AppTitle = SetTitle();
    internal static IServiceProvider? ServiceProvider { get; set; }
    internal static int ExitCode { get; set; } = 0;

    public Application(ILogger applicationLogger, bool useMockExternalAPICalls)
    {
        ServiceProvider = ConfigureServices(applicationLogger, useMockExternalAPICalls);

        AppDomain.CurrentDomain.ProcessExit += Application_ProcessExit;
    }

    public async Task<int> RunAsync()
    {
        await Task.Run(() =>
        {
            ServiceProvider!.GetRequiredService<Navigator>()
                .NavigateTo(typeof(LoginPresenter));
        });

        return ExitCode;
    }

    private IServiceProvider ConfigureServices(ILogger applicationLogger, bool useMockExternalAPICalls)
    {
        IServiceCollection services = new ServiceCollection()
            .AddSingleton(applicationLogger)
        .AddSingleton<ISessionValueProvider>(new Session())
        .AddSingleton(new Navigator());

        List<Type> massTransitConsumerTypes = [];
        AddPresentersAndConsumers(services, massTransitConsumerTypes);

        services.AddBusinessServices(useMockExternalAPICalls, [.. massTransitConsumerTypes]);

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync(); //TODO: Replace with MigrateAsync() after starting to use migrations.
        serviceProvider.GetRequiredService<IBusControl>().StartAsync();
        serviceProvider.GetRequiredService<IScheduler>().Start();

        return serviceProvider;
    }

    private void Application_ProcessExit(object? sender, EventArgs e)
    {
        ServiceProvider!.GetRequiredService<IScheduler>().Shutdown();
        ServiceProvider!.GetRequiredService<IBusControl>().Stop();

        AnsiConsole.Clear();
        AnsiConsole.WriteLine("Capta Trader application exited.");
    }

    private static void AddPresentersAndConsumers(IServiceCollection services, List<Type> massTransitConsumerTypes)
    {
        foreach (Type assemblyType in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (typeof(IConsumer).IsAssignableFrom(assemblyType))
            {
                massTransitConsumerTypes.Add(assemblyType);
                if (assemblyType.Name.EndsWith("Presenter") && !assemblyType.Name.Equals("Presenter"))
                {
                    // Consumers have to be singletons if their Consume method uses instance values.
                    services.AddSingleton(assemblyType);
                }
            }
            else if (assemblyType.Name.EndsWith("Presenter") && !assemblyType.Name.Equals("Presenter"))
            {
                services.AddScoped(assemblyType);
            }
        }
    }

    private static FigletText SetTitle()
    {
        // https://github.com/xero/figlet-fonts
        string? font = Assembly.GetExecutingAssembly().ReadResource("Ogre.flf");
        FigletFont figletFont = font != null ? FigletFont.Parse(font) : FigletFont.Default;

        return new FigletText(figletFont, "Capta Trader").Color(Color.Green);
    }
}
