using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using InControl;
using UniLinq;
using UnityEngine;

namespace Valgraves.Common
{
    public static class Extensions
    {
        public static Vector3i FloorToInt(this Vector3 self)
        {
            return new Vector3i(
                Utils.Fastfloor(self.x),
                Utils.Fastfloor(self.y),
                Utils.Fastfloor(self.z)
            );
        }
        
        public static double Magnitude(this Vector3i vector)
        {
            return Math.Sqrt((vector.x * vector.x) + (vector.y * vector.y) + (vector.z * vector.z));
        }
        
        public static void AddBindingConflictWithActionSet(this PlayerActionsBase self, PlayerActionsBase other)
        {
            PlayerActionData.ActionSetUserData data = self.UserData as PlayerActionData.ActionSetUserData;
            if(data == null)
            {
                self.UserData = new PlayerActionData.ActionSetUserData( new PlayerActionsBase[]
                {
                    other
                });

                return;
            }

            List<PlayerActionsBase> list = new List<PlayerActionsBase>(data.bindingsConflictWithSet);
            if (!list.Contains(other))
            {
                list.Add(other);
                self.UserData = new PlayerActionData.ActionSetUserData(list.ToArray());
            }
        }

        public static void LoadKeyBinding(this PlayerAction self, List<string> keyStrings, KeyCombo defaultBinding)
        {
            var keyBind = defaultBinding;
            if (keyStrings.Any())
            {
                var loadedKeyBind = new KeyCombo();
                foreach (var key in keyStrings)
                {
                    if (!Enum.TryParse(key, out Key keyEnum))
                    {
                        Logging.Error($"Could not parse {self.Name} binding key '{key}', will use default bind");
                        break;
                    }

                    loadedKeyBind.AddInclude(keyEnum);
                }
                
                keyBind = loadedKeyBind;
            }

            self.ClearBindings();
            self.AddBinding(new KeyBindingSource(keyBind));
        }

        public static string GetAssemblyFolder(this Type caller)
        {
            return Path.GetDirectoryName(caller.Assembly.Location);
        }
    }
}