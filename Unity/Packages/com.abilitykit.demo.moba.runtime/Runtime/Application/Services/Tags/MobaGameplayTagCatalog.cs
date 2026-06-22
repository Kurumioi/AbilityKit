using System;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA 运行时规则与配置校验共用的集中语义标签目录。
    /// </summary>
    public static class MobaGameplayTagCatalog
    {
        public static class State
        {
            public const string Untargetable = "State.Untargetable";
            public const string Invulnerable = "State.Invulnerable";
            public const string Silenced = "State.Silenced";
            public const string Disabled = "State.Disabled";
            public const string Stunned = "State.Stunned";
            public const string Rooted = "State.Rooted";
            public const string Suppressed = "State.Suppressed";
            public const string ControlImmune = "State.ControlImmune";
            public const string Feared = "State.Feared";
            public const string Asleep = "State.Asleep";
            public const string Charmed = "State.Charmed";
        }

        public static readonly string[] UntargetableAliases = { State.Untargetable, "Untargetable" };
        public static readonly string[] InvulnerableAliases = { State.Invulnerable, "Invulnerable" };
        public static readonly string[] SilencedAliases = { State.Silenced, "Silenced", "silenced" };
        public static readonly string[] DisabledAliases = { State.Disabled, "Disabled", "disabled" };
        public static readonly string[] StunnedAliases = { State.Stunned, "Stunned", "stunned" };
        public static readonly string[] RootedAliases = { State.Rooted, "Rooted", "rooted" };
        public static readonly string[] SuppressedAliases = { State.Suppressed, "Suppressed", "suppressed" };
        public static readonly string[] ControlImmuneAliases = { State.ControlImmune, "ControlImmune", "control_immune" };
        public static readonly string[] FearedAliases = { State.Feared, "Feared", "feared" };
        public static readonly string[] AsleepAliases = { State.Asleep, "Asleep", "Sleeping", "asleep", "sleeping" };
        public static readonly string[] CharmedAliases = { State.Charmed, "Charmed", "charmed" };

        public static readonly string[] MoveBlockedAliases = Combine(StunnedAliases, DisabledAliases, SuppressedAliases, RootedAliases, FearedAliases, AsleepAliases);
        public static readonly string[] CastBlockedAliases = Combine(StunnedAliases, DisabledAliases, SuppressedAliases, SilencedAliases, FearedAliases, AsleepAliases);
        public static readonly string[] ControlBlockedAliases = Combine(StunnedAliases, FearedAliases, CharmedAliases, AsleepAliases);

        public static bool TryGet(string tagName, out GameplayTag tag)
        {
            tag = default;
            return !string.IsNullOrWhiteSpace(tagName)
                && GameplayTagManager.Instance.TryGetTag(tagName, out tag)
                && tag.IsValid;
        }

        public static bool HasAny(GameplayTagContainer container, params string[] tagNames)
        {
            if (container == null || tagNames == null || tagNames.Length == 0) return false;

            for (int i = 0; i < tagNames.Length; i++)
            {
                if (TryGet(tagNames[i], out var tag) && container.HasTag(tag)) return true;
            }

            return false;
        }

        private static string[] Combine(params string[][] groups)
        {
            if (groups == null || groups.Length == 0) return Array.Empty<string>();

            var total = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null) total += groups[i].Length;
            }

            if (total == 0) return Array.Empty<string>();

            var result = new string[total];
            var offset = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                if (group == null || group.Length == 0) continue;

                Array.Copy(group, 0, result, offset, group.Length);
                offset += group.Length;
            }

            return result;
        }
    }
}
