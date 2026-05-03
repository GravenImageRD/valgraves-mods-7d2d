using System.Collections.Generic;
using Valgraves.Common;

namespace RepairVision
{
    public class RepairVisionConfig : ValgravesConfig<RepairVisionConfig>
    {
        public List<string> ToggleRepairVisionKeyBind { get; set; } = new List<string>();
    }
}