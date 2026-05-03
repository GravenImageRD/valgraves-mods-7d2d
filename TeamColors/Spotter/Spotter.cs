using System;
using System.Collections.Generic;
using HarmonyLib;
using UniLinq;
using UnityEngine;
using Valgraves.Common;

namespace TeamColors
{
    [HarmonyPatch(typeof(PlayerMoveController), nameof(PlayerMoveController.Update))]
    public class Spotter_SpotAllies
    {
        public static void Postfix(PlayerMoveController __instance)
        {
            if (TeamColors.SpotterActions == null)
            {
                TeamColors.SpotterActions = new SpotterActions();
            }
            
            if (!TeamColors.SpotterActions.SpotAllies.WasPressed)
            {
                return;
            }

            try
            {
                List<EntityPlayer> partyMembers = Player.Entity.Party?.MemberList ??  new List<EntityPlayer>();
                if (partyMembers.Count == 0)
                {
                    Logging.Warning("No party members found");
                    return;
                }

                var activeVehicles = VehicleManager.Instance.vehiclesActive;
                foreach (var ally in partyMembers)
                {
                    NavObject allyNavObject = ally.NavObject;
                    if (allyNavObject == null)
                    {
                        //Logging.Warning($"Ally {ally.name} has no NavObject.");
                        continue;
                    }

                    NavObjectScreenSettings screenSettings = allyNavObject.CurrentScreenSettings;
                    if (screenSettings == null)
                    {
                        //Logging.Warning($"Ally {ally.name} has no ScreenSettings.");
                        continue;
                    }
                    
                    var allyData = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(ally.entityId);
                    var allyVehicles = activeVehicles.Where(x => x.GetOwner().Equals(allyData.PrimaryId)).ToList();
                    if (screenSettings.MaxDistance > 0)
                    {
                        screenSettings.MaxDistance = -1;
                        Color? allyColor = TC.GetPartyColorByEntityId(ally.entityId);
                        if (allyColor == null)
                        {
                            //Logging.Error($"Could not find ally color for {ally.name}");
                            continue;
                        }
                        
                        foreach (var vehicle in allyVehicles)
                        {
                            try
                            {
                                var vehicleNavObject = vehicle.NavObject;
                                if (vehicleNavObject == null)
                                {
                                    //Logging.Warning($"No NavObject for ally vehicle {vehicle.EntityName}, creating one.");
                                    AddVehicleNavObject(vehicle, allyColor.Value);
                                    continue;
                                }

                                var navObjectClass = vehicleNavObject.NavObjectClass;
                                if (navObjectClass == null)
                                {
                                    //Logging.Warning($"No NavObjectClass for ally vehicle {vehicle.EntityName}");
                                    continue;
                                }
                                
                                navObjectClass.RequirementType = NavObjectClass.RequirementTypes.None;
                            }
                            catch (Exception e)
                            {
                                Logging.Warning(e.ToString());
                            }
                        }
                    }
                    else
                    {
                        screenSettings.MaxDistance = 20;
                        foreach (var vehicle in allyVehicles)
                        {
                            try
                            {
                                RemoveVehicleNavObject(vehicle);
                            }
                            catch (Exception e)
                            {
                                Logging.Warning(e.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
            }
        }

        private static void AddVehicleNavObject(EntityVehicle vehicle, Color overrideColor)
        {
            vehicle.NavObject = NavObjectManager.Instance.RegisterNavObject(EntityClass.list[vehicle.entityClass].NavObject, vehicle.vehicle.GetMeshTransform());
            vehicle.NavObject.OverrideColor = overrideColor;
            vehicle.NavObject.UseOverrideColor = true;
            Player.Entity.Waypoints.UpdateEntityVehicleWayPoint(vehicle);
        }
        
        private static void RemoveVehicleNavObject(EntityVehicle vehicle)
        {
            if (vehicle.NavObject == null) return;
            NavObjectManager.Instance.UnRegisterNavObject(vehicle.NavObject);
            vehicle.NavObject = null;
        }
    }    
}