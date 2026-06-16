namespace AbilityKit.Core.Pooling
{
    public interface IPoolable
    {
        void OnPoolGet();
        void OnPoolRelease();
        void OnPoolDestroy();
    }
}
