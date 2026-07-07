using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleAreaViewObjectFactory
    {
        private readonly BattleViewResourceProvider _resources;
        private readonly BattleAreaViewObjectPlacer _placer;

        public BattleAreaViewObjectFactory(
            BattleViewResourceProvider resources = null,
            BattleAreaViewObjectPlacer placer = null)
        {
            _resources = BattleViewResourceProvider.OrDefault(resources);
            _placer = placer ?? new BattleAreaViewObjectPlacer();
        }

        public GameObject CreateModel(int modelId, Transform attach, in Vector3 position)
        {
            return CreateAndPlace(modelId, createVfx: false, attach, in position);
        }

        public GameObject CreateRange(int templateId, float radius, int delayMs, Transform attach, in Vector3 position)
        {
            var go = _resources.CreateAoeRangeGo(templateId, radius, delayMs);
            if (go == null) return null;

            _placer.Place(go, attach, in position);
            return go;
        }

        public GameObject CreateVfx(int vfxId, Transform attach, in Vector3 position)
        {
            return CreateAndPlace(vfxId, createVfx: true, attach, in position);
        }

        private GameObject CreateAndPlace(int viewId, bool createVfx, Transform attach, in Vector3 position)
        {
            var go = createVfx
                ? _resources.CreateVfxGo(viewId)
                : _resources.CreateModelGo(viewId);
            if (go == null) return null;

            _placer.Place(go, attach, in position);
            return go;
        }
    }

    internal sealed class BattleAreaViewObjectPlacer
    {
        public void Place(GameObject go, Transform attach, in Vector3 position)
        {
            if (go == null) return;

            if (attach != null)
            {
                go.transform.SetParent(attach, worldPositionStays: false);
                go.transform.localPosition = Vector3.zero;
                return;
            }

            go.transform.position = position;
        }
    }
}
