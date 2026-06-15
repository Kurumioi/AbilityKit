namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// Abstracts the Entity-level feature binding (attach/detach) so that
    /// flow logic can be tested without <c>IEntity.WithRef</c> / <c>IEntity.RemoveComponent</c>.
    /// </summary>
    public interface IFeatureBinder
    {
        void AttachFeature(object feature);
        void DetachFeature(object feature);
    }
}
