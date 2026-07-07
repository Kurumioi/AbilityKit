using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleAreaViewHandle
    {
        public int AreaId { get; set; }
        public int TemplateId { get; set; }
        public GameObject ModelGo { get; set; }
        public GameObject VfxGo { get; set; }
        public GameObject RangeGo { get; set; }

        public void Destroy()
        {
            if (ModelGo != null)
            {
                Object.Destroy(ModelGo);
                ModelGo = null;
            }

            if (RangeGo != null)
            {
                Object.Destroy(RangeGo);
                RangeGo = null;
            }

            if (VfxGo != null)
            {
                Object.Destroy(VfxGo);
                VfxGo = null;
            }
        }
    }
}
