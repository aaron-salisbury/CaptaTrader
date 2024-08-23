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
    }
}
