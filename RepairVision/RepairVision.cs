using System.Reflection;
using HarmonyLib;
using Valgraves.Common;

namespace RepairVision
{
    public class RepairVision : IModApi
    {
        private const string ModName = "com.Valgraves.RepairVision";
        public static RepairVisionConfig Config;
        public static RepairVisionActions RepairVisionActions;

        public void InitMod(Mod _modInstance)
        {
            var harmony = new Harmony(_modInstance.Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());   
            Config = RepairVisionConfig.LoadFromJson();
            if (Config.DebugLogging)
            {
                Logging.EnableDebugLogging();
            }
            RepairVisionActions = new RepairVisionActions();
            BlockHelpers.Initialize();
        }
    }
}