using System;
using HarmonyLib;
using Valgraves.Common;

namespace RepairVision
{
    [HarmonyPatch(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.Update))]
    public class RepairVisionUpdate
    {
        public static void Postfix(EntityPlayerLocal __instance)
        {
            try
            {
                if (RepairVision.Manager.SkipProcessing(__instance))
                {
                    return;
                }

                RepairVision.Manager.UpdateScan(__instance);
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
            }
        }
    }
}