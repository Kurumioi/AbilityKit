# AbilityKit Moba Console 视图层架构设计

## 1. 概述

### 1.1 设计目标

参考 Unity 视图层 (`com.abilitykit.demo.moba.view.runtime`)，为 Console 环境实现完整的视图层，满足以下要求：

| 目标 | 描述 |
|-----|------|
| **功能完整** | 完整复刻 Unity 视图层的所有功能 |
| **更规范化** | 相比 Unity 实现更加规范、职责清晰 |
| **可测试** | 支持 AI 运行排查和自检 |
| **零依赖** | 不依赖 Unity，可在纯 C# 环境运行 |

### 1.2 核心设计理念

Console 视图层的核心设计理念与 Unity 视图层一致：**数据与表现分离**

```
┌─────────────────────────────────────────────────────────────────┐
│                         逻辑层 (Runtime)                         │
│  - 技能系统、战斗系统、伤害计算                                  │
│  - 状态管理、实体管理                                            │
│  - 事件发布 (IBattleViewEventSink, ISkillLifecycleObserver)     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ 事件/快照
┌─────────────────────────────────────────────────────────────────┐
│                         视图层 (View)                            │
│  - 接收逻辑层事件                                                │
│  - 格式化输出到 Console                                          │
│  - 状态查询 (IBattleEntityQuery)                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. 架构设计

### 2.1 项目结构

```
src/AbilityKit.Demo.Moba.Console/
├── AbilityKit.Demo.Moba.Console.csproj
├── Program.cs                              # 入口点，启动游戏循环
│
├── View/                                   # Console 视图层 (核心)
│   ├── IConsoleBattleViewEventSink.cs      # 视图事件接口
│   ├── ConsoleBattleViewEventSink.cs      # 视图事件实现
│   ├── ConsoleEntityDisplayService.cs      # 实体显示服务
│   ├── ConsoleSnapshotPresenter.cs          # 快照展示器
│   ├── ConsoleSkillLifecyclePrinter.cs     # 技能生命周期打印
│   ├── ConsoleBattleView.cs                # 主视图 (协调器)
│   ├── ConsoleViewFactory.cs               # 视图工厂
│   └── Rendering/
│       ├── IConsoleRenderer.cs             # 渲染器接口
│       ├── AsciiRenderer.cs                # ASCII 渲染器
│       ├── TextTableRenderer.cs            # 文本表格渲染器
│       └── BattleMapRenderer.cs            # 战场地图渲染器
│
├── Bootstrap/                              # 启动引导
│   ├── ConsoleBattleBootstrapper.cs        # Console 引导器
│   └── ConsoleConfigLoader.cs              # Console 配置加载
│
└── Infrastructure/                        # 平台基础设施
    ├── ConsoleLogSink.cs                   # Console 日志实现
    ├── ConsoleTextResourceProvider.cs      # 文本资源实现
    ├── ConsoleBinaryResourceProvider.cs    # 二进制资源实现
    └── ConsoleConfigSource.cs              # 配置源实现
```

### 2.2 核心接口设计

#### 2.2.1 `IConsoleBattleViewEventSink` - 视图事件接口

```csharp
namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 视图事件接收器
    /// 对应 Unity 的 IBattleViewEventSink
    /// </summary>
    public interface IConsoleBattleViewEventSink
    {
        void OnTriggerEvent(in TriggerEvent evt);
        void OnEnterGameSnapshot(ISnapshotEnvelope packet, EnterMobaGameRes res);
        void OnActorTransformSnapshot(ISnapshotEnvelope packet, (int actorId, float x, float y, float z)[] entries);
        void OnProjectileEventSnapshot(ISnapshotEnvelope packet, MobaProjectileEventSnapshotCodec.Entry[] entries);
        void OnAreaEventSnapshot(ISnapshotEnvelope packet, MobaAreaEventSnapshotCodec.Entry[] entries);
        void OnDamageEventSnapshot(ISnapshotEnvelope packet, MobaDamageEventSnapshotCodec.Entry[] entries);
    }
}
```

#### 2.2.2 `IConsoleRenderer` - 渲染器接口

```csharp
namespace AbilityKit.Demo.Moba.Console.View.Rendering
{
    /// <summary>
    /// Console 渲染器接口
    /// 抽象不同的渲染模式
    /// </summary>
    public interface IConsoleRenderer
    {
        string Render();
        void Clear();
        void AppendLine(string text);
    }

    /// <summary>
    /// 战场地图渲染器
    /// 使用 ASCII 字符渲染战场地图
    /// </summary>
    public interface IBattleMapRenderer : IConsoleRenderer
    {
        /// <summary>
        /// 更新实体位置
        /// </summary>
        void UpdateEntityPosition(int actorId, float x, float y, float z);

        /// <summary>
        /// 更新投射物位置
        /// </summary>
        void UpdateProjectilePosition(int projectileId, float x, float y, float z);

        /// <summary>
        /// 标记伤害数字
        /// </summary>
        void MarkDamage(int actorId, string damageText, bool isHeal);

        /// <summary>
        /// 渲染特效区域
        /// </summary>
        void RenderAreaEffect(int areaId, float centerX, float centerY, float centerZ, float radius);

