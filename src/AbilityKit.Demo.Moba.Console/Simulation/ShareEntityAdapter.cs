using System;
using System.Collections.Generic;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Share;

namespace AbilityKit.Demo.Moba.Console.Simulation
{
    /// <summary>
    /// Share 层 IShareEntityLookup 接口的 Console 实现
    /// 适配 ConsoleActorRepository 到 Share 层接口
    /// </summary>
    public sealed class ShareEntityLookupAdapter : IShareEntityLookup
    {
        private readonly ConsoleActorRepository _repository;
        private readonly Dictionary<int, ActorState> _netIdToActor = new();

        public ShareEntityLookupAdapter(ConsoleActorRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public int Count => _netIdToActor.Count;

        public void Bind(BattleNetId netId, IShareEntity entity)
        {
            if (entity == null) return;

            var actor = _repository.GetActor(netId.Value);
            if (actor != null)
            {
                _netIdToActor[netId.Value] = actor;
            }
        }

        public bool TryResolve(IShareEntityWorld world, BattleNetId netId, out IShareEntity entity)
        {
            entity = default;

            if (!_netIdToActor.TryGetValue(netId.Value, out var actor))
            {
                actor = _repository.GetActor(netId.Value);
                if (actor == null) return false;
                _netIdToActor[netId.Value] = actor;
            }

            // 返回一个简单的包装实体
            entity = new ShareEntityAdapter(actor);
            return true;
        }

        public bool TryResolveId(BattleNetId netId, out ShareEntityId entityId)
        {
            if (_netIdToActor.ContainsKey(netId.Value) || _repository.GetActor(netId.Value) != null)
            {
                entityId = new ShareEntityId(netId.Value);
                return true;
            }
            entityId = ShareEntityId.Invalid;
            return false;
        }

        public bool Unbind(BattleNetId netId)
        {
            return _netIdToActor.Remove(netId.Value);
        }

        public void Clear()
        {
            _netIdToActor.Clear();
        }

        public ShareEntityId GetAllNetIds()
        {
            return ShareEntityId.Invalid;
        }
    }

    /// <summary>
    /// Share 层 IShareEntity 接口的 Console 简单实现
    /// 包装 ActorState 为 Share 层实体
    /// </summary>
    public sealed class ShareEntityAdapter : IShareEntity
    {
        private readonly ActorState _actor;

        public ShareEntityAdapter(ActorState actor)
        {
            _actor = actor;
        }

        public ShareEntityId Id => new ShareEntityId(_actor.ActorId);
        public string Name => _actor.Name;
        public IShareEntityWorld World => null;
        public bool IsValid => _actor != null && _actor.Hp > 0;
        public bool IsAlive => _actor != null && _actor.Hp > 0;
        public BattleNetId NetId => new BattleNetId(_actor.ActorId);

        public bool TryGetRef<T>(out T component) where T : struct
        {
            component = default;
            return false;
        }

        public void SetRef<T>(in T component) where T : struct
        {
        }

        public bool Remove<T>() where T : struct
        {
            return false;
        }

        public void Destroy()
        {
        }
    }

    /// <summary>
    /// Share 层 ICharacterComponent 接口的 Console 实现
    /// </summary>
    public struct CharacterComponentAdapter : ICharacterComponent
    {
        private readonly ActorState _actor;

        public CharacterComponentAdapter(ActorState actor)
        {
            _actor = actor;
        }

        public int TeamId => _actor?.TeamId ?? 0;
        public float Hp => _actor?.Hp ?? 0;
        public float HpMax => _actor?.HpMax ?? 0;
        public int ModelId => _actor?.CharacterId ?? 0;
        public int Level => _actor?.SkillLevel ?? 1;
        public bool IsDead => _actor == null || _actor.Hp <= 0;
        public bool IsInvincible => false;
    }

    /// <summary>
    /// Share 层 IShareTransformComponent 接口的 Console 实现
    /// </summary>
    public struct TransformComponentAdapter : IShareTransformComponent
    {
        private readonly ActorState _actor;

        public TransformComponentAdapter(ActorState actor)
        {
            _actor = actor;
        }

        public Vec3 Position
        {
            get => new Vec3(_actor?.X ?? 0, _actor?.Y ?? 0, _actor?.Z ?? 0);
            set
            {
                if (_actor != null)
                {
                    _actor.X = value.X;
                    _actor.Y = value.Y;
                    _actor.Z = value.Z;
                }
            }
        }

        public Vec3 Forward
        {
            get => Vec3.Forward;
            set { }
        }

        public Vec3 Up
        {
            get => Vec3.Up;
            set { }
        }

        public float RotationY
        {
            get => 0f;
            set { }
        }

        public float Scale
        {
            get => 1f;
            set { }
        }

        public void SetPosition(float x, float y, float z)
        {
            if (_actor != null)
            {
                _actor.X = x;
                _actor.Y = y;
                _actor.Z = z;
            }
        }

        public void SetRotation(float rotationY)
        {
        }

        public void LookAt(Vec3 target)
        {
        }

        public void Translate(Vec3 delta)
        {
            if (_actor != null)
            {
                _actor.X += delta.X;
                _actor.Y += delta.Y;
                _actor.Z += delta.Z;
            }
        }

        public float DistanceTo(Vec3 position)
        {
            var dx = (_actor?.X ?? 0) - position.X;
            var dy = (_actor?.Y ?? 0) - position.Y;
            var dz = (_actor?.Z ?? 0) - position.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public float SqrDistanceTo(Vec3 position)
        {
            var dx = (_actor?.X ?? 0) - position.X;
            var dy = (_actor?.Y ?? 0) - position.Y;
            var dz = (_actor?.Z ?? 0) - position.Z;
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
