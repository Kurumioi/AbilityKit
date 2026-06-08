using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class ResourceBattleViewShellLoader : IBattleViewShellLoader
    {
        public GameObject CreateShellGameObject(int actorId, int modelId)
        {
            return BattleViewFactory.CreateShellGameObject(actorId, modelId);
        }
    }
}