        /// <summary>
        /// 设置视野范围
        /// </summary>
        void SetVisibleBounds(float minX, float minY, float minZ, float maxX, float maxY, float maxZ);
    }
}
```

#### 2.2.3 `IConsoleEntityDisplayService` - 实体显示服务

```csharp
namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// 实体显示服务接口
    /// 管理实体的 Console 显示信息
    /// </summary>
    public interface IConsoleEntityDisplayService
    {
        /// <summary>
        /// 注册实体
        /// </summary>
        void RegisterEntity(int actorId, string name, string entityType);

        /// <summary>
        /// 注销实体
        /// </summary>
        void UnregisterEntity(int actorId);

        /// <summary>
        /// 更新实体状态
        /// </summary>
        void UpdateEntityState(int actorId, float hp, float maxHp, float x, float y, float z);

        /// <summary>
        /// 获取实体显示信息
        /// </summary>
        bool TryGetEntityDisplay(int actorId, out EntityDisplayInfo info);

        /// <summary>
        /// 获取所有实体显示信息
        /// </summary>
        IReadOnlyList<EntityDisplayInfo> GetAllEntityDisplays();

        /// <summary>
        /// 清空所有实体
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 实体显示信息
    /// </summary>
    public readonly struct EntityDisplayInfo
    {
        public int ActorId { get; init; }
        public string Name { get; init; }
        public string EntityType { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }
        public float Hp { get; init; }
        public float MaxHp { get; init; }
        public float HpPercent => MaxHp > 0 ? Hp / MaxHp : 0f;
        public bool IsDead => Hp <= 0;
    }
}
```

### 2.3 核心类设计

#### 2.3.1 `ConsoleBattleViewEventSink` - 视图事件实现

```csharp
namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 视图事件接收器实现
    /// 接收逻辑层事件并输出到 Console
    /// </summary>
    public sealed class ConsoleBattleViewEventSink : IConsoleBattleViewEventSink
    {
        private readonly IConsoleEntityDisplayService _entityDisplay;
        private readonly IBattleMapRenderer _mapRenderer;
        private readonly ConsoleSkillLifecyclePrinter _skillPrinter;

        public ConsoleBattleViewEventSink(
            IConsoleEntityDisplayService entityDisplay,
            IBattleMapRenderer mapRenderer,
            ConsoleSkillLifecyclePrinter skillPrinter)
        {
            _entityDisplay = entityDisplay ?? throw new ArgumentNullException(nameof(entityDisplay));
            _mapRenderer = mapRenderer ?? throw new ArgumentNullException(nameof(mapRenderer));
            _skillPrinter = skillPrinter ?? throw new ArgumentNullException(nameof(skillPrinter));
        }

        public void OnTriggerEvent(in TriggerEvent evt)
        {
            if (evt.Id == null) return;

            // 处理技能触发事件
            if (evt.Id == DamagePipelineEvents.AfterApply)
            {
                if (evt.Payload is DamageResult r)
                {
                    HandleDamageResult(r);
                }
            }
        }

        private void HandleDamageResult(DamageResult r)
        {
            if (r == null) return;
            if (r.TargetActorId <= 0) return;
            if (r.Value == 0f) return;

            var isHeal = r.Value < 0f;
            var text = isHeal ? $"+{Math.Abs(r.Value):0.#}" : $"-{Math.Abs(r.Value):0.#}";
            _mapRenderer.MarkDamage(r.TargetActorId, text, isHeal);

            // 输出伤害数字到 Console
            var color = isHeal ? ConsoleColor.Green : ConsoleColor.Red;
            LogWithColor($"[DAMAGE] Actor#{r.TargetActorId} {text}", color);
        }

        public void OnEnterGameSnapshot(ISnapshotEnvelope packet, EnterMobaGameRes res)
        {
            Log.Info("[GAME] ================== GAME START ==================");
            RefreshAllViews();
        }

        public void OnActorTransformSnapshot(ISnapshotEnvelope packet, (int actorId, float x, float y, float z)[] entries)
        {
            foreach (var entry in entries)
            {
                _mapRenderer.UpdateEntityPosition(entry.actorId, entry.x, entry.y, entry.z);

                if (_entityDisplay.TryGetEntityDisplay(entry.actorId, out var info))
                {
                    _entityDisplay.UpdateEntityState(entry.actorId, info.Hp, info.MaxHp, entry.x, entry.y, entry.z);
                }
            }
        }

        public void OnProjectileEventSnapshot(ISnapshotEnvelope packet, MobaProjectileEventSnapshotCodec.Entry[] entries)
        {
            foreach (var evt in entries)
            {
                var kind = (MobaProjectileEventSnapshotCodec.EventKind)evt.Kind;
                var vfxName = kind switch
                {
                    MobaProjectileEventSnapshotCodec.EventKind.Spawn => "SPAWN",
                    MobaProjectileEventSnapshotCodec.EventKind.Hit => "HIT",
                    MobaProjectileEventSnapshotCodec.EventKind.Exit => "EXPIRE",
                    _ => "UNKNOWN"
                };

                Log.Info($"[PROJECTILE] #{evt.ProjectileActorId} [{vfxName}] at ({evt.X:F1}, {evt.Y:F1}, {evt.Z:F1})");

                _mapRenderer.UpdateProjectilePosition(evt.ProjectileActorId, evt.X, evt.Y, evt.Z);
            }
        }

        public void OnAreaEventSnapshot(ISnapshotEnvelope packet, MobaAreaEventSnapshotCodec.Entry[] entries)
        {
            foreach (var evt in entries)
            {
                var kind = (MobaAreaEventSnapshotCodec.EventKind)evt.Kind;
                Log.Info($"[AREA] #{evt.AreaId} [{kind}] Center=({evt.CenterX:F1}, {evt.CenterY:F1}, {evt.CenterZ:F1}) Radius={evt.Radius:F1}");

                _mapRenderer.RenderAreaEffect(evt.AreaId, evt.CenterX, evt.CenterY, evt.CenterZ, evt.Radius);
            }
        }

        public void OnDamageEventSnapshot(ISnapshotEnvelope packet, MobaDamageEventSnapshotCodec.Entry[] entries)
        {
            foreach (var e in entries)
            {
                if (e.TargetActorId <= 0) continue;
                if (e.Value == 0f) continue;

                var isHeal = e.Kind == (int)MobaDamageEventSnapshotCodec.EventKind.Heal;
                var text = isHeal ? $"+{Math.Abs(e.Value):0.#}" : $"-{Math.Abs(e.Value):0.#}";

                _mapRenderer.MarkDamage(e.TargetActorId, text, isHeal);

                var color = isHeal ? ConsoleColor.Green : ConsoleColor.Red;
                LogWithColor($"[DAMAGE] Actor#{e.TargetActorId} {text}", color);
            }
        }

        private void RefreshAllViews()
        {
            var allEntities = _entityDisplay.GetAllEntityDisplays();
            foreach (var entity in allEntities)
            {
                _mapRenderer.UpdateEntityPosition(entity.ActorId, entity.X, entity.Y, entity.Z);
            }
        }

        private static void LogWithColor(string message, ConsoleColor color)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = original;
        }
    }
}
```

#### 2.3.2 `ConsoleSkillLifecyclePrinter` - 技能生命周期打印

```csharp
namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 技能生命周期打印器
    /// 实现 ISkillLifecycleObserver 接口
    /// </summary>
    public sealed class ConsoleSkillLifecyclePrinter : SkillLifecycleObserverAdapter
    {
        private readonly bool _enableDetailedOutput;
        private readonly Dictionary<long, SkillInstanceInfo> _activeSkills = new();

        public ConsoleSkillLifecyclePrinter(bool enableDetailedOutput = true)
        {
            _enableDetailedOutput = enableDetailedOutput;
        }

        public int TotalSkillStarted { get; private set; }
        public int TotalSkillCompleted { get; private set; }
        public int TotalSkillFailed { get; private set; }
        public int TotalBuffApplied { get; private set; }

        protected override void OnStartRequested(in SkillLifecycleEvent evt)
        {
            TotalSkillStarted++;

            Log.Info($"[SKILL] #{evt.CasterActorId} requests Skill#{evt.SkillId} " +
                    $"(Slot={evt.SkillSlot} Level={evt.SkillLevel}) " +
                    $"Target=#{evt.TargetActorId} Instance={evt.InstanceId}");

            _activeSkills[evt.InstanceId] = new SkillInstanceInfo
            {
                CasterId = evt.CasterActorId,
                SkillId = evt.SkillId,
                InstanceId = evt.InstanceId,
                StartTime = DateTime.UtcNow
            };
        }

        protected override void OnStartSucceeded(in SkillLifecycleEvent evt)
        {
            Log.Info($"[SKILL] #{evt.CasterActorId} Skill#{evt.SkillId} STARTED (Instance={evt.InstanceId})");
        }

        protected override void OnStartFailed(in SkillLifecycleEvent evt)
        {
            TotalSkillFailed++;
            Log.Warning($"[SKILL] #{evt.CasterActorId} Skill#{evt.SkillId} FAILED: {evt.StringParam} (Instance={evt.InstanceId})");
            _activeSkills.Remove(evt.InstanceId);
        }

        protected override void OnPhaseStarting(in SkillLifecycleEvent evt)
        {
            if (!_enableDetailedOutput) return;
            Log.Info($"[SKILL]   Phase '{evt.PhaseId}' starting for Instance={evt.InstanceId}");
        }

        protected override void OnPhaseCompleted(in SkillLifecycleEvent evt)
        {
            if (!_enableDetailedOutput) return;
            Log.Info($"[SKILL]   Phase '{evt.PhaseId}' completed for Instance={evt.InstanceId}");
        }

        protected override void OnCompleted(in SkillLifecycleEvent evt)
        {
            TotalSkillCompleted++;
            var duration = GetDuration(evt.InstanceId);
            Log.Info($"[SKILL] #{evt.CasterActorId} Skill#{evt.SkillId} COMPLETED " +
                    $"(Instance={evt.InstanceId} Duration={duration.TotalMilliseconds:F0}ms)");
            _activeSkills.Remove(evt.InstanceId);
        }

        protected override void OnCancelled(in SkillLifecycleEvent evt)
        {
            TotalSkillFailed++;
            Log.Warning($"[SKILL] #{evt.CasterActorId} Skill#{evt.SkillId} CANCELLED: {evt.StringParam}");
            _activeSkills.Remove(evt.InstanceId);
        }

        protected override void OnInterrupted(in SkillLifecycleEvent evt)
        {
            TotalSkillFailed++;
            Log.Warning($"[SKILL] #{evt.CasterActorId} Skill#{evt.SkillId} INTERRUPTED at Phase={evt.PhaseId}");
            _activeSkills.Remove(evt.InstanceId);
        }

        protected override void OnBuffApplied(in SkillLifecycleEvent evt)
        {
            TotalBuffApplied++;
            Log.Info($"[BUFF] #{evt.TargetActorId} gains Buff#{evt.IntParam} from #{evt.CasterActorId} Skill#{evt.SkillId} (Instance={evt.InstanceId})");
        }

        protected override void OnBuffRemoved(in SkillLifecycleEvent evt)
        {
            Log.Info($"[BUFF] #{evt.TargetActorId} loses Buff#{evt.IntParam}: {evt.StringParam}");
        }

        protected override void OnPassiveTriggered(in SkillLifecycleEvent evt)
        {
            Log.Info($"[PASSIVE] #{evt.CasterActorId} Passive#{evt.SkillId} triggered: {evt.StringParam} Result={evt.BoolParam}");
        }

        protected override void OnTriggerEvaluated(in SkillLifecycleEvent evt)
        {
            if (!_enableDetailedOutput) return;
            Log.Info($"[TRIGGER] Evaluation '{evt.StringParam}' = {evt.BoolParam} for Instance={evt.InstanceId}");
        }

        protected override void OnTriggerExecuted(in SkillLifecycleEvent evt)
        {
            if (!_enableDetailedOutput) return;
            var actionType = evt.ExtraData.TryGetValue("ActionType", out var at) ? at?.ToString() : "unknown";
            Log.Info($"[TRIGGER] Execute '{evt.StringParam}' Action={actionType}");
        }

        public string GetStatisticsReport()
        {
            return $"""
                === Skill Statistics ===
                Started:    {TotalSkillStarted}
                Completed:  {TotalSkillCompleted}
                Failed:     {TotalSkillFailed}
                BuffApplied:{TotalBuffApplied}
                Active:     {_activeSkills.Count}
                """;
        }

        private TimeSpan GetDuration(long instanceId)
        {
            if (_activeSkills.TryGetValue(instanceId, out var info))
            {
                return DateTime.UtcNow - info.StartTime;
            }
            return TimeSpan.Zero;
        }

        private struct SkillInstanceInfo
        {
            public int CasterId;
            public int SkillId;
            public long InstanceId;
            public DateTime StartTime;
        }
    }
}
```

#### 2.3.3 `BattleMapRenderer` - 战场地图渲染器

```csharp
namespace AbilityKit.Demo.Moba.Console.View.Rendering
{
    /// <summary>
    /// ASCII 战场地图渲染器
    /// 使用 ASCII 字符在 Console 中渲染战场
    /// </summary>
    public sealed class BattleMapRenderer : IBattleMapRenderer
    {
        private const int MapWidth = 80;
        private const int MapHeight = 40;
        private const char EmptyChar = '.';
        private const char PlayerChar = '@';
        private const char EnemyChar = 'X';
        private const char NpcChar = 'N';
        private const char ProjectileChar = '*';
        private const char AreaChar = '~';
        private const char DamageChar = '!';
        private const char HealChar = '+';

