using UnityEngine;

namespace AbilityKit.Game.Flow
{
    public interface IBattleViewShellLoader
    {
        GameObject CreateShellGameObject(int actorId, int modelId);
    }
}
