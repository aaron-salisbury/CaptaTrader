using System;

namespace CaptaTrader.Models;

public class Portfolio : Model
{
    private decimal? _accountBalance;
    public decimal? AccountBalance
    {
        get => _accountBalance;
        set
        {
            SetProperty(ref _accountBalance, value);
        }
    }

    private decimal? _assetBalance;
    public decimal? AssetBalance
    {
        get => _assetBalance;
        set
        {
            SetProperty(ref _assetBalance, value);
        }
    }

    private TradeOrder? _lastCompletedTradeOrder;
    public TradeOrder? LastCompletedTradeOrder
    {
        get => _lastCompletedTradeOrder;
        set
        {
            SetProperty(ref _lastCompletedTradeOrder, value);
        }
    }
}
