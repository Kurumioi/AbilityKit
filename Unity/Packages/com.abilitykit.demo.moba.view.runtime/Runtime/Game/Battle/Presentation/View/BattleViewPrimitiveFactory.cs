using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewPrimitiveFactory
    {
        public GameObject CreateActorFallback()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(1f, 2f, 1f);
            return go;
        }

        public GameObject CreateAoeModelFallback()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = Vector3.one * 0.5f;
            return go;
        }

        public GameObject CreateVfxFallback()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = Vector3.one * 0.5f;
            return go;
        }
    }
}
