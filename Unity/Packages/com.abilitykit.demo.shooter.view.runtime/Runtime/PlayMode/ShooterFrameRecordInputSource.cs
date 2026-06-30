#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Core.Recording.FrameRecord;
using AbilityKit.Demo.Shooter.View.Hosting;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public sealed class ShooterFrameRecordInputSource : IShooterHostInputSource
    {
        private readonly Dictionary<int, ShooterHostFrameInput> _inputsByFrame;
        private int _frameCursor;
        private ShooterHostFrameInput _lastInput;

        public ShooterFrameRecordInputSource(FrameRecordFile record, int controlledPlayerId)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            _inputsByFrame = BuildInputMap(record, controlledPlayerId);
        }

        public int FrameCursor => _frameCursor;

        public int InputFrameCount => _inputsByFrame.Count;

        public void Reset()
        {
            _frameCursor = 0;
            _lastInput = default;
        }

        public ShooterHostFrameInput ReadInput(int controlledPlayerId)
        {
            if (_inputsByFrame.TryGetValue(_frameCursor, out var input))
            {
                _lastInput = input;
            }
            else
            {
                _lastInput = default;
            }

            _frameCursor++;
            return _lastInput;
        }

        private static Dictionary<int, ShooterHostFrameInput> BuildInputMap(FrameRecordFile record, int controlledPlayerId)
        {
            var inputs = record.Inputs;
            var map = new Dictionary<int, ShooterHostFrameInput>(inputs?.Count ?? 0);
            if (inputs == null || inputs.Count == 0)
            {
                return map;
            }

            for (var i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (input == null || input.OpCode != ShooterOpCodes.Input.PlayerCommand)
                {
                    continue;
                }

                var payload = DecodePayload(input.PayloadBase64);
                var commands = ShooterInputCodec.Deserialize(payload);
                for (var c = 0; c < commands.Length; c++)
                {
                    var command = commands[c];
                    if (command.PlayerId != controlledPlayerId)
                    {
                        continue;
                    }

                    map[input.Frame] = new ShooterHostFrameInput(
                        command.MoveX,
                        command.MoveY,
                        command.AimX,
                        command.AimY,
                        command.Fire);
                    break;
                }
            }

            return map;
        }

        private static byte[] DecodePayload(string? payloadBase64)
        {
            if (string.IsNullOrEmpty(payloadBase64))
            {
                return Array.Empty<byte>();
            }

            return Convert.FromBase64String(payloadBase64);
        }
    }
}
