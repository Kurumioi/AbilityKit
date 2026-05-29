using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Protocol.Moba.FrameSync;

namespace ET.Logic
{
    [ETInputCommandConverter(typeof(StopCommand))]
    public sealed class StopCommandConverter : IETInputCommandConverter
    {
        public Type CommandType => typeof(StopCommand);

        public bool TryConvert(object command, FrameIndex frameIndex, out PlayerInputCommand playerCommand)
        {
            if (!(command is StopCommand stop))
            {
                playerCommand = default;
                return false;
            }

            playerCommand = new PlayerInputCommand(
                frameIndex,
                new PlayerId(stop.PlayerId),
                InputOpCodes.Stop,
                null);
            Log.Debug($"[StopCommandConverter] PlayerId={stop.PlayerId}");
            return true;
        }
    }
}
