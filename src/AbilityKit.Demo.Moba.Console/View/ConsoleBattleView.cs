using System;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.View
{
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
    /// </summary>
    public sealed class ConsoleBattleView : IConsoleBattleView
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

        #region 视图方法

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

            foreach (var kvp in _areaViews.GetAll())
            {
                var area = kvp.Value;
                var (cx, cy) = _renderer.WorldToScreen(area.CenterX, area.CenterZ);
                _renderer.DrawText(cx - 2, cy, $"[@{area.Radius}]");
            }

            foreach (var proj in _projectileDisplay.GetAll())
            {
                var (px, py) = _renderer.WorldToScreen(proj.X, proj.Z);
                var stateChar = proj.State == ProjectileState.Flying ? "*" : (proj.State == ProjectileState.Hit ? "X" : "-");
                _renderer.DrawText(px, py, stateChar);
            }

            foreach (var entity in _entityDisplay.GetAll())
            {
                var (px, py) = _renderer.WorldToScreen(entity.X, entity.Z);

                _renderer.DrawText(px, py - 1, entity.Name);
                _renderer.DrawText(px, py, entity.IsDead ? "X" : "O");

                if (!entity.IsDead)
                {
                    var hpWidth = (int)(entity.HpPercent * 10);
                    var hpBar = new string('|', hpWidth) + new string('-', 10 - hpWidth);
                    _renderer.DrawText(px - 2, py + 1, hpBar);
                }
            }

            foreach (var ft in _floatingTexts.GetAll())
            {
                if (_entityDisplay.TryGet(ft.TargetActorId, out var target))
                {
                    var (tx, ty) = _renderer.WorldToScreen(target.X, target.Z);
                    var offsetY = (int)(ft.Age * ft.VelocityY * 10);
                    var text = ft.IsHeal ? $"+{ft.Text}" : ft.Text;
                    _renderer.DrawText(tx, ty - 2 - offsetY, text);
                }
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