        private readonly char[,] _map = new char[MapHeight, MapWidth];
        private readonly Dictionary<int, MapPosition> _entities = new();
        private readonly Dictionary<int, MapPosition> _projectiles = new();
        private readonly Dictionary<int, DamageMarker> _damageMarkers = new();
        private readonly Dictionary<int, AreaEffect> _areaEffects = new();

        private float _worldMinX = -50f;
        private float _worldMaxX = 50f;
        private float _worldMinZ = -50f;
        private float _worldMaxZ = 50f;

        private readonly List<string> _outputBuffer = new();

        public BattleMapRenderer()
        {
            Clear();
        }

        public void SetVisibleBounds(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            _worldMinX = minX;
            _worldMaxX = maxX;
            _worldMinZ = minZ;
            _worldMaxZ = maxZ;
        }

        public void UpdateEntityPosition(int actorId, float x, float y, float z)
        {
            _entities[actorId] = new MapPosition(x, z);
        }

        public void UpdateProjectilePosition(int projectileId, float x, float y, float z)
        {
            _projectiles[projectileId] = new MapPosition(x, z);
        }

        public void MarkDamage(int actorId, string damageText, bool isHeal)
        {
            if (_entities.TryGetValue(actorId, out var pos))
            {
                _damageMarkers[actorId] = new DamageMarker(damageText, isHeal, pos);
            }
        }

