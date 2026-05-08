using System;
using System.Collections.Generic;
using InControl;
using UniLinq;

namespace Valgraves.Common.Extensions
{
    /// <summary>
    /// Extensions for various InControl features.
    /// </summary>
    public static class KeyBindingExtensions
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

        /// <summary>
        /// Attempts to convert a list of string values into a <see cref="KeyCombo"/> binding. If that
        /// fails it will use the passed in default binding instead.
        /// </summary>
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
    }
}