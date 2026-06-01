using System;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// Marks a strongly typed plan action module for discovery.
    /// New MOBA trigger actions should inherit MobaPlanActionModuleBase<TActionArgs, TModule>
    /// and pair with a MobaPlanActionSchemaBase<TActionArgs> schema.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PlanActionModuleAttribute : Attribute
    {
        public int Order { get; }

        public PlanActionModuleAttribute(int order = 0)
        {
            Order = order;
        }
    }
}
