using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using ActorSnapshot = AbilityKit.Demo.Moba.Console.Battle.Sync.ActorStateSnapshot;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 版本的 View Binder
    /// 绑定战斗实体到 Console 视图，执行插值渲染
    /// 对标 moba.view 的 BattleViewBinder
    /// </summary>
    public partial class ConsoleViewBinder : IConsoleViewBinder
    {
        private readonly Dictionary<int, EntityInterpolationState> _entityStates = new();
        private readonly Dictionary<int, EntityDisplayInfo> _entityDisplay = new();

        private double _renderTime;
        private double _backTimeSeconds = 1.0 / 30.0; // 默认一帧的回溯时间
        private int _tickRate = 30;
        private bool _interpolationEnabled = true;

        /// <inheritdoc />
        public double RenderTime
        {
            get => _renderTime;
            set => _renderTime = value;
        }

        /// <inheritdoc />
        public bool InterpolationEnabled
        {
            get => _interpolationEnabled;
            set => _interpolationEnabled = value;
        }

        /// <inheritdoc />
        public float BackTimeSeconds
        {
            get => (float)_backTimeSeconds;
            set => _backTimeSeconds = value;
        }

        /// <inheritdoc />
        public int TickRate
        {
            get => _tickRate;
            set => _tickRate = value;
        }

        /// <summary>
        /// 获取渲染时间
        /// </summary>
        public double GetRenderTime() => _renderTime;

        /// <summary>
        /// 设置渲染时间
        /// </summary>
        public void SetRenderTime(double time) => _renderTime = time;

        /// <summary>
        /// 注册或更新实体状态
        /// </summary>
        public void SyncActor(int actorId, ActorSnapshot snapshot, double logicTime)
        {
            if (!_entityStates.TryGetValue(actorId, out var state))
            {
                state = new EntityInterpolationState { ActorId = actorId };
                _entityStates[actorId] = state;
            }

            // 更新当前位置
            state.CurrentX = snapshot.X;
            state.CurrentY = snapshot.Y;
            state.CurrentZ = snapshot.Z;
            state.CurrentRotation = snapshot.Rotation;
            state.IsDead = snapshot.Hp <= 0;
            state.Hp = snapshot.Hp;
            state.MaxHp = snapshot.HpMax;

            // 采样到缓冲
            state.PositionBuffer.Add(logicTime, snapshot.X, snapshot.Y, snapshot.Z);
        }

        /// <inheritdoc />
        public void TickRender(float deltaTime, double logicTime)
        {
            // 计算渲染时间 = 逻辑时间 - 回溯时间
            var targetRenderTime = logicTime - _backTimeSeconds;
            if (targetRenderTime < 0) targetRenderTime = 0;

            // 平滑过渡到目标渲染时间
            if (_renderTime > targetRenderTime)
            {
                _renderTime = targetRenderTime;
            }
            else
            {
                _renderTime += deltaTime;
            }

            if (!_interpolationEnabled)
            {
                // 不启用插值时，渲染位置 = 当前位置
                foreach (var kvp in _entityStates)
                {
                    var state = kvp.Value;
                    state.RenderX = state.CurrentX;
                    state.RenderY = state.CurrentY;
                    state.RenderZ = state.CurrentZ;
                    state.RenderRotation = state.CurrentRotation;
                }
                return;
            }

            // 为每个实体计算插值后的渲染位置
            foreach (var kvp in _entityStates)
            {
                var state = kvp.Value;

                if (state.PositionBuffer.TryEvaluate(_renderTime, out var x, out var y, out var z))
                {
                    state.RenderX = x;
                    state.RenderY = y;
                    state.RenderZ = z;
                }
                else
                {
                    // 采样失败，使用当前位置
                    state.RenderX = state.CurrentX;
                    state.RenderY = state.CurrentY;
                    state.RenderZ = state.CurrentZ;
                }
            }
        }

        /// <inheritdoc />
        public bool TryGetRenderPosition(int actorId, out float x, out float y, out float z)
        {
            if (_entityStates.TryGetValue(actorId, out var state))
            {
                x = state.RenderX;
                y = state.RenderY;
                z = state.RenderZ;
                return true;
            }

            x = y = z = 0;
            return false;
        }

        /// <inheritdoc />
        public bool IsActorDead(int actorId)
        {
            return _entityStates.TryGetValue(actorId, out var state) && state.IsDead;
        }

        /// <inheritdoc />
        public IEnumerable<(int ActorId, float X, float Y, float Z, bool IsDead)> GetAllRenderPositions()
        {
            foreach (var kvp in _entityStates)
            {
                var state = kvp.Value;
                yield return (state.ActorId, state.RenderX, state.RenderY, state.RenderZ, state.IsDead);
            }
        }

        /// <inheritdoc />
        public void RemoveActor(int actorId)
        {
            _entityStates.Remove(actorId);
            _entityDisplay.Remove(actorId);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _entityStates.Clear();
            _entityDisplay.Clear();
        }

        /// <inheritdoc />
        public int Count => _entityStates.Count;
    }

    /// <summary>
    /// 实体插值状态
    /// </summary>
    public sealed class EntityInterpolationState
    {
        public int ActorId;
        public float CurrentX;
        public float CurrentY;
        public float CurrentZ;
        public float CurrentRotation;
        public float RenderX;
        public float RenderY;
        public float RenderZ;
        public float RenderRotation;
        public float Hp;
        public float MaxHp;
        public bool IsDead;

        public PositionSampleBuffer PositionBuffer = new();
    }

    /// <summary>
    /// 位置采样缓冲
    /// 对标 moba.view 的 SampleBuffer
    /// </summary>
    public sealed class PositionSampleBuffer
    {
        private const int Capacity = 4;
        private Sample _s0;
        private Sample _s1;
        private Sample _s2;
        private Sample _s3;
        private int _count;

        private const double TimeEpsilon = 1e-6;

        public void Clear()
        {
            _s0 = default;
            _s1 = default;
            _s2 = default;
            _s3 = default;
            _count = 0;
        }

        private Sample Get(int index)
        {
            switch (index)
            {
                case 0: return _s0;
                case 1: return _s1;
                case 2: return _s2;
                case 3: return _s3;
                default: return default;
            }
        }

        private void Set(int index, in Sample s)
        {
            switch (index)
            {
                case 0: _s0 = s; break;
                case 1: _s1 = s; break;
                case 2: _s2 = s; break;
                case 3: _s3 = s; break;
            }
        }

        public void Add(double time, float x, float y, float z)
        {
            var s = new Sample { Time = time, X = x, Y = y, Z = z };

            for (var i = 0; i < _count; i++)
            {
                var existing = Get(i);
                if (Math.Abs(existing.Time - time) <= TimeEpsilon)
                {
                    Set(i, in s);
                    return;
                }
            }

            if (_count == 0)
            {
                Set(0, in s);
                _count = 1;
                return;
            }

            var insertAt = _count;
            for (var i = 0; i < _count; i++)
            {
                if (time < Get(i).Time)
                {
                    insertAt = i;
                    break;
                }
            }

            if (_count < Capacity)
            {
                for (var i = _count; i > insertAt; i--)
                {
                    var prev = Get(i - 1);
                    Set(i, in prev);
                }
                Set(insertAt, in s);
                _count++;
                return;
            }

            if (insertAt <= 0)
            {
                return;
            }

            for (var i = 0; i < Capacity - 1; i++)
            {
                var next = Get(i + 1);
                Set(i, in next);
            }
            Set(Capacity - 1, in s);
        }

        public bool TryEvaluate(double time, out float x, out float y, out float z)
        {
            x = y = z = 0;

            if (_count <= 0)
            {
                return false;
            }

            if (_count == 1)
            {
                var s = Get(0);
                x = s.X;
                y = s.Y;
                z = s.Z;
                return true;
            }

            var first = Get(0);
            if (time <= first.Time)
            {
                x = first.X;
                y = first.Y;
                z = first.Z;
                return true;
            }

            var last = Get(_count - 1);
            if (time >= last.Time)
            {
                x = last.X;
                y = last.Y;
                z = last.Z;
                return true;
            }

            for (var i = 0; i < _count - 1; i++)
            {
                var a = Get(i);
                var b = Get(i + 1);
                if (time < a.Time) continue;
                if (time > b.Time) continue;

                var dt = b.Time - a.Time;
                if (dt <= 0d)
                {
                    x = b.X;
                    y = b.Y;
                    z = b.Z;
                    return true;
                }

                var t = (float)((time - a.Time) / dt);
                x = LerpUnclamped(a.X, b.X, t);
                y = LerpUnclamped(a.Y, b.Y, t);
                z = LerpUnclamped(a.Z, b.Z, t);
                return true;
            }

            var ls = Get(_count - 1);
            x = ls.X;
            y = ls.Y;
            z = ls.Z;
            return true;
        }

        private static float LerpUnclamped(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private struct Sample
        {
            public double Time;
            public float X;
            public float Y;
            public float Z;
        }
    }

    /// <summary>
    /// 实体显示信息
    /// </summary>
    public sealed class EntityDisplayInfo
    {
        public int ActorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public float Hp { get; set; }
        public float HpMax { get; set; }
        public int TeamId { get; set; }
    }

    /// <summary>
    /// Disposable extension for ConsoleViewBinder
    /// </summary>
    public sealed partial class ConsoleViewBinder
    {
        public void Dispose()
        {
            Clear();
        }
    }
}
