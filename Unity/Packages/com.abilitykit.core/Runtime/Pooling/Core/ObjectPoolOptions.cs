using System;

namespace AbilityKit.Core.Pooling
{
    public sealed class ObjectPoolOptions<T> where T : class
    {
        public Func<T> CreateFunc;
        public Action<T> OnGet;
        public Action<T> OnRelease;
        public Action<T> OnDestroy;

        public int DefaultCapacity = 0;
        public int MaxSize = 1024;
        public PoolTrimPolicy TrimPolicy = PoolTrimPolicy.KeepDefaultCapacity;

        public bool CollectionCheck = true;
        public bool NeverTrim;

        public ObjectPoolOptions(Func<T> createFunc)
        {
            CreateFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        }
    }
}
