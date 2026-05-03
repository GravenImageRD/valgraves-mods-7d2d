using InControl;
using Platform;
using Valgraves.Common;

namespace TeamColors
{
    public class SpotterActions : PlayerActionsBase
    {
        public PlayerAction SpotAllies;

        public SpotterActions()
        {
            Name = "spotterActions";
            
            PlayerActionsLocal actions = PlatformManager.NativePlatform.Input.PrimaryPlayer;
            UserData = new PlayerActionData.ActionSetUserData(new PlayerActionsBase[] { actions });
            actions.AddBindingConflictWithActionSet(this);
        }

        public override void CreateActions()
        {
            SpotAllies = CreatePlayerAction("Spot Allies");
            SpotAllies.UserData = new PlayerActionData.ActionUserData("teamColorsSpotAllies", "teamColorsSpotAlliesTooltip", PlayerActionData.GroupUI, PlayerActionData.EAppliesToInputType.KbdMouseOnly, true);
            SpotAllies.LoadKeyBinding(TeamColors.Config.ToggleSpotterKeyBind, KeyCombo.With(Key.Alt, Key.P));
        }

        public override void CreateDefaultKeyboardBindings()
        {
        }

        public override void CreateDefaultJoystickBindings()
        {
        }
    }
}