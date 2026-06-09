using AbilityKit.Demo.Moba.Config.Core;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewModelFactory
    {
        private readonly BattleViewPrimitiveFactory _primitives;
        private readonly BattleViewModelPrefabResolver _prefabs;
        private readonly BattleViewAttachRootUtility _attachRoots;

        public BattleViewModelFactory(
            BattleViewPrimitiveFactory primitives = null,
            BattleViewModelPrefabResolver prefabs = null,
            BattleViewAttachRootUtility attachRoots = null)
        {
            _primitives = primitives ?? new BattleViewPrimitiveFactory();
            _prefabs = prefabs ?? new BattleViewModelPrefabResolver();
            _attachRoots = attachRoots ?? new BattleViewAttachRootUtility();
        }

        public GameObject CreateActorShell(MobaConfigDatabase configs, int actorId, int modelId)
        {
            var model = _prefabs.Resolve(configs, modelId);

            GameObject go;
            if (model.HasPrefab)
            {
                go = Object.Instantiate(model.Prefab);
                var s = model.Scale <= 0f ? 1f : model.Scale;
                go.transform.localScale = new Vector3(s, s, s);
            }
            else
            {
                go = _primitives.CreateActorFallback();
            }

            go.name = $"Actor_{actorId}";
            _attachRoots.Ensure(go);
            return go;
        }

        public GameObject CreateAoeModel(MobaConfigDatabase configs, int modelId)
        {
            if (modelId <= 0) return null;

            var model = _prefabs.Resolve(configs, modelId);

            GameObject go;
            if (model.HasPrefab)
            {
                go = Object.Instantiate(model.Prefab);
            }
            else
            {
                go = _primitives.CreateAoeModelFallback();
            }

            go.name = $"AoeModel_{modelId}";
            return go;
        }
    }
}
