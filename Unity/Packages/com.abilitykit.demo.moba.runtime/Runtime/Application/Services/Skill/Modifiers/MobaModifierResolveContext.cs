namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaModifierOwnerScope
    {
        None = 0,
        Actor = 1,
        SkillRuntime = 2,
        Launcher = 3,
        Projectile = 4,
        Summon = 5
    }

    public readonly struct MobaModifierOwnerRef
    {
        public MobaModifierOwnerRef(MobaModifierOwnerScope scope, int id)
        {
            Scope = scope;
            Id = id;
        }

        public MobaModifierOwnerScope Scope { get; }
        public int Id { get; }
        public bool IsValid => Scope != MobaModifierOwnerScope.None && Id > 0;

        public static MobaModifierOwnerRef Actor(int actorId)
        {
            return new MobaModifierOwnerRef(MobaModifierOwnerScope.Actor, actorId);
        }

        public static MobaModifierOwnerRef SkillRuntime(int skillRuntimeId)
        {
            return new MobaModifierOwnerRef(MobaModifierOwnerScope.SkillRuntime, skillRuntimeId);
        }

        public static MobaModifierOwnerRef Launcher(int launcherActorId)
        {
            return new MobaModifierOwnerRef(MobaModifierOwnerScope.Launcher, launcherActorId);
        }

        public static MobaModifierOwnerRef Projectile(int projectileActorId)
        {
            return new MobaModifierOwnerRef(MobaModifierOwnerScope.Projectile, projectileActorId);
        }

        public static MobaModifierOwnerRef Summon(int summonActorId)
        {
            return new MobaModifierOwnerRef(MobaModifierOwnerScope.Summon, summonActorId);
        }
    }

    public readonly struct MobaModifierResolveContext
    {
        public MobaModifierResolveContext(
            int actorId = 0,
            int skillRuntimeId = 0,
            int launcherActorId = 0,
            int projectileActorId = 0,
            int summonActorId = 0)
        {
            ActorId = actorId;
            SkillRuntimeId = skillRuntimeId;
            LauncherActorId = launcherActorId;
            ProjectileActorId = projectileActorId;
            SummonActorId = summonActorId;
        }

        public int ActorId { get; }
        public int SkillRuntimeId { get; }
        public int LauncherActorId { get; }
        public int ProjectileActorId { get; }
        public int SummonActorId { get; }

        public MobaModifierOwnerRef Actor => MobaModifierOwnerRef.Actor(ActorId);
        public MobaModifierOwnerRef SkillRuntime => MobaModifierOwnerRef.SkillRuntime(SkillRuntimeId);
        public MobaModifierOwnerRef Launcher => MobaModifierOwnerRef.Launcher(LauncherActorId);
        public MobaModifierOwnerRef Projectile => MobaModifierOwnerRef.Projectile(ProjectileActorId);
        public MobaModifierOwnerRef Summon => MobaModifierOwnerRef.Summon(SummonActorId);

        public MobaModifierOwnerRef[] ActorChain()
        {
            return BuildChain(Actor);
        }

        public MobaModifierOwnerRef[] LauncherThenActorChain()
        {
            return BuildChain(Launcher, Actor);
        }

        public MobaModifierOwnerRef[] ProjectileThenLauncherThenActorChain()
        {
            return BuildChain(Projectile, Launcher, Actor);
        }

        public MobaModifierOwnerRef[] SummonThenActorChain()
        {
            return BuildChain(Summon, Actor);
        }

        private static MobaModifierOwnerRef[] BuildChain(params MobaModifierOwnerRef[] candidates)
        {
            var count = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].IsValid)
                {
                    count++;
                }
            }

            if (count == candidates.Length)
            {
                return candidates;
            }

            var chain = new MobaModifierOwnerRef[count];
            var index = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].IsValid)
                {
                    chain[index++] = candidates[i];
                }
            }

            return chain;
        }
    }
}
