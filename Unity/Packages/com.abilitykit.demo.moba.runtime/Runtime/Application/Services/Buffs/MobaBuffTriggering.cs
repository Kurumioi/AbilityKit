namespace AbilityKit.Demo.Moba.Services
{
    public static class MobaBuffTriggering
    {
        public static class Prefixes
        {
            public const string Buff = "buff.";
        }

        public static class Separators
        {
            public const string EventSegment = ".";
        }

        public static class Events
        {
            public const string ApplyOrRefresh = "buff.apply";
            public const string Apply = ApplyOrRefresh;
            public const string Remove = "buff.remove";
            public const string Interval = "buff.interval";
            public const string Stack = "buff.stack";
            public const string Refresh = "buff.refresh";
            public const string Tick = "buff.tick";
            public const string End = "buff.end";
            public const string Added = "buff.added";
            public const string Removed = "buff.removed";
            public const string StackChanged = "buff.stack_changed";
            public const string EffectTick = "buff.effect_tick";

            public static string WithEffect(string baseEventId, int effectId)
            {
                return string.IsNullOrEmpty(baseEventId) ? null : string.Concat(baseEventId, Separators.EventSegment, effectId);
            }
        }

        public static class Stages
        {
            public const string ApplyOrRefresh = "apply";
            public const string Add = "add";
            public const string Remove = "remove";
            public const string Interval = "interval";

            public static bool IsRemove(string stage)
            {
                return stage == Remove;
            }
        }
    }
}
