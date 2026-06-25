#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
using Svelto.DataStructures;
using Svelto.ECS;
using UnityHFSM;
using UnityHFSM.Extension;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal interface IShooterBotAiRuntime
    {
        int BotAiCount { get; }

        bool MountBotAi(int playerId, ShooterBotAiConfig config);

        bool UnmountBotAi(int playerId);

        void ClearBotAi();

        void Tick(float deltaTime);
    }

    internal sealed class ShooterBotAiRuntime : IShooterBotAiRuntime
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly ShooterSpatialTargetIndex _targetIndex = new();
        private readonly Dictionary<int, ShooterBotAiController> _controllers = new Dictionary<int, ShooterBotAiController>();

        public ShooterBotAiRuntime(ShooterBattleState state, IShooterEntityManager entities)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public int BotAiCount => _controllers.Count;

        public bool MountBotAi(int playerId, ShooterBotAiConfig config)
        {
            if (playerId <= 0 || !_entities.HasPlayer(playerId))
            {
                return false;
            }

            _controllers[playerId] = ShooterBotAiController.Create(playerId, _state, _entities, _targetIndex, config ?? ShooterBotAiProfileCatalog.SimpleBattle);
            return true;
        }

        public bool UnmountBotAi(int playerId)
        {
            return _controllers.Remove(playerId);
        }

        public void ClearBotAi()
        {
            _controllers.Clear();
        }

        public void Tick(float deltaTime)
        {
            if (_controllers.Count == 0)
            {
                return;
            }

            _targetIndex.Rebuild(_entities.SveltoContext, _state.CurrentFrame);

            foreach (var kv in _controllers)
            {
                var controller = kv.Value;
                if (!_targetIndex.TryGetLivePlayer(controller.PlayerId, out _))
                {
                    _state.InputBuffer.RemoveLatestCommand(controller.PlayerId);
                    continue;
                }

                controller.Tick(deltaTime);
                var command = controller.Command;
                _state.InputBuffer.SubmitCommand(_state.CurrentFrame, in command);
            }
        }
    }
}
