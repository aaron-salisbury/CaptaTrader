using CaptaTrader.Data.Base.Attributes;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CaptaTrader.Data.Base
{
    public static class Json
    {
        public static async Task<T?> ReadDataAsync<T>(ILogger logger)
        {
            try
            {
                string filePath = Path.Combine(IO.GetIODirectoryPath<T>(), GetJsonFileNameForType(typeof(T)));

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
                logger.LogError(ex, "Failed to read from file system.");
            }

            return default;
        }

        internal static async Task<InternalStorage> ReadInternalStorageAsync()
        {
            ILogger logger = LoggerFactory.Create(static builder => builder.AddConsole()).CreateLogger("InternalStorage");
            InternalStorage? internalStorage = ReadDataAsync<InternalStorage?>(logger)?.Result;

            if (internalStorage != null)
            {
                return internalStorage;
            }
            else
            {
                internalStorage = new InternalStorage() { UserStorageDirectory = IO.GetAppDirectoryPath() };

                await UpdateDataAsync(internalStorage, logger);

                return internalStorage;
            }
        }

        public static async Task UpdateDataAsync<T>(T data, ILogger logger)
        {
            if (data != null)
            {
                try
                {
                    FileInfo file = new(Path.Combine(IO.GetIODirectoryPath<T>(), GetJsonFileNameForType(typeof(T))));

                    if (file != null && file.Directory != null)
                    {
                        file.Directory.Create();

                        using (FileStream createStream = File.Create(file.FullName))
                        {
                            await JsonSerializer.SerializeAsync(createStream, data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to write to file system.");
                }
            }
        }

        public static void DeleteDomain<T>(ILogger logger)
        {
            try
            {
                string filePath = Path.Combine(IO.GetIODirectoryPath<T>(), GetJsonFileNameForType(typeof(T)));

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete file.");
            }
        }

        internal static string GetJsonFileNameForType(Type domainType)
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
