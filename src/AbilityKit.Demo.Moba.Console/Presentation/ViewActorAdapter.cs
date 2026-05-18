using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Presentation
{
    /// <summary>
    /// Presentation 层角色视图适配器
    ///
    /// 职责：
    /// - 从 Simulation 层的 ConsoleActorRepository 读取数据
    /// - 提供只读的视图数据给表现层使用
    /// - 隔离 Simulation 层和 Presentation 层
    ///
    /// 数据流向：
    /// ConsoleActorRepository (Simulation) → ViewActorAdapter (Presentation) → ConsoleBattleView (Rendering)
    /// </summary>
    public sealed class ViewActorAdapter
    {
        private readonly Simulation.ConsoleActorRepository _source;

        public ViewActorAdapter(Simulation.ConsoleActorRepository source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// 获取角色信息（只读）
        /// </summary>
        public ActorViewData? GetActor(int actorId)
        {
            var state = _source.GetActor(actorId);
            if (state == null) return null;

            return new ActorViewData(
                state.ActorId,
                state.Name,
                state.X,
                state.Y,
                state.Z,
                state.Hp,
                state.HpMax,
                state.TeamId);
        }

        /// <summary>
        /// 获取所有角色（只读）
        /// </summary>
        public IEnumerable<ActorViewData> GetAllActors()
        {
            foreach (var state in _source.GetAllActors())
            {
                yield return new ActorViewData(
                    state.ActorId,
                    state.Name,
                    state.X,
                    state.Y,
                    state.Z,
                    state.Hp,
                    state.HpMax,
                    state.TeamId);
            }
        }

        /// <summary>
        /// 角色数量
        /// </summary>
        public int ActorCount => _source.ActorCount;
    }

    /// <summary>
    /// Presentation 层角色视图数据（只读）
    /// </summary>
    public readonly struct ActorViewData
    {
        public int ActorId { get; }
        public string Name { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float Hp { get; }
        public float HpMax { get; }
        public int TeamId { get; }

        public ActorViewData(int actorId, string name, float x, float y, float z, float hp, float hpMax, int teamId)
        {
            ActorId = actorId;
            Name = name;
            X = x;
            Y = y;
            Z = z;
            Hp = hp;
            HpMax = hpMax;
            TeamId = teamId;
        }
    }
}