        public void RenderAreaEffect(int areaId, float centerX, float centerY, float centerZ, float radius)
        {
            _areaEffects[areaId] = new AreaEffect(centerX, centerZ, radius);
        }

        public void Clear()
        {
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    _map[y, x] = EmptyChar;
                }
            }
            _outputBuffer.Clear();
        }

        public void AppendLine(string text)
        {
            _outputBuffer.Add(text);
        }

        public string Render()
        {
            Clear();

            // 渲染边界
            DrawBorder();

            // 渲染范围效果
            RenderAreaEffects();

            // 渲染投射物
            RenderProjectiles();

            // 渲染实体
            RenderEntities();

            // 渲染伤害数字
            RenderDamageMarkers();

            // 生成输出
            return BuildOutput();
        }

        private void DrawBorder()
        {
            for (int x = 0; x < MapWidth; x++)
            {
                _map[0, x] = '-';
                _map[MapHeight - 1, x] = '-';
            }
            for (int y = 0; y < MapHeight; y++)
            {
                _map[y, 0] = '|';
                _map[y, MapWidth - 1] = '|';
            }
            _map[0, 0] = '+';
            _map[0, MapWidth - 1] = '+';
            _map[MapHeight - 1, 0] = '+';
            _map[MapHeight - 1, MapWidth - 1] = '+';
        }

        private void RenderAreaEffects()
        {
            foreach (var area in _areaEffects.Values)
            {
                var (minPx, maxPx, minPy, maxPy) = WorldToScreenBounds(
                    area.CenterX - area.Radius, area.CenterZ - area.Radius,
                    area.CenterX + area.Radius, area.CenterZ + area.Radius);

                for (int py = minPy; py <= maxPy; py++)
                {
                    for (int px = minPx; px <= maxPx; px++)
                    {
                        if (IsInBounds(px, py) && IsInCircle(px, py, area))
                        {
                            _map[py, px] = AreaChar;
                        }
                    }
                }
            }
        }

        private void RenderProjectiles()
        {
            foreach (var proj in _projectiles.Values)
            {
                var (px, py) = WorldToScreen(proj.X, proj.Z);
                if (IsInBounds(px, py))
                {
                    _map[py, px] = ProjectileChar;
                }
            }
        }

        private void RenderEntities()
        {
            foreach (var entity in _entities)
            {
                var (px, py) = WorldToScreen(entity.Value.X, entity.Value.Z);
                if (IsInBounds(px, py))
                {
                    // 根据 ID 判断阵营 (示例)
                    _map[py, px] = entity.Key % 2 == 0 ? PlayerChar : EnemyChar;
                }
            }
        }

        private void RenderDamageMarkers()
        {
            foreach (var marker in _damageMarkers)
            {
                var (px, py) = WorldToScreen(marker.Position.X, marker.Position.Z);
                // 在实体上方渲染
                if (py > 1 && IsInBounds(px, py - 1))
                {
                    _map[py - 1, px] = marker.IsHeal ? HealChar : DamageChar;
                }
            }
        }

        private string BuildOutput()
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔" + new string('═', MapWidth - 2) + "╗");
            for (int y = 1; y < MapHeight - 1; y++)
            {
                sb.Append('║');
                for (int x = 1; x < MapWidth - 1; x++)
                {
                    sb.Append(_map[y, x]);
                }
                sb.AppendLine("║");
            }
            sb.AppendLine("╚" + new string('═', MapWidth - 2) + "╝");
            sb.AppendLine();

            // 附加信息
            sb.AppendLine("--- Entities ---");
            foreach (var entity in _entities)
            {
                if (_damageMarkers.TryGetValue(entity.Key, out var marker))
                {
                    sb.AppendLine($"#{entity.Key}: ({entity.Value.X:F1}, {entity.Value.Z:F1}) {marker.Text}");
                }
                else
                {
                    sb.AppendLine($"#{entity.Key}: ({entity.Value.X:F1}, {entity.Value.Z:F1})");
                }
            }

            return sb.ToString();
        }

        private (int px, int py) WorldToScreen(float worldX, float worldZ)
        {
            var px = (int)((worldX - _worldMinX) / (_worldMaxX - _worldMinX) * (MapWidth - 2)) + 1;
            var py = (int)((_worldMaxZ - worldZ) / (_worldMaxZ - _worldMinZ) * (MapHeight - 2)) + 1;
            px = Math.Clamp(px, 1, MapWidth - 2);
            py = Math.Clamp(py, 1, MapHeight - 2);
            return (px, py);
        }

        private (int minPx, int maxPx, int minPy, int maxPy) WorldToScreenBounds(
            float minX, float minZ, float maxX, float maxZ)
        {
            var (minPx, minPy) = WorldToScreen(minX, minZ);
            var (maxPx, maxPy) = WorldToScreen(maxX, maxZ);
            return (minPx, maxPx, minPy, maxPy);
        }

        private bool IsInBounds(int px, int py)
        {
            return px >= 1 && px < MapWidth - 1 && py >= 1 && py < MapHeight - 1;
        }

        private bool IsInCircle(int px, int py, AreaEffect area)
        {
            var (cx, cy) = WorldToScreen(area.CenterX, area.CenterZ);
            var radiusInScreen = area.Radius / (_worldMaxX - _worldMinX) * (MapWidth - 2);
            var dx = px - cx;
            var dy = py - cy;
            return dx * dx + dy * dy <= radiusInScreen * radiusInScreen;
        }

        private record MapPosition(float X, float Z);
        private record DamageMarker(string Text, bool IsHeal, MapPosition Position);
        private record AreaEffect(float CenterX, float CenterZ, float Radius);
    }
}
```

#### 2.3.4 `ConsoleBattleView` - 主视图协调器

```csharp
namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 战斗视图主协调器
    /// 管理所有视图组件的生命周期
    /// </summary>
    public sealed class ConsoleBattleView : IDisposable
    {
        private readonly BattleContext _battleContext;
        private readonly IConsoleBattleEventSink _eventSink;
        private readonly IBattleSnapshotViewAdapter _snapshotAdapter;
        private readonly IBattleEntityQuery _entityQuery;
        private readonly IConsoleEntityDisplayService _entityDisplay;
        private readonly IBattleMapRenderer _mapRenderer;
        private readonly ConsoleSkillLifecyclePrinter _skillPrinter;

        private bool _disposed;

        public ConsoleBattleView(
            BattleContext battleContext,
            IBattleEntityQuery entityQuery)
        {
            _battleContext = battleContext ?? throw new ArgumentNullException(nameof(battleContext));
            _entityQuery = entityQuery ?? throw new ArgumentNullException(nameof(entityQuery));

            // 初始化组件
            _entityDisplay = new ConsoleEntityDisplayService();
            _mapRenderer = new BattleMapRenderer();
            _skillPrinter = new ConsoleSkillLifecyclePrinter(enableDetailedOutput: true);

            // 初始化事件接收器
            _eventSink = new ConsoleBattleViewEventSink(
                _entityDisplay,
                _mapRenderer,
                _skillPrinter);

            // 初始化快照适配器
            if (_battleContext.FrameSnapshots != null)
            {
                _snapshotAdapter = new BattleSnapshotViewAdapter(
                    _battleContext.FrameSnapshots,
                    _eventSink);
            }
        }

        /// <summary>
        /// 渲染当前状态
        /// </summary>
        public string Render()
        {
            // 更新地图
            RefreshAllEntityPositions();

            // 返回渲染结果
            return _mapRenderer.Render();
        }

        /// <summary>
        /// 获取技能统计报告
        /// </summary>
        public string GetSkillStatisticsReport()
        {
            return _skillPrinter.GetStatisticsReport();
        }

        /// <summary>
        /// 获取实体状态报告
        /// </summary>
        public string GetEntityStatusReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Entity Status ===");

            var allEntities = _entityDisplay.GetAllEntityDisplays();
            foreach (var entity in allEntities.OrderBy(e => e.ActorId))
            {
                var status = entity.IsDead ? "[DEAD]" : $"HP={entity.Hp:F0}/{entity.MaxHp:F0} ({entity.HpPercent:P0})";
                sb.AppendLine($"#{entity.ActorId} {entity.Name,-10} {entity.EntityType,-10} {status}");
            }

            return sb.ToString();
        }

        private void RefreshAllEntityPositions()
        {
            if (_entityQuery?.World == null) return;

            _entityQuery.World.ForEachAlive(e =>
            {
                if (!e.TryGetRef(out BattleNetIdComponent netId) || netId.NetId.Value <= 0) return;
                if (!e.TryGetRef(out BattleTransformComponent transform)) return;

                var actorId = netId.NetId.Value;

                if (!_entityDisplay.TryGetEntityDisplay(actorId, out var info))
                {
                    // 获取实体类型
                    var entityType = "Unknown";
                    if (e.TryGetRef(out BattleEntityMetaComponent meta))
                    {
                        entityType = meta.Kind.ToString();
                    }

                    _entityDisplay.RegisterEntity(actorId, $"Actor_{actorId}", entityType);
                }

                // 获取 HP
                var hp = 0f;
                var maxHp = 100f;
                if (e.TryGetRef(out BattleCharacterComponent character))
                {
                    hp = character.Hp;
                    maxHp = character.MaxHp;
                }

                _entityDisplay.UpdateEntityState(
                    actorId, hp, maxHp,
                    transform.Position.X, transform.Position.Y, transform.Position.Z);

                _mapRenderer.UpdateEntityPosition(actorId, transform.Position.X, transform.Position.Y, transform.Position.Z);
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _snapshotAdapter?.Dispose();
            _mapRenderer.Clear();
            _entityDisplay.Clear();
        }
    }
}
```

#### 2.3.5 `BattleSnapshotViewAdapter` - 快照视图适配器

```csharp
namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// 帧快照视图适配器
    /// 将 FrameSnapshotDispatcher 的事件路由到 Console 视图
    /// </summary>
    public sealed class BattleSnapshotViewAdapter : IDisposable
    {
        private readonly FrameSnapshotDispatcher _snapshots;
        private readonly IConsoleBattleViewEventSink _sink;

        private IDisposable _subEnterGame;
        private IDisposable _subActorTransform;
        private IDisposable _subProjectileEvents;
        private IDisposable _subAreaEvents;
        private IDisposable _subDamageEvents;

        public BattleSnapshotViewAdapter(
            FrameSnapshotDispatcher snapshots,
            IConsoleBattleViewEventSink sink)
        {
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));

            // 订阅快照事件
            _subEnterGame = _snapshots.Subscribe<EnterMobaGameRes>(
                (int)MobaOpCode.EnterGameSnapshot, _sink.OnEnterGameSnapshot);

            _subActorTransform = _snapshots.Subscribe<(int actorId, float x, float y, float z)[]>(
                (int)MobaOpCode.ActorTransformSnapshot, _sink.OnActorTransformSnapshot);

            _subProjectileEvents = _snapshots.Subscribe<MobaProjectileEventSnapshotCodec.Entry[]>(
                (int)MobaOpCode.ProjectileEventSnapshot, _sink.OnProjectileEventSnapshot);

            _subAreaEvents = _snapshots.Subscribe<MobaAreaEventSnapshotCodec.Entry[]>(
                (int)MobaOpCode.AreaEventSnapshot, _sink.OnAreaEventSnapshot);

            _subDamageEvents = _snapshots.Subscribe<MobaDamageEventSnapshotCodec.Entry[]>(
                (int)MobaOpCode.DamageEventSnapshot, _sink.OnDamageEventSnapshot);
        }

        public void Dispose()
        {
            _subEnterGame?.Dispose();
            _subActorTransform?.Dispose();
            _subProjectileEvents?.Dispose();
            _subAreaEvents?.Dispose();
            _subDamageEvents?.Dispose();
        }
    }
}
```

### 2.4 启动引导设计

#### 2.4.1 `ConsoleBattleBootstrapper` - 引导器

```csharp
namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 战斗引导器
    /// 负责初始化所有组件并启动战斗
    /// </summary>
    public sealed class ConsoleBattleBootstrapper : IDisposable
    {
        private readonly IConfigSource _configSource;
        private readonly IConsoleRenderer _renderer;

        private BattleContext _battleContext;
        private ConsoleBattleView _battleView;
        private bool _disposed;

        public ConsoleBattleBootstrapper(IConfigSource configSource, IConsoleRenderer renderer)
        {
            _configSource = configSource ?? throw new ArgumentNullException(nameof(configSource));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        /// <summary>
        /// 初始化战斗
        /// </summary>
        public BattleContext Initialize()
        {
            Log.Info("[BOOT] Initializing Console Battle...");

            // 创建 BattleContext
            _battleContext = BattleContext.Rent();

            // 加载配置
            var configs = LoadConfigs();
            BattleViewFactory.Configs = configs;

            // 初始化实体世界
            InitializeEntityWorld();

            // 创建战斗视图
            _battleView = new ConsoleBattleView(_battleContext, _battleContext.EntityQuery);

            Log.Info("[BOOT] Initialization complete.");

            return _battleContext;
        }

        /// <summary>
        /// 运行战斗循环
        /// </summary>
        public void Run(int frameCount = 1000, int framesPerRender = 10)
        {
            if (_battleContext == null)
            {
                throw new InvalidOperationException("Battle not initialized. Call Initialize() first.");
            }

            Log.Info($"[BOOT] Starting battle loop: {frameCount} frames, render every {framesPerRender} frames");

            for (int frame = 1; frame <= frameCount; frame++)
            {
                // 更新逻辑
                _battleContext.LastFrame = frame;
                _battleContext.LogicTimeSeconds = frame / 30.0; // 假设 30 FPS

                // 刷新脏实体
                RefreshDirtyEntities();

                // 渲染
                if (frame % framesPerRender == 0)
                {
                    Render();
                }
            }

            Log.Info("[BOOT] Battle loop completed.");
            PrintStatistics();
        }

        private void Render()
        {
            if (_battleView == null) return;

            Console.Clear();
            Console.WriteLine(_battleView.Render());
        }

        private void RefreshDirtyEntities()
        {
            if (_battleContext.DirtyEntities == null || _battleContext.DirtyEntities.Count == 0) return;

            foreach (var id in _battleContext.DirtyEntities)
            {
                if (!_battleContext.EntityWorld.IsAlive(id)) continue;

                var entity = _battleContext.EntityWorld.Wrap(id);
                // 视图层会通过快照接收位置更新
            }

            _battleContext.DirtyEntities.Clear();
        }

        private MobaConfigDatabase LoadConfigs()
        {
            return ConsoleConfigLoader.LoadDefault(_configSource);
        }

        private void PrintStatistics()
        {
            if (_battleView == null) return;

            Console.WriteLine();
            Console.WriteLine(_battleView.GetSkillStatisticsReport());
            Console.WriteLine(_battleView.GetEntityStatusReport());
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _battleView?.Dispose();
            BattleContext.Return(_battleContext);
        }
    }
}
```

#### 2.4.2 `Program.cs` - 入口点

```csharp
namespace AbilityKit.Demo.Moba.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            // 设置日志
            Log.SetSink(new ConsoleLogSink());

            // 打印标题
            PrintBanner();

            // 创建配置源
            using var configSource = new ConsoleConfigSource();

            // 创建渲染器
            using var renderer = new BattleMapRenderer();

            // 创建引导器
            using var bootstrapper = new ConsoleBattleBootstrapper(configSource, renderer);

            try
            {
                // 初始化
                var battleContext = bootstrapper.Initialize();

                // 运行战斗
                bootstrapper.Run(frameCount: 300, framesPerRender: 15);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Battle failed");
            }
        }

        static void PrintBanner()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          AbilityKit MOBA Demo - Console View               ║");
            Console.WriteLine("║                                                            ║");
            Console.WriteLine("║  A pure C# implementation of the MOBA battle system         ║");
            Console.WriteLine("║  demonstrating data-driven game architecture               ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
        }
    }
}
```

---

## 3. 基础设施实现

### 3.1 `ConsoleConfigSource` - Console 配置源

```csharp
namespace AbilityKit.Demo.Moba.Console.Infrastructure
{
    /// <summary>
    /// Console 配置源实现
    /// 从文件系统加载配置数据
    /// </summary>
    public sealed class ConsoleConfigSource : IConfigSource
    {
        private readonly string _basePath;

