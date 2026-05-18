using System;
using System.Collections.Generic;
using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Console.Presentation
{
    /// <summary>
    /// HUD 表现接口 (Console 平台实现)
    /// 定义游戏 HUD 的表现契约
    /// </summary>
    public interface IHudPresentation
    {
        void Show();
        void Hide();
        void Reset();
        void UpdateActorHp(int actorId, int currentHp, int maxHp);
        void UpdateActorMp(int actorId, int currentMp, int maxMp);
        void UpdateActorLevel(int actorId, int level);
        void UpdateSkillCooldown(int actorId, int slotIndex, float remainingSeconds);
        void ResetSkillCooldown(int actorId, int slotIndex);
        void ShowSkillReady(int actorId, int slotIndex);
        void UpdateItemSlot(int actorId, int slotIndex, int itemId);
        void UpdateItemCooldown(int actorId, int slotIndex, float remainingSeconds);
        void UpdateGold(int actorId, int gold);
        void UpdateExperience(int actorId, int currentExp, int expToNextLevel);
        void ShowKillFeed(int killerId, int victimId, KillFeedType killType);
        void ShowScore(int teamId, int score);
        void UpdateMinimapIcon(int actorId, float x, float z, MinimapIconType iconType);
        void ShowMinimapAlert(float x, float z, MinimapAlertType alertType);
    }

    public enum KillFeedType
    {
        Normal = 0,
        Ace = 1,
        FirstBlood = 2,
        Assist = 3,
    }

    public enum MinimapIconType
    {
        AllyHero = 0,
        EnemyHero = 1,
        Minion = 2,
        Neutral = 3,
        Ward = 4,
        AllyTower = 5,
        EnemyTower = 6,
    }

    public enum MinimapAlertType
    {
        EnemySpotted = 0,
        Assemble = 1,
        NeedHelp = 2,
        Warning = 3,
    }
}
