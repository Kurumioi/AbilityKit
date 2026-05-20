using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// 弹道信息
    /// </summary>
    public sealed class ProjectileInfo
    {
        public int ProjectileId;
        public int TemplateId;
        public float X;
        public float Y;
        public float Z;
        public float StartX;
        public float StartY;
        public float StartZ;
        public float TargetX;
        public float TargetY;
        public float TargetZ;
        public float Speed;
        public float CreatedTime;
        public int OwnerActorId;
        public int TargetActorId;
        public ProjectileState State;
        public List<(float X, float Y, float Z)> Trajectory = new();
    }

    /// <summary>
    /// 投射物状态
    /// </summary>
    public enum ProjectileState
    {
        Flying,
        Hit,
        Expired,
        Destroyed
    }

    /// <summary>
    /// 投射物事件类型
    /// </summary>
    public enum ProjectileEventType
    {
        Spawn,
        Hit,
        Expire,
        Destroy
    }

    /// <summary>
    /// 投射物事件回调
    /// </summary>
    public delegate void ProjectileEventHandler(int projectileId, ProjectileEventType eventType, ProjectileInfo info);

    /// <summary>
    /// Console 弹道显示服务 (增强版)
    /// 对标 Unity BattleProjectileViewService
    /// </summary>
    public sealed class ConsoleProjectileDisplayService
    {
        private readonly Dictionary<int, ProjectileInfo> _projectiles = new();
        private readonly List<int> _toRemove = new();
        private double _logicTimeSeconds;
        private ProjectileEventHandler _onEvent;

        /// <summary>
        /// 设置逻辑时间
        /// </summary>
        public void SetLogicTime(double seconds)
        {
            _logicTimeSeconds = seconds;
        }

        /// <summary>
        /// 设置事件回调
        /// </summary>
        public void SetEventHandler(ProjectileEventHandler handler)
        {
            _onEvent = handler;
        }

        /// <summary>
        /// 生成投射物
        /// </summary>
        public void Spawn(int projectileId, int templateId, float x, float y, float z,
            int ownerActorId = 0, int targetActorId = 0)
        {
            if (_projectiles.ContainsKey(projectileId))
            {
                _projectiles.Remove(projectileId);
            }

            var info = new ProjectileInfo
            {
                ProjectileId = projectileId,
                TemplateId = templateId,
                X = x,
                Y = y,
                Z = z,
                StartX = x,
                StartY = y,
                StartZ = z,
                TargetX = x,
                TargetY = y,
                TargetZ = z,
                Speed = 10f,
                CreatedTime = (float)_logicTimeSeconds,
                OwnerActorId = ownerActorId,
                TargetActorId = targetActorId,
                State = ProjectileState.Flying
            };

            _projectiles[projectileId] = info;

            _onEvent?.Invoke(projectileId, ProjectileEventType.Spawn, info);

            Platform.Log.Projectile($"[Projectile] Spawn #{projectileId} (Template:{templateId}) at ({x:F1}, {y:F1}, {z:F1})");
        }

        /// <summary>
        /// 设置投射物目标
        /// </summary>
        public void SetTarget(int projectileId, float targetX, float targetY, float targetZ, float speed)
        {
            if (_projectiles.TryGetValue(projectileId, out var info))
            {
                info.TargetX = targetX;
                info.TargetY = targetY;
                info.TargetZ = targetZ;
                info.Speed = speed;
            }
        }

        /// <summary>
        /// 更新投射物位置
        /// </summary>
        public void UpdatePosition(int projectileId, float x, float y, float z)
        {
            if (_projectiles.TryGetValue(projectileId, out var info))
            {
                // 记录轨迹
                info.Trajectory.Add((info.X, info.Y, info.Z));
                if (info.Trajectory.Count > 10)
                {
                    info.Trajectory.RemoveAt(0);
                }

                info.X = x;
                info.Y = y;
                info.Z = z;
            }
        }

        /// <summary>
        /// 投射物命中
        /// </summary>
        public void Hit(int projectileId)
        {
            if (_projectiles.TryGetValue(projectileId, out var info))
            {
                info.State = ProjectileState.Hit;
                _onEvent?.Invoke(projectileId, ProjectileEventType.Hit, info);
                Platform.Log.Projectile($"[Projectile] Hit #{projectileId} at ({info.X:F1}, {info.Y:F1}, {info.Z:F1})");
            }
        }

        /// <summary>
        /// 投射物过期
        /// </summary>
        public void Expire(int projectileId)
        {
            if (_projectiles.TryGetValue(projectileId, out var info))
            {
                info.State = ProjectileState.Expired;
                _onEvent?.Invoke(projectileId, ProjectileEventType.Expire, info);
                Platform.Log.Projectile($"[Projectile] Expire #{projectileId}");
            }
        }

        /// <summary>
        /// 销毁投射物
        /// </summary>
        public void Destroy(int projectileId)
        {
            if (_projectiles.TryGetValue(projectileId, out var info))
            {
                info.State = ProjectileState.Destroyed;
                _onEvent?.Invoke(projectileId, ProjectileEventType.Destroy, info);
                _toRemove.Add(projectileId);
                Platform.Log.Projectile($"[Projectile] Destroy #{projectileId}");
            }
        }

        /// <summary>
        /// 移除投射物
        /// </summary>
        public void Remove(int projectileId)
        {
            _projectiles.Remove(projectileId);
        }

        /// <summary>
        /// 更新投射物 (Tick)
        /// </summary>
        public void Tick(float deltaTime)
        {
            _toRemove.Clear();

            foreach (var kvp in _projectiles)
            {
                var info = kvp.Value;

                if (info.State == ProjectileState.Flying)
                {
                    // 简单的直线运动
                    var dx = info.TargetX - info.X;
                    var dy = info.TargetY - info.Y;
                    var dz = info.TargetZ - info.Z;
                    var distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    if (distance < 0.5f)
                    {
                        // 到达目标
                        info.State = ProjectileState.Hit;
                        _onEvent?.Invoke(kvp.Key, ProjectileEventType.Hit, info);
                    }
                    else
                    {
                        // 移动
                        var moveDistance = info.Speed * deltaTime;
                        if (moveDistance > distance)
                        {
                            moveDistance = distance;
                        }

                        var factor = moveDistance / distance;
                        info.X += dx * factor;
                        info.Y += dy * factor;
                        info.Z += dz * factor;

                        // 记录轨迹
                        info.Trajectory.Add((info.X, info.Y, info.Z));
                        if (info.Trajectory.Count > 10)
                        {
                            info.Trajectory.RemoveAt(0);
                        }
                    }
                }

                // 检查过期时间
                var age = (float)_logicTimeSeconds - info.CreatedTime;
                if (age > 10f && info.State == ProjectileState.Flying)
                {
                    info.State = ProjectileState.Expired;
                    _onEvent?.Invoke(kvp.Key, ProjectileEventType.Expire, info);
                    _toRemove.Add(kvp.Key);
                }
            }

            // 移除过期的投射物
            foreach (var id in _toRemove)
            {
                _projectiles.Remove(id);
            }
        }

        /// <summary>
        /// 获取投射物信息
        /// </summary>
        public bool TryGetInfo(int projectileId, out ProjectileInfo info)
            => _projectiles.TryGetValue(projectileId, out info);

        /// <summary>
        /// 获取所有投射物
        /// </summary>
        public IEnumerable<ProjectileInfo> GetAll()
            => _projectiles.Values;

        /// <summary>
        /// 获取飞行中的投射物
        /// </summary>
        public IEnumerable<ProjectileInfo> GetFlying()
        {
            foreach (var kvp in _projectiles)
            {
                if (kvp.Value.State == ProjectileState.Flying)
                {
                    yield return kvp.Value;
                }
            }
        }

        /// <summary>
        /// 获取投射物数量
        /// </summary>
        public int Count => _projectiles.Count;

        /// <summary>
        /// 获取飞行中的投射物数量
        /// </summary>
        public int FlyingCount
        {
            get
            {
                var count = 0;
                foreach (var kvp in _projectiles)
                {
                    if (kvp.Value.State == ProjectileState.Flying)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// 清除所有投射物
        /// </summary>
        public void Clear()
        {
            var count = _projectiles.Count;
            _projectiles.Clear();
            Platform.Log.Projectile($"[Projectile] Cleared {count} projectiles");
        }
    }
}
