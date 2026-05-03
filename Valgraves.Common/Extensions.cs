using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using InControl;
using UniLinq;

namespace Valgraves.Common
{
    public static class Extensions
    {
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