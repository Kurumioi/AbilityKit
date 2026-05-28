using System;
using AbilityKit.Demo.Moba.Config.BattleDemo;

namespace AbilityKit.Demo.Moba.Config.Core
{
    public interface IMobaConfigLoadProfile
    {
        string Name { get; }

        void Load(MobaConfigDatabase database);
    }

    public sealed class ResourcesJsonMobaConfigLoadProfile : IMobaConfigLoadProfile
    {
        public static readonly ResourcesJsonMobaConfigLoadProfile Default = new ResourcesJsonMobaConfigLoadProfile();

        public ResourcesJsonMobaConfigLoadProfile(
            string resourcesDir = MobaConfigPaths.DefaultResourcesDir,
            bool strict = false)
        {
            if (string.IsNullOrEmpty(resourcesDir)) throw new ArgumentException(nameof(resourcesDir));

            ResourcesDir = resourcesDir;
            Strict = strict;
        }

        public string Name => "ResourcesJson";

        public string ResourcesDir { get; }

        public bool Strict { get; }

        public void Load(MobaConfigDatabase database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));

            database.LoadFromResources(ResourcesDir, Strict);
        }
    }
}
