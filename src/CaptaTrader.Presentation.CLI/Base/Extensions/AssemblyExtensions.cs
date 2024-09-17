using System.IO;
using System.Linq;
using System.Reflection;

namespace CaptaTrader.Presentation.CLI.Base.Extensions;

public static class AssemblyExtensions
{
    public static string? ReadResource(this Assembly assembly, string name)
    {
        // ref: https://stackoverflow.com/a/3314213

        string resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(name));

        using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
        {
            if (stream != null)
            {
                using (StreamReader reader = new(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        return null;
    }
}
