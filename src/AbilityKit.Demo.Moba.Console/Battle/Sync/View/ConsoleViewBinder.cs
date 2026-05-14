using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Sync;

namespace AbilityKit.Demo.Moba.Console.Battle.Sync.View;

/// <summary>
/// Console 版本的 View Binder
/// 绑定战斗实体到 Console 视图，执行插值渲染
/// 从 moba.view.runtime 的 BattleViewBinder 简化而来
/// </summary>
public sealed class ConsoleViewBinder : IDisposable
{
    private readonly Dictionary<int, EntityInterpolationState> _entityStates = new();
    private readonly Dictionary<int, EntityDisplayInfo> _entityDisplay = new();

    private double _renderTime;
    private double _backTimeSeconds = 1.0 / 30.0; // 默认一帧的回溯时间
    private int _tickRate = 30;
    private bool _interpolationEnabled = true;

    /// <summary>
    /// 渲染时间（秒）- 比逻辑时间滞后
    /// </summary>
    public double RenderTime
    {
        get => _renderTime;
        set => _renderTime = value;
    }

    /// <summary>
    /// 是否启用插值
    /// </summary>
    public bool InterpolationEnabled
    {
        get => _interpolationEnabled;
        set => _interpolationEnabled = value;
    }

    /// <summary>
    /// 回溯时间（秒）- 渲染时间比逻辑时间滞后的量
    /// </summary>
    public float BackTimeSeconds
    {
        get => (float)_backTimeSeconds;
        set => _backTimeSeconds = value;
    }

    /// <summary>
    /// Tick 率（帧/秒）
    /// </summary>
    public int TickRate
    {
        get => _tickRate;
        set => _tickRate = value;
    }

    /// <summary>
    /// 注册或更新实体状态
    /// </summary>
    public void SyncActor(int actorId, ActorStateSnapshot snapshot, double logicTime)
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

        // 采样到缓冲
        state.PositionBuffer.Add(logicTime, snapshot.X, snapshot.Y, snapshot.Z);
    }

    /// <summary>
    /// 每帧更新渲染位置
    /// </summary>
    /// <param name="deltaTime">帧间隔（秒）</param>
    /// <param name="logicTime">当前逻辑时间（秒）</param>
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

    /// <summary>
    /// 获取实体的渲染位置
    /// </summary>
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

    /// <summary>
    /// 获取实体是否已死亡
    /// </summary>
    public bool IsActorDead(int actorId)
    {
        return _entityStates.TryGetValue(actorId, out var state) && state.IsDead;
    }

    /// <summary>
    /// 获取所有实体的渲染位置
    /// </summary>
    public IEnumerable<(int ActorId, float X, float Y, float Z, bool IsDead)> GetAllRenderPositions()
    {
        foreach (var kvp in _entityStates)
        {
            var state = kvp.Value;
            yield return (state.ActorId, state.RenderX, state.RenderY, state.RenderZ, state.IsDead);
        }
    }

    /// <summary>
    /// 移除实体
    /// </summary>
    public void RemoveActor(int actorId)
    {
        _entityStates.Remove(actorId);
        _entityDisplay.Remove(actorId);
    }

    /// <summary>
    /// 清除所有实体
    /// </summary>
    public void Clear()
    {
        _entityStates.Clear();
        _entityDisplay.Clear();
    }

    /// <summary>
    /// 获取实体数量
    /// </summary>
    public int Count => _entityStates.Count;

    public void Dispose()
    {
        Clear();
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
