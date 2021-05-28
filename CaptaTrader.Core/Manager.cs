using CaptaTrader.Services.TradingBots;
using System;
using System.Collections.Generic;

namespace CaptaTrader.Core
{
    public class Manager
    {
        //TODO:
        // Logging, rewatch that one video on serilog logging in a console app.
        // Has a trading service
        // First servcice will be Binance
        // all services will have things in common, like needing to be able to find out assests in the account, balances, etc
        // Console app should look for a saved service, or this when it gets newed up
        // Console options, new service, load service (if one was found to have been saved)
        // If new service is selected, show options that the app can do. v1.0 will just be Binanace.us
        // After loading or creating a new one, option to start it
        // service needs to record every transaction and create a report on them (mostly for taxes)

        // maybe instead of new vs load, have one save file per service

        public enum TradingServiceTypes
        {
            Binance
        }

        public ITradingBotService TradingBotService { get; set; }

        public TradingServiceTypes TradingServiceType { get; set; }

        public decimal AccountBalance { get; set; }

        public decimal BitcoinBalance { get; set; }

        public Manager(TradingServiceTypes tradingService)
        {
            TradingServiceType = tradingService;

            switch (tradingService)
            {
                case TradingServiceTypes.Binance:
                    TradingBotService = new BinanceTradingBotService();
                    break;
            }
        }

        public IEnumerable<string> Activate()
        {
            yield return "Bot is active.";

            AccountBalance = TradingBotService.GetAccountBalance();
            yield return $"Account Balance: {AccountBalance}";

            BitcoinBalance = TradingBotService.GetAssetBalance("BTC");
            yield return $"Bitcoin Balance: {BitcoinBalance}";

            //TODO: Also need quantity with the price.
            decimal bitcoinSpotPrice = TradingBotService.GetSpotPrice("BTCUSDT");
        }
    }
}
