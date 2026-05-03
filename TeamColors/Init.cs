using System.Reflection;
using HarmonyLib;

namespace TeamColors
{
    public class TeamColors : IModApi
    {
        private const string ModName = "com.Valgraves.TeamColors";
        
        private static TeamColorsConfig _config = null;
        public static SpotterActions SpotterActions;
        
        public void InitMod(Mod _modInstance)
        {
            Log.Warning("Initializing Team Colors");
            var harmony = new Harmony(_modInstance.Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static TeamColorsConfig Config => _config ?? (_config = TeamColorsConfig.LoadFromJson());
    }
}