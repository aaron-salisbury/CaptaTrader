using CaptaTrader.Data.Attributes;
using CaptaTrader.Data.Domains;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace CaptaTrader.Data
{
    public static class CRUD
    {
        private static readonly InternalStorage _internalStorage = Task.Run(() => InitializeInternalStorageAsync()).Result;

        public static async Task<T?> ReadDataAsync<T>(ILogger logger)
        {
            try
            {
                string filePath = Path.Combine(GetIODirectoryPath<T>(), GetJsonFileNameForType(typeof(T)));

                if (File.Exists(filePath))
                {
                    using (FileStream openStream = File.OpenRead(filePath))
                    {
                        return await JsonSerializer.DeserializeAsync<T>(openStream);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default, ex, "Failed to read from file system.");
            }

            return default;
        }

        public static async Task UpdateDataAsync<T>(T data, ILogger logger)
        {
            if (data != null)
            {
                try
                {
                    FileInfo file = new(Path.Combine(GetIODirectoryPath<T>(), GetJsonFileNameForType(typeof(T))));

                    if (file != null && file.Directory != null)
                    {
                        file.Directory.Create();

                        using (FileStream createStream = File.Create(file.FullName))
                        {
                            await JsonSerializer.SerializeAsync<T>(createStream, data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(default, ex, "Failed to write to file system.");
                }
            }
        }

        public static async Task<bool> UpdateUserStorageDirectoryAsync<T>(string newUserDirectory, ILogger logger)
        {
            bool isSuccessful = false;

            try
            {
                if (!string.IsNullOrEmpty(newUserDirectory))
                {
                    MoveOrDeleteAppData<T>(newUserDirectory, logger);

                    _internalStorage.UserStorageDirectory = newUserDirectory;
                    await UpdateDataAsync<InternalStorage>(_internalStorage, logger);

                    isSuccessful = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default, ex, "Failed to update internal storage.");
            }

            return isSuccessful;
        }

        public static void DeleteDomain<T>(ILogger logger)
        {
            try
            {
                string filePath = Path.Combine(GetIODirectoryPath<T>(), GetJsonFileNameForType(typeof(T)));

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default, ex, "Failed to delete file.");
            }
        }

        public static string GetCurrentUserStorageDirectory()
        {
            return _internalStorage.UserStorageDirectory;
        }

        private static async Task<InternalStorage> InitializeInternalStorageAsync()
        {
            ILogger logger = LoggerFactory.Create(static builder => builder.AddConsole()).CreateLogger("InternalStorage");
            InternalStorage? internalStorage = ReadDataAsync<InternalStorage?>(logger)?.Result;

            if (internalStorage != null)
            {
                return internalStorage;
            }
            else
            {
                internalStorage = new InternalStorage() { UserStorageDirectory = GetAppDirectoryPath() };

                await UpdateDataAsync<InternalStorage>(internalStorage, logger);

                return internalStorage;
            }
        }

        private static void MoveOrDeleteAppData<T>(string userDirectory, ILogger logger)
        {
            try
            {
                if (!string.IsNullOrEmpty(userDirectory))
                {
                    string appDataFileName = GetJsonFileNameForType(typeof(T));
                    string oldAppDataFilePath = Path.Combine(_internalStorage.UserStorageDirectory, appDataFileName);
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
                            CRUD.DeleteDomain<T>(logger);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to move/delete application data: {e.Message}", "ERROR");
            }
        }

        private static string GetIODirectoryPath<T>()
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

        private static string GetJsonFileNameForType(Type domainType)
        {
            Attribute[] attributes = Attribute.GetCustomAttributes(domainType);
            Attribute? baseNameAttribute = attributes.Where(a => a is BaseNameAttribute).FirstOrDefault();
            if (baseNameAttribute != null)
            {
                return $"{((BaseNameAttribute)baseNameAttribute).BaseName}.json";
            }

            string typeName = domainType.ToString();
            int pos = typeName.LastIndexOf('.') + 1;
            string objectName = typeName.Substring(pos, typeName.Length - pos);

            return $"{objectName}.json";
        }
    }
}
