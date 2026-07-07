#nullable enable

using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    internal sealed class ShooterPlayModeRoomState
    {
        private readonly List<ShooterGatewayRoomSummary> _rooms = new List<ShooterGatewayRoomSummary>();

        public IReadOnlyList<ShooterGatewayRoomSummary> Rooms => _rooms;
        public int Count => _rooms.Count;
        public int SelectedIndex { get; private set; } = -1;
        public int NextOffset { get; private set; }

        public void ReplaceRooms(IEnumerable<ShooterGatewayRoomSummary> rooms, int nextOffset)
        {
            _rooms.Clear();
            _rooms.AddRange(rooms);
            NextOffset = nextOffset;
            if (_rooms.Count == 0)
            {
                SelectedIndex = -1;
            }
        }

        public bool TrySelect(int index, out ShooterGatewayRoomSummary room)
        {
            if (index < 0 || index >= _rooms.Count)
            {
                SelectedIndex = -1;
                room = default;
                return false;
            }

            SelectedIndex = index;
            room = _rooms[index];
            return true;
        }
    }
}
