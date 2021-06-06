using CaptaTrader.Core;
using System;
using System.Diagnostics;
using System.Reflection;
using static CaptaTrader.Core.Manager;

namespace CaptaTrader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Capta Trader v{GetProductVersion()}");
            Console.Write(Environment.NewLine);

            string[] tradingServiceTypes = Enum.GetNames(typeof(TradingServiceTypes));
            string menuSelection;
            do
            {
                Console.WriteLine("Select Service:");
                for (int i = 1; i <= tradingServiceTypes.Length; i++)
                {
                    Console.WriteLine($" ({i}) {tradingServiceTypes[i - 1]}");
                }

                menuSelection = Console.ReadLine();
                Console.Write(Environment.NewLine);
            } while (!int.TryParse(menuSelection, out int input) || input < 1 || input > tradingServiceTypes.Length);

            TradingServiceTypes tradingServiceType = (TradingServiceTypes)(Convert.ToInt32(menuSelection) - 1);
            Manager manager = new Manager(tradingServiceType);

            Console.WriteLine($"Press any key to activate {tradingServiceType} bot . . .");
            Console.ReadLine();

            foreach (string botResponse in manager.TradingBotService.ActivateAutoTrading())
            {
                Console.WriteLine(botResponse);
            }
        }

        private static string GetProductVersion()
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

            return fileVersionInfo.ProductVersion;
        }
    }
}
