#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.World.Svelto;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Internal;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal static class ShooterSveltoGameplayScenarioEcsUtility
    {
        public static bool RemoveGroupIfExists(ISveltoWorldContext context, ExclusiveGroupStruct group)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (!context.EntitiesDB.ExistsAndIsNotEmpty(group))
            {
                return false;
            }

            context.EntityFunctions.RemoveEntitiesFromGroup(group);
            return true;
        }

        public static void RebuildIndex(Dictionary<uint, int> indexByEntityId, NativeEntityIDs ids, int count)
        {
            if (indexByEntityId == null) throw new ArgumentNullException(nameof(indexByEntityId));

            indexByEntityId.Clear();
            for (var i = 0; i < count; i++)
            {
                indexByEntityId[ids[i]] = i;
            }
        }

        public static bool IsAlive(uint entityId, NB<ShooterSveltoHealthComponent> healths, Dictionary<uint, int> indexByEntityId)
        {
            return indexByEntityId.TryGetValue(entityId, out var index) && healths[index].Alive != 0;
        }

        public static bool TryGetTransform(
            uint entityId,
            NB<ShooterSveltoTransformComponent> transforms,
            Dictionary<uint, int> indexByEntityId,
            out ShooterSveltoTransformComponent transform)
        {
            if (indexByEntityId.TryGetValue(entityId, out var index))
            {
                transform = transforms[index];
                return true;
            }

            transform = default;
            return false;
        }

        public static bool TryGetLiveTarget(
            uint entityId,
            NB<ShooterSveltoTransformComponent> transforms,
            NB<ShooterSveltoHealthComponent> healths,
            Dictionary<uint, int> indexByEntityId,
            out ShooterSveltoTransformComponent transform)
        {
            if (indexByEntityId.TryGetValue(entityId, out var index) && healths[index].Alive != 0)
            {
                transform = transforms[index];
                return true;
            }

            transform = default;
            return false;
        }

        public static void Normalize(ref float x, ref float y)
        {
            var lengthSquared = x * x + y * y;
            if (lengthSquared <= 0.000001f)
            {
                x = 1f;
                y = 0f;
                return;
            }

            var inv = 1f / MathF.Sqrt(lengthSquared);
            x *= inv;
            y *= inv;
        }
    }
}
