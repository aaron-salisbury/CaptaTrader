using CaptaTrader.Business.Modules.AutoTrading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.CLIApp.Base;

/// <summary>
/// Keeps the terminal window/tab title animated while auto-trading runs. Unlike in-screen output, the
/// title stays visible while a menu prompt is blocking for input, so the cue is constant.
/// </summary>
internal sealed class WindowTitle : IAsyncDisposable
{
    private static readonly TimeSpan INTERVAL = TimeSpan.FromMilliseconds(250);

    private readonly AutoTrader _autoTrader;
    private readonly bool _isDemo;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _loop;

    internal WindowTitle(AutoTrader autoTrader, bool isDemo)
    {
        _autoTrader = autoTrader;
        _isDemo = isDemo;
    }

    internal void Start()
    {
        _loop = Task.Run(() => RunAsync(_cancellation.Token));
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellation.CancelAsync();

        if (_loop is not null)
        {
            try
            {
                await _loop;
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }

        _cancellation.Dispose();
        TrySetTitle("CaptaTrader");
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(INTERVAL);
        int frame = 0;
        string demo = _isDemo ? " [DEMO]" : string.Empty;

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            TradingStatus status = _autoTrader.Status;

            if (status.IsRunning)
            {
                frame = (frame + 1) % Ui.SPINNER_FRAMES.Count;
                TrySetTitle($"{Ui.SPINNER_FRAMES[frame]} CaptaTrader{demo} · TRADING · BTC {Ui.Price(status.LastTick?.Price)}");
            }
            else
            {
                TrySetTitle($"CaptaTrader{demo} · idle");
            }
        }
    }

    private static void TrySetTitle(string title)
    {
        try
        {
            System.Console.Title = title;
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or System.IO.IOException)
        {
            // Some terminals and redirected sessions have no title to set.
        }
    }
}
