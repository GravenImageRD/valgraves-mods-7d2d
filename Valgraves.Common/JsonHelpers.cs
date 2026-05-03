using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Valgraves.Common
{
    public class ValgravesConfig<TConfigType> where TConfigType : new()
    {
        public static TConfigType LoadFromJson() 
        {
            var exePath = Path.GetDirectoryName(typeof(TConfigType).Assembly.Location) ?? string.Empty;
            var configPath = Path.Combine(exePath, $"{typeof(TConfigType).Name}.json");
            if (File.Exists(configPath))
            {
                Logging.Warning($"Loading config file '{configPath}'");
                return JsonConvert.DeserializeObject<TConfigType>(File.ReadAllText(configPath));
            }
            
            Logging.Warning($"Failed to find config file '{configPath}'");
            return new TConfigType();
        }
    }
}