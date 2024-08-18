using CaptaTrader.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
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
            Console.WriteLine($"Capta Trader v{GetAppVersion()}");
            Console.Write(Environment.NewLine);

            Manager manager = new(_log);
            Console.Write(manager.BitcoinTradingBot.ToString());

            Console.WriteLine("Press any key to activate bitcoin trading bot . . .");
            Console.ReadLine();

            foreach (string botResponse in manager.ActivateAutoTrading())
            {
                Console.WriteLine(botResponse);
            }
        }

        private static string GetAppVersion()
        {
            string versionNumber = string.Empty;

            // Version set in Project File (right-click project) > Package > Package Version
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = executingAssembly.GetName();

            if (assemblyName.Version != null)
            {
                versionNumber = assemblyName.Version.ToString();

                // Drop 4th position, only care about Major.Minor.Patch
                int index = versionNumber.LastIndexOf(".");
                if (index >= 0)
                {
                    versionNumber = versionNumber.Substring(0, index);
                }
            }

            return versionNumber;
        }
    }
}
