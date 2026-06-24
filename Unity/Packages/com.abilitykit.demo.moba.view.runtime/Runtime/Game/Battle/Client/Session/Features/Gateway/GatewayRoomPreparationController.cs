using System;
using System.Threading.Tasks;

namespace AbilityKit.Game.Flow
{
    internal static class GatewayRoomPreparationController
    {
        public static async Task RunAsync(
            Func<BattleStartPlan> getPlan,
            Func<Task> waitForConnectionAsync,
            Func<Task> ensureSessionTokenAsync,
            Func<Task> createAndJoinRoomAsync,
            Func<Task> joinRoomAsync)
        {
            if (getPlan == null) throw new ArgumentNullException(nameof(getPlan));
            if (waitForConnectionAsync == null) throw new ArgumentNullException(nameof(waitForConnectionAsync));
            if (ensureSessionTokenAsync == null) throw new ArgumentNullException(nameof(ensureSessionTokenAsync));
            if (createAndJoinRoomAsync == null) throw new ArgumentNullException(nameof(createAndJoinRoomAsync));
            if (joinRoomAsync == null) throw new ArgumentNullException(nameof(joinRoomAsync));

            await waitForConnectionAsync();
            await ensureSessionTokenAsync();

            var gateway = getPlan().Gateway;
            if (gateway.AutoCreateRoom)
            {
                await createAndJoinRoomAsync();
                return;
            }

            if (gateway.AutoJoinRoom)
            {
                await joinRoomAsync();
            }
        }
    }
}
