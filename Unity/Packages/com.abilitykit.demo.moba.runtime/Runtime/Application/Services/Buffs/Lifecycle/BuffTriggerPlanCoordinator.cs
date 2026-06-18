using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Components;

namespace AbilityKit.Demo.Moba.Services.Buffs.Lifecycle
{
    /// <summary>
    /// Buff 持续触发计划绑定器：只负责按 Buff 实例 ownerKey 维护 Actor 上的 ongoing trigger plan 条目。
    /// </summary>
    internal sealed class BuffTriggerPlanCoordinator
    {
        private readonly Dictionary<int, int[]> _triggerIdsByBuffId = new Dictionary<int, int[]>();

        public void Upsert(global::ActorEntity e, long ownerKey, BuffMO buff)
        {
            if (e == null) return;
            if (ownerKey == 0) return;
            if (buff == null) return;

            if (buff.TriggerIds == null || buff.TriggerIds.Count == 0)
            {
                Remove(e, ownerKey);
                return;
            }

            var ids = GetOrCreateTriggerIds(buff);
            var list = e.hasOngoingTriggerPlans ? e.ongoingTriggerPlans.Active : null;
            if (list == null)
            {
                list = new List<OngoingTriggerPlanEntry>(1);
            }

            var replaced = false;
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (it == null) continue;
                if (it.OwnerKey != ownerKey) continue;

                it.TriggerIds = ids;
                replaced = true;
                break;
            }

            if (!replaced)
            {
                list.Add(new OngoingTriggerPlanEntry { OwnerKey = ownerKey, TriggerIds = ids });
            }

            var rev = e.hasOngoingTriggerPlans ? e.ongoingTriggerPlans.Revision + 1 : 1;
            if (e.hasOngoingTriggerPlans) e.ReplaceOngoingTriggerPlans(list, rev);
            else e.AddOngoingTriggerPlans(list, rev);
        }

        public static void Remove(global::ActorEntity e, long ownerKey)
        {
            if (e == null) return;
            if (ownerKey == 0) return;
            if (!e.hasOngoingTriggerPlans) return;

            var list = e.ongoingTriggerPlans.Active;
            if (list == null || list.Count == 0) return;

            var removedAny = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var it = list[i];
                if (it == null || it.OwnerKey == ownerKey)
                {
                    list.RemoveAt(i);
                    removedAny = true;
                }
            }

            if (!removedAny) return;

            var rev = e.ongoingTriggerPlans.Revision + 1;
            if (list.Count == 0) e.RemoveOngoingTriggerPlans();
            else e.ReplaceOngoingTriggerPlans(list, rev);
        }

        private int[] GetOrCreateTriggerIds(BuffMO buff)
        {
            if (buff == null || buff.TriggerIds == null || buff.TriggerIds.Count == 0) return Array.Empty<int>();
            if (_triggerIdsByBuffId.TryGetValue(buff.Id, out var ids) && ids != null && ids.Length == buff.TriggerIds.Count)
            {
                return ids;
            }

            ids = new int[buff.TriggerIds.Count];
            for (int i = 0; i < buff.TriggerIds.Count; i++) ids[i] = buff.TriggerIds[i];
            _triggerIdsByBuffId[buff.Id] = ids;
            return ids;
        }
    }
}
