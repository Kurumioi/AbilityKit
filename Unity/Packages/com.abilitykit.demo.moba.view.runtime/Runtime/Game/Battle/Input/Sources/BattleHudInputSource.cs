namespace AbilityKit.Game.Flow
{
    internal readonly struct BattleSkillAimSubmitInput
    {
        public readonly int Slot;
        public readonly float AimPosX;
        public readonly float AimPosY;
        public readonly float AimPosZ;
        public readonly float AimDirX;
        public readonly float AimDirY;
        public readonly float AimDirZ;

        public BattleSkillAimSubmitInput(
            int slot,
            float aimPosX,
            float aimPosY,
            float aimPosZ,
            float aimDirX,
            float aimDirY,
            float aimDirZ)
        {
            Slot = slot;
            AimPosX = aimPosX;
            AimPosY = aimPosY;
            AimPosZ = aimPosZ;
            AimDirX = aimDirX;
            AimDirY = aimDirY;
            AimDirZ = aimDirZ;
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
            if (ctx != null && ctx.TryConsumeHudSkillAimSubmit(
                out var slot,
                out var aimPosX,
                out var aimPosY,
                out var aimPosZ,
                out var aimDirX,
                out var aimDirY,
                out var aimDirZ))
            {
                input = new BattleSkillAimSubmitInput(
                    slot,
                    aimPosX,
                    aimPosY,
                    aimPosZ,
                    aimDirX,
                    aimDirY,
                    aimDirZ);
                return true;
            }

            input = default;
            return false;
        }
    }
}
