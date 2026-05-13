using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Core.Battle.ECS.Components;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Services;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 实体显示信息
    /// </summary>
    public sealed class ConsoleEntityInfo
    {
        public int ActorId;
        public string Name;
        public string Type;
        public float X;
        public float Y;
        public float Z;
        public float Hp;
        public float MaxHp;

        public bool IsDead => Hp <= 0;
        public float HpPercent => MaxHp > 0 ? Hp / MaxHp : 0f;
    }

    /// <summary>
    /// Console 实体显示服务
    /// </summary>
    public sealed class ConsoleEntityDisplayService
    {
        private readonly Dictionary<int, ConsoleEntityInfo> _entities = new();

        public void Add(int actorId, string name, string type, float hp, float maxHp, float x, float y, float z)
        {
            if (!_entities.TryGetValue(actorId, out var info))
            {
                info = new ConsoleEntityInfo();
                _entities[actorId] = info;
            }

            info.ActorId = actorId;
            info.Name = name;
            info.Type = type;
            info.X = x;
            info.Y = y;
            info.Z = z;
            info.Hp = hp;
            info.MaxHp = maxHp;
        }

        public void UpdatePosition(int actorId, float x, float y, float z)
        {
            if (_entities.TryGetValue(actorId, out var info))
            {
                info.X = x;
                info.Y = y;
                info.Z = z;
            }
        }

        public void UpdateHp(int actorId, float hp, float maxHp)
        {
            if (_entities.TryGetValue(actorId, out var info))
            {
                info.Hp = hp;
                info.MaxHp = maxHp;
            }
        }

        public void Remove(int actorId) => _entities.Remove(actorId);
        public bool TryGet(int actorId, out ConsoleEntityInfo info) => _entities.TryGetValue(actorId, out info);
        public IEnumerable<ConsoleEntityInfo> GetAll() => _entities.Values;
        public int Count => _entities.Count;
        public void Clear() => _entities.Clear();
    }

    /// <summary>
    /// 视图接口
    /// </summary>
    public interface IConsoleBattleView : IDisposable
    {
        void OnGameStart(int playerCount);
        void UpdateActorPosition(int actorId, float x, float y, float z);
        void ShowFloatingText(int targetActorId, string text, bool isHeal);
        void ShowProjectileSpawn(int projectileId, int templateId, float x, float y, float z);
        void ShowProjectileHit(int templateId, float x, float y, float z);
        void ShowProjectileExpire(int projectileId);
        void ShowAreaEffectStart(int areaId, int templateId, float centerX, float centerZ, float radius);
        void ShowAreaEffectEnd(int areaId);
        void ShowBuffApply(int targetId, int buffId, int casterId);
        void ShowBuffRemove(int targetId, int buffId);
        void RegisterEntity(int actorId, string name, string type, float hp, float maxHp, float x, float y, float z);
        void UpdateEntityHp(int actorId, float hp, float maxHp);
        void Tick(float deltaTime);
        void Render();

        ConsoleEntityDisplayService EntityDisplay { get; }
    }

    /// <summary>
    /// Console 战斗视图
    /// 实现 BattleViewServices 接口用于战斗服务调用
    /// </summary>
    public sealed class ConsoleBattleView : IConsoleBattleView, BattleViewServices
    {
        private readonly ConsoleEntityDisplayService _entityDisplay;
        private readonly ConsoleFloatingTextSystem _floatingTexts;
        private readonly ConsoleAreaViewSystem _areaViews;
        private readonly ConsoleProjectileDisplayService _projectileDisplay;
        private readonly IRenderer _renderer;
        private bool _disposed;
        private int _playerCount;

        public ConsoleBattleView(
            ConsoleEntityDisplayService entityDisplay,
            ConsoleFloatingTextSystem floatingTexts,
            ConsoleAreaViewSystem areaViews,
            ConsoleProjectileDisplayService projectileDisplay,
            IRenderer renderer)
        {
            _entityDisplay = entityDisplay ?? throw new ArgumentNullException(nameof(entityDisplay));
            _floatingTexts = floatingTexts ?? throw new ArgumentNullException(nameof(floatingTexts));
            _areaViews = areaViews ?? throw new ArgumentNullException(nameof(areaViews));
            _projectileDisplay = projectileDisplay ?? throw new ArgumentNullException(nameof(projectileDisplay));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public ConsoleEntityDisplayService EntityDisplay => _entityDisplay;

        #region BattleViewServices 实现

        /// <summary>
        /// 显示伤害飘字
        /// </summary>
        void BattleViewServices.ShowDamage(int actorId, float hp, float hpMax)
        {
            UpdateEntityHp(actorId, hp, hpMax);
        }

        /// <summary>
        /// 显示技能效果
        /// </summary>
        void BattleViewServices.ShowSkillEffect(int actorId, int skillId)
        {
            Log.Skill($"[View] Actor #{actorId} skill effect {skillId}");
        }

        /// <summary>
        /// 显示死亡
        /// </summary>
        void BattleViewServices.ShowDeath(int actorId, int killerActorId)
        {
            ShowFloatingText(actorId, "DIED!", false);
            _entityDisplay.Remove(actorId);
            Log.Battle($"[View] Actor #{actorId} died, killed by #{killerActorId}");
        }

        #endregion

        #region IConsoleBattleView 实现

        public void OnGameStart(int playerCount)
        {
            _playerCount = playerCount;
            Log.View($"Game started with {playerCount} players");
        }

        public void UpdateActorPosition(int actorId, float x, float y, float z)
        {
            _entityDisplay.UpdatePosition(actorId, x, y, z);
        }

        public void ShowFloatingText(int targetActorId, string text, bool isHeal)
        {
            _floatingTexts.Spawn(targetActorId, text, isHeal);
        }

        public void ShowProjectileSpawn(int projectileId, int templateId, float x, float y, float z)
        {
            _projectileDisplay.Spawn(projectileId, templateId, x, y, z);
            Log.Projectile($"Spawn: #{projectileId} at ({x:F1}, {y:F1}, {z:F1})");
        }

        public void ShowProjectileHit(int templateId, float x, float y, float z)
        {
            Log.Projectile($"Hit: Template#{templateId} at ({x:F1}, {y:F1}, {z:F1})");
        }

        public void ShowProjectileExpire(int projectileId)
        {
            _projectileDisplay.Remove(projectileId);
            Log.Projectile($"Expire: #{projectileId}");
        }

        public void ShowAreaEffectStart(int areaId, int templateId, float centerX, float centerZ, float radius)
        {
            _areaViews.Spawn(areaId, templateId, centerX, centerZ, radius);
            Log.Area($"Start: #{areaId} Center=({centerX:F1}, {centerZ:F1}) Radius={radius:F1}");
        }

        public void ShowAreaEffectEnd(int areaId)
        {
            _areaViews.Remove(areaId);
            Log.Area($"End: #{areaId}");
        }

        public void ShowBuffApply(int targetId, int buffId, int casterId)
        {
            Log.Buff($"Apply: #{targetId} gains Buff#{buffId} from #{casterId}");
        }

        public void ShowBuffRemove(int targetId, int buffId)
        {
            Log.Buff($"Remove: #{targetId} loses Buff#{buffId}");
        }

        public void RegisterEntity(int actorId, string name, string type, float hp, float maxHp, float x, float y, float z)
        {
            _entityDisplay.Add(actorId, name, type, hp, maxHp, x, y, z);
            Log.Entity($"Spawn: #{actorId} {name} ({type}) HP={hp:F0}");
        }

        public void UpdateEntityHp(int actorId, float hp, float maxHp)
        {
            _entityDisplay.UpdateHp(actorId, hp, maxHp);
        }

        public void Tick(float deltaTime)
        {
            _floatingTexts.Tick();
        }

        public void Render()
        {
            _renderer.Clear();

            foreach (var entity in _entityDisplay.GetAll())
            {
                var (px, py) = _renderer.WorldToScreen(entity.X, entity.Z);
                _renderer.DrawText(px, py, entity.IsDead ? "X" : "O");
            }

            _renderer.Present();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _floatingTexts.Clear();
            _areaViews.Clear();
            _projectileDisplay.Clear();
        }

        #endregion
    }
}
