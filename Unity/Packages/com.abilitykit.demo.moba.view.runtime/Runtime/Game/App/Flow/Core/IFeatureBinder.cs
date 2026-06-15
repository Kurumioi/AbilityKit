namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// Abstracts runtime-level feature binding so that flow logic can be tested
    /// without depending on a concrete component storage implementation.
    /// </summary>
    public interface IFeatureBinder
    {
        void AttachFeature(object feature);
        void DetachFeature(object feature);
    }
}
