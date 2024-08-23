using CaptaTrader.Data.DataAccess;
using CaptaTrader.Data.Domains;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CaptaTrader.Data.Base
{
    internal class IO
    {
        private static readonly InternalStorage _internalStorage = Task.Run(() => Json.ReadInternalStorageAsync()).Result;

        public static string GetCurrentUserStorageDirectory() => _internalStorage.UserStorageDirectory;

        public static async Task<bool> UpdateUserStorageDirectoryAsync<T>(string newUserDirectory, ILogger logger)
        {
            bool isSuccessful = false;

            try
            {
                if (!string.IsNullOrEmpty(newUserDirectory))
                {
                    MoveOrDeleteAppData<T>(newUserDirectory, logger);

                    _internalStorage.UserStorageDirectory = newUserDirectory;
                    await Json.UpdateDataAsync<InternalStorage>(_internalStorage, logger);

                    isSuccessful = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default, ex, "Failed to update internal storage.");
            }

            return isSuccessful;
        }

        private static void MoveOrDeleteAppData<T>(string userDirectory, ILogger logger)
        {
            try
            {
                if (!string.IsNullOrEmpty(userDirectory))
                {
                    string appDataFileName = Json.GetJsonFileNameForType(typeof(T));
                    string oldAppDataFilePath = Path.Combine(IO.GetCurrentUserStorageDirectory(), appDataFileName);
                    string newAppDataFilePath = Path.Combine(userDirectory, appDataFileName);

                    if (File.Exists(oldAppDataFilePath))
                    {
                        if (!File.Exists(newAppDataFilePath))
                        {
                            Directory.Move(oldAppDataFilePath, newAppDataFilePath);
                        }
                        else
                        {
                            // If the file already exists at the new location, they chose it for a reason; delete the previous one.
                            Json.DeleteDomain<T>(logger);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to move/delete application data: {e.Message}", "ERROR");
            }
        }

        internal static string GetIODirectoryPath<T>()
        {
            if (typeof(T) == typeof(InternalStorage))
            {
                return GetAppDirectoryPath();
            }
            else
            {
                return GetCurrentUserStorageDirectory();
            }
        }

        internal static string GetAppDirectoryPath()
        {
            string appPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string? assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string? appName = assemblyName?.Substring(0, assemblyName.IndexOf('.'));

            string appDirectoryPath = !string.IsNullOrEmpty(appName) ? Path.Combine(appPath, appName) : appPath;
            return appDirectoryPath;
        }
    }
}