        public ConsoleConfigSource(string basePath = "Configs")
        {
            _basePath = Path.GetFullPath(basePath);
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public bool TryGetText(string path, out string text)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, path);
                if (File.Exists(fullPath))
                {
                    text = File.ReadAllText(fullPath);
                    return true;
                }
            }
            catch { }

            text = null;
            return false;
        }

        public bool TryGetBytes(string path, out byte[] bytes)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, path);
                if (File.Exists(fullPath))
                {
                    bytes = File.ReadAllBytes(fullPath);
                    return true;
                }
            }
            catch { }

            bytes = null;
            return false;
        }
    }
}
```

### 3.2 `ConsoleConfigLoader` - Console 配置加载器

```csharp
namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 配置加载器
    /// 为 Console 环境提供默认配置
    /// </summary>
    public static class ConsoleConfigLoader
    {
        public static MobaConfigDatabase LoadDefault(IConfigSource configSource)
        {
            // 尝试从文件系统加载
            if (configSource != null)
            {
                try
                {
                    var loader = new DefaultMobaConfigLoader(configSource);
                    return loader.Load();
                }
                catch
                {
                    // 加载失败，使用内置配置
                }
            }

            // 返回内置默认配置
            return CreateBuiltinConfig();
        }

        private static MobaConfigDatabase CreateBuiltinConfig()
        {
            var configs = new MobaConfigDatabase();

            // 添加默认英雄配置
            configs.AddCharacter(new CharacterMO
            {
                Code = "hero_warrior",
                Name = "Warrior",
                ModelId = 1,
                BaseHp = 800f,
                BaseMp = 300f,
                BaseAttack = 65f,
                BaseDefense = 40f,
                BaseMoveSpeed = 3f,
                AttackRange = 1.5f,
                Skills = new[] { 101, 102, 103, 104 }
            });

            // 添加默认投射物配置
            configs.AddProjectile(new ProjectileMO
            {
                Code = "proj_arrow",
                Name = "Arrow",
                ModelId = 10,
                Speed = 15f,
                MaxDistance = 20f,
                VfxId = 1001,
                OnHitVfxId = 1002,
                OnSpawnVfxId = 1003,
                OnExpireVfxId = 1004
            });

            // 添加默认 AOE 配置
            configs.AddAoe(new AoeMO
            {
                Code = "aoe_fireball",
                Name = "Fireball",
                ModelId = 20,
                Radius = 5f,
                Duration = 2f,
                VfxId = 2001
            });

            return configs;
        }
    }
}
```

---

## 4. 接口映射表

### 4.1 Unity 视图层 → Console 视图层

| Unity 组件 | Console 组件 | 说明 |
|-----------|-------------|------|
| `BattleViewEventSink` | `ConsoleBattleViewEventSink` | 事件接收 |
| `BattleViewBinder` | `ConsoleEntityDisplayService` | 实体管理 |
| `BattleFloatingTextSystem` | `BattleMapRenderer.MarkDamage()` | 伤害数字 |
| `BattleAreaViewSystem` | `BattleMapRenderer.RenderAreaEffect()` | 范围效果 |
| `BattleVfxManager` | `Log.Info()` / ASCII | VFX 替代 |
| `GameObject` | 文本/ASCII 字符 | 实体表示 |
| `Transform.position` | `MapPosition` | 位置表示 |

### 4.2 核心接口对应

| Unity 接口 | Console 实现 |
|-----------|-------------|
| `IBattleViewEventSink` | `IConsoleBattleViewEventSink` |
| `BattleContext` | `BattleContext` (共享) |
| `IBattleEntityQuery` | `IBattleEntityQuery` (共享) |
| `FrameSnapshotDispatcher` | `FrameSnapshotDispatcher` (共享) |
| `ISkillLifecycleObserver` | `ConsoleSkillLifecyclePrinter` |

---

## 5. 后续优化建议

### 5.1 可选增强功能

| 功能 | 描述 |
|-----|------|
| **彩色输出** | 使用 Console颜色增强可读性 |
| **实时刷新** | 非阻塞的实时渲染 |
| **命令输入** | 支持 Console 命令控制 |
| **日志文件** | 同时输出到文件 |
| **性能统计** | 显示 FPS、实体数量等 |

### 5.2 AI 自检能力

Console 视图层天然适合 AI 自检：

```csharp
// 验证战斗状态一致性
public class BattleValidator
{
    private readonly ConsoleBattleView _view;

    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        // 检查实体 HP 不为负
        foreach (var entity in _view.GetAllEntities())
        {
            if (entity.Hp < 0)
                result.AddError($"Entity #{entity.ActorId} has negative HP: {entity.Hp}");
        }

