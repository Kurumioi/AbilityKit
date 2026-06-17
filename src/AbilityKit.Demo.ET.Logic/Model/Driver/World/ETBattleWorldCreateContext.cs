using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    public readonly struct ETBattleWorldCreateContext
    {
        public ETBattleWorldCreateContext(in BattleStartPlan plan, IReadOnlyList<ETPlayerSpawnData> playerSpawnData, ITextAssetLoader textAssetLoader)
        {
            Plan = plan;
            PlayerSpawnData = playerSpawnData;
            TextAssetLoader = textAssetLoader;
        }

        public BattleStartPlan Plan { get; }
        public IReadOnlyList<ETPlayerSpawnData> PlayerSpawnData { get; }
        public ITextAssetLoader TextAssetLoader { get; }
    }
}
