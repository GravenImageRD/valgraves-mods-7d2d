using System.IO;
using System.Reflection;
using UnityEngine;
using Valgraves.Common;

namespace RepairVision
{
    public class RepairVision : IModApi
    {
        private const string ModName = "com.RepairVision";

        public void InitMod(Mod _modInstance)
        {
            Log.Warning("Initializing RepairVision");
            Logging.RegisterMod(ModName);
            var harmony = new HarmonyLib.Harmony(_modInstance.Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}