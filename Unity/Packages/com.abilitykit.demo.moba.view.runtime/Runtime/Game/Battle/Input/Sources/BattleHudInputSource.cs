namespace AbilityKit.Game.Flow
{
    internal readonly struct BattleSkillAimSubmitInput
    {
        public readonly int Slot;
        public readonly float Dx;
        public readonly float Dz;

        public BattleSkillAimSubmitInput(int slot, float dx, float dz)
        {
            Slot = slot;
            Dx = dx;
            Dz = dz;
        }
    }

    internal static class BattleHudInputSource
    {
        public static bool TryReadMove(BattleContext ctx, out float dx, out float dz)
        {
            if (ctx != null) return ctx.TryReadHudMove(out dx, out dz);

            dx = 0f;
            dz = 0f;
            return false;
        }

        public static bool TryConsumeSkillClick(BattleContext ctx, out int slot)
        {
            if (ctx != null) return ctx.TryConsumeHudSkillClick(out slot);

            slot = 0;
            return false;
        }

        public static bool TryConsumeSkillAimSubmit(BattleContext ctx, out BattleSkillAimSubmitInput input)
        {
            if (ctx != null && ctx.TryConsumeHudSkillAimSubmit(out var slot, out var dx, out var dz))
            {
                input = new BattleSkillAimSubmitInput(slot, dx, dz);
                return true;
            }

            input = default;
            return false;
        }
    }
}
