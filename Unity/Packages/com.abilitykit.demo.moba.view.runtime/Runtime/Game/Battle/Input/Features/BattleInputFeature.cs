namespace AbilityKit.Game.Flow
{
    public sealed class BattleInputFeature : IGamePhaseFeature
    {
        private readonly BattleMoveInputState _moveInputState;
        private BattleContext _ctx;
        private float _inputDiagCooldown;

        public BattleInputFeature()
        {
            _moveInputState = new BattleMoveInputState();
        }

        public void OnAttach(in GamePhaseContext ctx)
        {
            ctx.Root.TryGetRef(out _ctx);
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            _ctx = null;
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
            if (_ctx == null || _ctx.Session == null) return;
            if (_ctx.Plan.EnableInputReplay) return;

            var plan = _ctx.Plan;
            var playerId = BattleInputSessionIdentity.ResolvePlayerId(in plan);
            var worldId = BattleInputSessionIdentity.ResolveWorldId(in plan);
            var nextFrame = _ctx.LastFrame + 1;

            _ctx.LocalInputQueue ??= new BattleLocalInputQueue();
            var submitter = new BattleInputSubmitter(_ctx, playerId, worldId);

            if (!BattleHudInputSource.TryReadMove(_ctx, out var dx, out var dz))
            {
                BattleKeyboardInputSource.ReadMove(out dx, out dz);
            }

            if (_moveInputState.TryGetMoveToSubmit(dx, dz, out var submitDx, out var submitDz))
            {
                var moveCmd = BattleInputCommandFactory.CreateMove(nextFrame, playerId, submitDx, submitDz);
                submitter.Submit(in moveCmd);
            }
            else
            {
                TickInputDiagnostics(deltaTime);
            }

            if (BattleKeyboardInputSource.TryReadSkillSlotDown(out var keyboardSlot))
            {
                var skillCmd = BattleInputCommandFactory.CreateSkillSlot(nextFrame, playerId, keyboardSlot);
                submitter.Submit(in skillCmd);
            }

            if (BattleHudInputSource.TryConsumeSkillClick(_ctx, out var hudSlot))
            {
                var skillCmd = BattleInputCommandFactory.CreateSkillSlot(nextFrame, playerId, hudSlot);
                submitter.Submit(in skillCmd);
            }

            if (BattleHudInputSource.TryConsumeSkillAimSubmit(_ctx, out var aimInput))
            {
                var aimCmd = BattleInputCommandFactory.CreateSkillAimRelease(
                    nextFrame,
                    playerId,
                    aimInput.Slot,
                    aimInput.Dx,
                    aimInput.Dz);
                submitter.Submit(in aimCmd);
            }

            _ctx.LocalInputQueue.Flush();
        }

        private void TickInputDiagnostics(float deltaTime)
        {
            _inputDiagCooldown -= deltaTime;
            if (_inputDiagCooldown <= 0f)
            {
                _inputDiagCooldown = 1f;
            }
        }
    }
}
