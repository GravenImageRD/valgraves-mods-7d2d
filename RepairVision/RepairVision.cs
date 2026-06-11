using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Valgraves.Common;

namespace RepairVision
{
    public class RepairVision : IModApi
    {
        private const string ModName = "com.Valgraves.RepairVision";
        public static RepairVisionConfig Config;
        public static RepairVisionManager Manager;

        public void InitMod(Mod _modInstance)
        {
            var harmony = new Harmony(_modInstance.Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());   
            Config = RepairVisionConfig.LoadFromJson();
            if (Config.DebugLogging)
            {
                Logging.EnableDebugLogging();
            }
            
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var bundlePath = Path.Combine(exePath, "Resources", "repairvision.unity3d");
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            
            Manager = new RepairVisionManager(Config, bundle);
            BlockHelpers.Initialize(bundle);
        }
    }
}