        // 检查技能完成率
        var stats = _view.GetSkillStatisticsReport();
        var completed = ParseCompleted(stats);
        var started = ParseStarted(stats);
        if (started > 0 && completed / started < 0.8)
            result.AddWarning($"Low skill completion rate: {completed}/{started}");

        return result;
    }
}
```

---

## 6. 总结

### 6.1 设计亮点

1. **零依赖 Unity** - 完全基于纯 C# 实现
2. **职责清晰** - 每个组件职责单一，易于测试
3. **事件驱动** - 与 Unity 视图层保持一致的架构
4. **ASCII 渲染** - 创新的 Console 可视化方案
5. **可扩展** - 易于添加新的渲染模式或输出格式

### 6.2 文件清单

| 文件 | 行数(估) | 优先级 |
|-----|---------|-------|
| `IConsoleBattleViewEventSink.cs` | 30 | P0 |
| `ConsoleBattleViewEventSink.cs` | 200 | P0 |
| `ConsoleEntityDisplayService.cs` | 100 | P0 |
| `BattleMapRenderer.cs` | 300 | P0 |
| `ConsoleSkillLifecyclePrinter.cs` | 150 | P0 |
| `ConsoleBattleView.cs` | 150 | P0 |
| `BattleSnapshotViewAdapter.cs` | 60 | P0 |
| `ConsoleBattleBootstrapper.cs` | 100 | P1 |
| `Program.cs` | 50 | P1 |
| 基础设施文件 | 100 | P1 |

---

*文档版本: 1.0*
*最后更新: 2026-05-13*
