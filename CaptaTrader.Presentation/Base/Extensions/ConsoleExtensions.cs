using System;
using System.Reflection;

namespace CaptaTrader.Presentation.Base.Extensions
{
    public static class ConsoleExtensions
    {
        /// <summary>
        /// Get Major.Minor.Patch set in Project File > Package > Package Version.
        /// </summary>
        public static string GetAppVersion()
        {
            string versionNumber = string.Empty;

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = executingAssembly.GetName();

            if (assemblyName.Version != null)
            {
                versionNumber = assemblyName.Version.ToString();

                // Drop 4th position, only care about Major.Minor.Patch
                int index = versionNumber.LastIndexOf('.');
                if (index >= 0)
                {
                    versionNumber = versionNumber.Substring(0, index);
                }
            }

            return versionNumber;
        }

        /// <summary>
        /// Default CLI masked password request.
        /// </summary>
        public static string RequestPassword(string requestMessage = "Enter your password: ")
        {
            // https://stackoverflow.com/questions/3404421/password-masking-console-application

            string passPhrase = string.Empty;

            Console.Write(requestMessage);
            ConsoleKey key;

            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && passPhrase.Length > 0)
                {
                    Console.Write("\b \b");
                    passPhrase = passPhrase[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    passPhrase += keyInfo.KeyChar;
                }
            }
            while (key != ConsoleKey.Enter || key == ConsoleKey.Enter && string.IsNullOrEmpty(passPhrase));

            Console.Write(Environment.NewLine);

            return passPhrase;
        }
    }
}
