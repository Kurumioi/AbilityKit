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
    /// - 作为状态同步客户端，只管理单位数据
    /// - 数据由快照更新，不自己做计算
    /// - 不包含任何游戏业务逻辑（伤害、Buff、移动等）
    /// </summary>
    [EntitySystemOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETUnitComponentSystem
    {
        // Unit dictionary (static storage)
        private static readonly Dictionary<long, ETUnit> _units = new();
        private static ETUnitComponent? _current;

        [EntitySystem]
        private static void Awake(this ETUnitComponent self)
        {
            _units.Clear();
            _current = self;
        }

        [EntitySystem]
        private static void Destroy(this ETUnitComponent self)
        {
            foreach (var unit in _units.Values)
            {
                unit.Dispose();
            }
            _units.Clear();
            _current = null;
        }

        #region Basic CRUD

        /// <summary>
        /// 创建单位
        /// </summary>
        public static ETUnit CreateUnit(
            this ETUnitComponent self,
            long actorId,
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
            var unit = self.AddChild<ETUnit>();
            unit.ActorId = actorId;
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

            _units[actorId] = unit;

            Log.Info($"[ETUnit] Unit created: {name} ({actorId}) at ({x}, {y})");

            return unit;
        }

        /// <summary>
        /// 获取单位
        /// </summary>
        public static ETUnit? GetUnit(this ETUnitComponent self, long actorId)
        {
            return _units.TryGetValue(actorId, out var unit) ? unit : null;
        }

        /// <summary>
        /// 获取所有单位
        /// </summary>
        public static IEnumerable<ETUnit> GetAllUnits(this ETUnitComponent self)
        {
            return _units.Values;
        }

        /// <summary>
        /// 获取特定类型的单位
        /// </summary>
        public static IEnumerable<ETUnit> GetUnitsByKind(this ETUnitComponent self, ActorKind kind)
        {
            foreach (var unit in _units.Values)
            {
                if (unit.Kind == kind)
                    yield return unit;
            }
        }

        /// <summary>
        /// 获取本地玩家单位
        /// </summary>
        public static ETUnit? GetLocalPlayerUnit(this ETUnitComponent self)
        {
            foreach (var unit in _units.Values)
            {
                if (unit.IsLocalPlayer)
                    return unit;
            }
            return null;
        }

        /// <summary>
        /// 获取第一个单位
        /// </summary>
        public static ETUnit? GetFirstUnit(this ETUnitComponent self)
        {
            foreach (var unit in _units.Values)
            {
                return unit;
            }
            return null;
        }

        /// <summary>
        /// 移除单位
        /// </summary>
        public static void RemoveUnit(this ETUnitComponent self, long actorId)
        {
            if (_units.TryGetValue(actorId, out var unit))
            {
                _units.Remove(actorId);
                unit.Dispose();
                Log.Info($"[ETUnit] Unit removed: {actorId}");
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// 获取单位数量
        /// </summary>
        public static int UnitCount(this ETUnitComponent self)
        {
            return _units.Count;
        }

        /// <summary>
        /// 获取存活单位数量
        /// </summary>
        public static int AliveUnitCount(this ETUnitComponent self)
        {
            int count = 0;
            foreach (var unit in _units.Values)
            {
                if (!unit.IsDead)
                    count++;
            }
            return count;
        }

        #endregion

        #region ❌ 已删除的业务逻辑

        // ❌ ExecuteDamage() - 伤害由 moba.core 计算，通过快照更新
        // ❌ FindUnitsInRange() - 范围查询由 moba.core 处理
        // ❌ Tick() - 移动和冷却由 moba.core 计算，通过快照更新
        // ❌ OnUnitDead() - 死亡由 moba.core 检测，通过快照更新

        #endregion
    }
}
