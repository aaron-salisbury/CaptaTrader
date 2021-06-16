using CaptaTrader.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Reflection;

namespace CaptaTrader
{
    class MainService : IMainService
    {
        private readonly ILogger<MainService> _log;
        private readonly IConfiguration _config;

        public MainService(ILogger<MainService> log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }

        public void Run()
        {
            Console.WriteLine($"Capta Trader v{GetProductVersion()}");
            Console.Write(Environment.NewLine);

            Manager manager = new Manager(_log);

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
