using System.Reflection;
using Valgraves.Common;

namespace TeamColors
{
    public class TeamColors : IModApi
    {
        private const string ModName = "com.TeamColors";
        
        public void InitMod(Mod _modInstance)
        {
            Log.Warning("Initializing Team Colors");
            Logging.RegisterMod(ModName);
            var harmony = new HarmonyLib.Harmony(_modInstance.Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Spotter.Initialize();
        }
    }
}