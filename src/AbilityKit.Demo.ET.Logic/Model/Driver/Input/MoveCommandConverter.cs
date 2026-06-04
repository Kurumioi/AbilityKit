using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    [ETInputCommandConverter(typeof(MoveCommand))]
    public sealed class MoveCommandConverter : IETInputCommandConverter
    {
        public Type CommandType => typeof(MoveCommand);

        public bool TryConvert(object command, FrameIndex frameIndex, out PlayerInputCommand playerCommand)
        {
            if (!(command is MoveCommand move))
            {
                playerCommand = default;
                return false;
            }

            var payload = MobaMoveCodec.Serialize(move.Dx, move.Dz);
            playerCommand = new PlayerInputCommand(
                frameIndex,
                new PlayerId(move.PlayerId),
                MobaOpCodes.Input.Move,
                payload);
            Log.Debug($"[MoveCommandConverter] PlayerId={move.PlayerId}, Dir=({move.Dx:F2}, {move.Dz:F2})");
            return true;
        }
    }
}
