using System.Collections.Generic;
using Valgraves.Common;

namespace TeamColors
{
    public class TeamColorsConfig : ValgravesConfig<TeamColorsConfig>
    {
        public List<string> ToggleSpotterKeyBind { get; set; } = new List<string>();
    }
}