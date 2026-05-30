using System.Collections.Generic;
using UnityEngine;
using Valgraves.Common;

namespace RepairVision
{
    public class RepairVisionConfig : ValgravesConfig<RepairVisionConfig>
    {
        private Color _startColor = new Color(1f, 0.92156863f, 0.015686275f, 0.25f);
        private Color _endColor = new Color(0.8f, 0f, 0f, 0.25f);
        
        public List<string> ToggleRepairVisionKeyBind { get; set; } = new List<string>();
        public float DamageThreshold { get; set; } = 0.9f;
        public string StartColor { get; set; } = "FFEB04";
        public string EndColor { get; set; } = "CC0000";
        public int ScanRange { get; set; } = 25;

        public Color GetStartColor()
        {
            if (!ColorUtility.TryParseHtmlString(RepairVision.Config.StartColor, out _startColor))
            {
                Logging.Warning($"Failed to convert StartColor {RepairVision.Config.StartColor} to Color, is it a valid HTML color code?");
            }
            return _startColor;
        }
        
        public Color GetEndColor()
        {
            if (!ColorUtility.TryParseHtmlString(RepairVision.Config.EndColor, out _endColor))
            {
                Logging.Warning($"Failed to convert EndColor {RepairVision.Config.EndColor} to Color, is it a valid HTML color code?"); 
            }
            return _endColor;
        }
    }
}