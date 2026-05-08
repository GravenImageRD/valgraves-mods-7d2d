using InControl;
using Platform;
using Valgraves.Common.Extensions;

namespace RepairVision
{
    
    public class RepairVisionActions : PlayerActionsBase
    {
        public PlayerAction ToggleRepairVision;

        public RepairVisionActions()
        {
            Name = "repairVisionActions";
            PlayerActionsLocal actions = PlatformManager.NativePlatform.Input.PrimaryPlayer;
            UserData = new PlayerActionData.ActionSetUserData(actions);
            actions.AddBindingConflictWithActionSet(this);
        }

        public override void CreateActions()
        {
            ToggleRepairVision = CreatePlayerAction("Toggle Repair Vision");
            ToggleRepairVision.UserData = new PlayerActionData.ActionUserData("repairVisionToggle", "repairVisionToggleTooltip", PlayerActionData.GroupUI, PlayerActionData.EAppliesToInputType.KbdMouseOnly, true);
            ToggleRepairVision.LoadKeyBinding(RepairVision.Config.ToggleRepairVisionKeyBind, new KeyCombo(Key.Control, Key.V));
        }

        public override void CreateDefaultKeyboardBindings()
        {
        }

        public override void CreateDefaultJoystickBindings()
        {
        }
    }
}