using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class ResourceBattleViewShellLoader : IBattleViewShellLoader
    {
        private readonly BattleViewResourceProvider _resources;

        public ResourceBattleViewShellLoader(BattleViewResourceProvider resources = null)
        {
            _resources = BattleViewResourceProvider.OrDefault(resources);
        }

        public GameObject CreateShellGameObject(int actorId, int modelId)
        {
            return _resources.CreateShellGameObject(actorId, modelId);
        }
    }
}
