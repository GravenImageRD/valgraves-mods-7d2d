using UnityEngine;

namespace RepairVision.Objects
{
    public class RepairVisionBlock
    {
        public GameObject GameObject { get; set; }
        public Vector3i Position { get; set; }

        public void Update(float hpPercent)
        {
            var blockColor = Color.Lerp(RepairVision.Config.GetEndColor(), RepairVision.Config.GetStartColor(), hpPercent);
            foreach (var renderer in GameObject.GetComponentsInChildren<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    material.SetColor("_Color", blockColor);
                }
            }
        }
    }
}