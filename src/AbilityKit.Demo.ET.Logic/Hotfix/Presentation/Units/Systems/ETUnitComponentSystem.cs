using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETUnitComponent System
    /// 管理 ETUnit 生命周期
    ///
    /// 设计说明：
    /// - 使用 ET 原生的 AddChild/GetChild 管理子实体
    /// - ETUnit.Id 使用 moba.core 的 ActorId
    /// - 创建: AddChild&lt;ETUnit&gt;((long)actorId)
    /// - 查询: GetChild&lt;ETUnit&gt;(actorId)
    /// </summary>
    [EntitySystemOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETUnitComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETUnitComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ETUnitComponent self)
        {
        }

        #region Basic CRUD

        /// <summary>
        /// 创建单位
        /// 使用 AddChildWithId&lt;ETUnit&gt;(actorId) 让 ET 自动管理
        /// </summary>
        public static ETUnit CreateUnit(
            this ETUnitComponent self,
            int actorId,
            int entityCode,
            ActorKind kind,
            string name,
            float x = 0,
            float y = 0,
            float maxHp = 100f,
            float attack = 10f,
            float defense = 5f,
            float moveSpeed = 5f,
            bool isLocalPlayer = false)
        {
            // Idempotency check: return existing unit if already created
            var existingUnit = self.GetChild<ETUnit>((long)actorId);
            if (existingUnit != null)
            {
                Log.Warning($"[ETUnit] Unit already exists: ActorId={actorId}, returning existing unit");
                return existingUnit;
            }

            // 使用 actorId 作为 ET 实体 ID
            var unit = self.AddChildWithId<ETUnit>((long)actorId);
            unit.LogicActorId = actorId;
            unit.EntityCode = entityCode;
            unit.Kind = kind;
            unit.Name = name;
            unit.X = x;
            unit.Y = y;
            unit.MaxHp = maxHp;
            unit.Hp = maxHp;
            unit.Attack = attack;
            unit.Defense = defense;
            unit.MoveSpeed = moveSpeed;
            unit.IsLocalPlayer = isLocalPlayer;
            unit.RenderX = x;
            unit.RenderY = y;

            Log.Info($"[ETUnit] Unit created: {name} (ActorId={actorId}, EntityCode={entityCode}) at ({x}, {y})");

            return unit;
        }

        /// <summary>
        /// 获取单位
        /// 使用 ET 原生的 GetChild 方法
        /// </summary>
        public static ETUnit? GetUnit(this ETUnitComponent self, int actorId)
        {
            return self.GetChild<ETUnit>((long)actorId);
        }

        /// <summary>
        /// 获取所有单位
        /// </summary>
        public static IEnumerable<ETUnit> GetAllUnits(this ETUnitComponent self)
        {
            foreach (var entity in self.Children.Values)
            {
                if (entity is ETUnit unit)
                    yield return unit;
            }
        }

        /// <summary>
        /// 获取特定类型的单位
        /// </summary>
        public static IEnumerable<ETUnit> GetUnitsByKind(this ETUnitComponent self, ActorKind kind)
        {
            foreach (var child in self.Children.Values)
            {
                if (child is ETUnit unit && unit.Kind == kind)
                    yield return unit;
            }
        }

        /// <summary>
        /// 获取本地玩家单位
        /// </summary>
        public static ETUnit? GetLocalPlayerUnit(this ETUnitComponent self)
        {
            foreach (var child in self.Children.Values)
            {
                if (child is ETUnit unit && unit.IsLocalPlayer)
                    return unit;
            }
            return null;
        }

        /// <summary>
        /// 获取第一个单位
        /// </summary>
        public static ETUnit? GetFirstUnit(this ETUnitComponent self)
        {
            foreach (var child in self.Children.Values)
            {
                if (child is ETUnit unit)
                    return unit;
            }
            return null;
        }

        /// <summary>
        /// 移除单位
        /// </summary>
        public static void RemoveUnit(this ETUnitComponent self, int actorId)
        {
            var unit = self.GetChild<ETUnit>((long)actorId);
            if (unit != null)
            {
                unit.Dispose();
                Log.Info($"[ETUnit] Unit removed: ActorId={actorId}");
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// 获取单位数量
        /// </summary>
        public static int UnitCount(this ETUnitComponent self)
        {
            return self.Children.Count;
        }

        /// <summary>
        /// 获取存活单位数量
        /// </summary>
        public static int AliveUnitCount(this ETUnitComponent self)
        {
            int count = 0;
            foreach (var child in self.Children.Values)
            {
                if (child is ETUnit unit && !unit.IsDead)
                    count++;
            }
            return count;
        }

        #endregion
    }
}
