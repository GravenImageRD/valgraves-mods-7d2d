using System;
using HarmonyLib;
using UnityEngine;
using Valgraves.Common;

namespace TeamColors
{
    [HarmonyPatch(typeof(NavObjectManager), nameof(NavObjectManager.RegisterNavObject))]
    [HarmonyPatch(new Type[] { typeof(string), typeof(Vector3), typeof(string), typeof(bool), typeof(int), typeof(Entity) })]
    public class NavObjectManager_RegisterNavObject
    {
        public static void Postfix(NavObject __result)
        {
            if (Player.Entity == null) return;
            
            try
            {
                if (__result.OwnerEntity == null ||  __result.OwnerEntity.entityId == Player.EntityId) return;
                
                Color? allyColor = TC.GetPartyColorByEntityId(__result.OwnerEntity.entityId);
                if (allyColor == null)
                {
                    return;
                }
                
                __result.OverrideColor = allyColor.Value;
                __result.UseOverrideColor = true;
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
            }
        }
    }
}