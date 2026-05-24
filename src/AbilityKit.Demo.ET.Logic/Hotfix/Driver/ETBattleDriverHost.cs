using System;
using System.Linq;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Coordinator;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// ET Demo Battle Driver Host
    ///
    /// Responsibilities:
    /// - Encapsulate ETMobaBattleDriver
    /// - Provide frame number and logic time
    /// - Submit inputs to battle logic
    /// - Query entity states for Coordinator
    ///
    /// This bridges AbilityKit.Coordinator's IBattleDriverHost with ET's battle system.
    /// </summary>
    public sealed class ETBattleDriverHost : IBattleDriverHost
    {
        // ============== References ==============

        private readonly ETMobaBattleDriver _driver;

        // ============== Constructor ==============

        public ETBattleDriverHost(ETMobaBattleDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        // ============== IBattleDriverHost Implementation ==============

        public int CurrentFrame => _driver?.CurrentFrame ?? 0;

        public double LogicTimeSeconds => _driver?.LogicTimeSeconds ?? 0;

        public bool IsRunning => _driver?.IsRunning ?? false;

        /// <summary>
        /// Submit inputs to the battle logic
        /// Converts PlayerInput to ET's internal command format
        /// </summary>
        public void SubmitInputs(PlayerInput[] inputs)
        {
            if (_driver == null || inputs == null || inputs.Length == 0)
                return;

            foreach (var input in inputs)
            {
                switch (input.OpCode)
                {
                    case InputOpCodes.Move:
                        HandleMoveInput(input);
                        break;

                    case InputOpCodes.Skill:
                        HandleSkillInput(input);
                        break;

                    case InputOpCodes.Stop:
                        HandleStopInput(input);
                        break;
                }
            }
        }

        /// <summary>
        /// Get all entity states for rendering
        /// Maps from ET's ActorStateSnapshotData to Coordinator's EntityState
        /// </summary>
        public EntityState[] GetAllEntityStates()
        {
            // Get actor states from ET's sync adapter
            var adapter = _driver?.SyncAdapter as IETBattleSyncAdapter;
            if (adapter == null)
            {
                return Array.Empty<EntityState>();
            }

            var actorStates = adapter.GetAllActorStates();
            if (actorStates == null || actorStates.Length == 0)
            {
                return Array.Empty<EntityState>();
            }

            return actorStates
                .Select(s => new EntityState
                {
                    EntityId = s.ActorId,
                    X = s.X,
                    Y = s.Y,
                    Z = s.Z,
                    Rotation = s.Rotation,
                    Hp = s.Hp,
                    HpMax = s.HpMax,
                    TeamId = s.TeamId,
                    IsDead = s.Hp <= 0,
                })
                .ToArray();
        }

        // ============== Input Handlers ==============

        private void HandleMoveInput(PlayerInput input)
        {
            if (!input.TryGetMoveTarget(out float x, out float z))
                return;

            // Submit through ETMobaBattleDriverSystem
            ETMobaBattleDriverSystem.SubmitMoveInput(_driver, input.PlayerId, x, z);
        }

        private void HandleSkillInput(PlayerInput input)
        {
            if (!input.TryGetSkillTarget(out int slot, out float x, out float z))
                return;

            // Submit through ETMobaBattleDriverSystem
            ETMobaBattleDriverSystem.SubmitSkillInput(_driver, input.PlayerId, slot, x, z);
        }

        private void HandleStopInput(PlayerInput input)
        {
            // Submit through ETMobaBattleDriverSystem
            ETMobaBattleDriverSystem.SubmitStopInput(_driver, input.PlayerId);
        }
    }
}
