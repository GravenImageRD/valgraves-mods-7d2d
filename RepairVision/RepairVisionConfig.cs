using System.Collections.Generic;
using Valgraves.Common;

namespace RepairVision
{
    public class RepairVisionConfig : ValgravesConfig<RepairVisionConfig>
    {
        public List<string> ToggleRepairVisionKeyBind { get; set; } = new List<string>();
        public float DamageThreshold { get; set; } = 0.9f;
        public string StartColor { get; set; } = "FFEB04";
        public string EndColor { get; set; } = "CC0000";
        public int ScanRange { get; set; } = 25;
    }
}