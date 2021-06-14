using CaptaTrader.Core;
using System;
using System.Diagnostics;
using System.Reflection;

namespace CaptaTrader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Capta Trader v{GetProductVersion()}");
            Console.Write(Environment.NewLine);

            Manager manager = new Manager();

            Console.WriteLine("Press any key to activate bitcoin trading bot . . .");
            Console.ReadLine();

            foreach (string botResponse in manager.ActivateAutoTrading())
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
