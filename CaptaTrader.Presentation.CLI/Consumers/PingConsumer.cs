using CaptaTrader.Business.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI.Consumers
{
    //TODO: This is a test. I expect the BotTraderPresenter to consume trade orders.
    public class PingConsumer : IConsumer<Ping>
    {
        private readonly ILogger<PingConsumer> _logger;

        public PingConsumer(ILogger<PingConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<Ping> context)
        {
            AnsiConsole.WriteLine($"Trade Made: {context.Message.Value}");
            return Task.CompletedTask;
        }
    }
}
