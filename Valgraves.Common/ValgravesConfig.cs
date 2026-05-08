using System.IO;
using Newtonsoft.Json;

namespace Valgraves.Common
{
    public class ValgravesConfig<TConfigType> where TConfigType : new()
    {
        /// <summary>
        /// If enabled any logging will also be sent to the client via in game chat.
        /// </summary>
        public bool LogToChat { get; set; } = false;
        
        /// <summary>
        /// If enabled all logging will be written out, otherwise only errors will be.
        /// </summary>
        public bool DebugLogging { get; set; } = false;
        
        /// <summary>
        /// Loads a config file from JSON using the default format of:
        ///   [ModFolder]\[ConfigTypeName].json
        /// Ex: For RepairVisionConfig in the RepairVision mod it would load RepairVision\RepairVisionConfig.json
        /// </summary>
        /// <returns>The loaded version of the configuration object.</returns>
        public static TConfigType LoadFromJson() 
        {
            var exePath = Path.GetDirectoryName(typeof(TConfigType).Assembly.Location) ?? string.Empty;
            var configPath = Path.Combine(exePath, $"{typeof(TConfigType).Name}.json");
            if (File.Exists(configPath))
            {
                //Logging.Warning($"Loading config file '{configPath}'");
                return JsonConvert.DeserializeObject<TConfigType>(File.ReadAllText(configPath));
            }
            
            Logging.Warning($"Failed to find config file '{configPath}'");
            return new TConfigType();
        }
    }
}