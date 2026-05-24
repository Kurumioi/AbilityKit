using System;

namespace ET.Logic
{
    /// <summary>
    /// View SubFeature Interface
    ///
    /// Design:
    /// - ViewSubFeatures are modular components that extend battle view functionality
    /// - Each SubFeature handles a specific aspect of the view (binding, interpolation, VFX, etc.)
    /// - SubFeatures receive events and can modify how entities are displayed
    ///
    /// Note: Event types are defined in ET.AbilityKit.Demo.ET.Share namespace
    /// </summary>
    public interface IViewSubFeature
    {
        /// <summary>
        /// Called when the SubFeature is attached to a battle
        /// </summary>
        void OnAttach(ETBattleComponent battle);

        /// <summary>
        /// Called when the SubFeature is detached from a battle
        /// </summary>
        void OnDetach(ETBattleComponent battle);
    }

    /// <summary>
    /// View Spawn SubFeature Interface
    ///
    /// For SubFeatures that need to handle actor spawn events
    /// </summary>
    public interface IViewSpawnSubFeature : IViewSubFeature
    {
        /// <summary>
        /// Called when an actor spawns
        /// </summary>
        void OnActorSpawn(ET.AbilityKit.Demo.ET.Share.ActorSpawnEvent evt);

        /// <summary>
        /// Called when an actor despawns
        /// </summary>
        void OnActorDespawn(int actorId);
    }

    /// <summary>
    /// View Transform SubFeature Interface
    ///
    /// For SubFeatures that need to handle actor transform (position, rotation) events
    /// </summary>
    public interface IViewTransformSubFeature : IViewSubFeature
    {
        /// <summary>
        /// Called when an actor moves
        /// </summary>
        void OnActorMove(ET.AbilityKit.Demo.ET.Share.ActorMoveEvent evt);
    }

    /// <summary>
    /// View Attribute SubFeature Interface
    ///
    /// For SubFeatures that need to handle actor attribute (HP, MP, etc.) events
    /// </summary>
    public interface IViewAttributeSubFeature : IViewSubFeature
    {
        /// <summary>
        /// Called when an actor takes damage
        /// </summary>
        void OnActorDamage(ET.AbilityKit.Demo.ET.Share.ActorDamageEvent evt);

        /// <summary>
        /// Called when an actor dies
        /// </summary>
        void OnActorDead(ET.AbilityKit.Demo.ET.Share.ActorDeadEvent evt);

        /// <summary>
        /// Called when an actor's attribute changes
        /// </summary>
        void OnActorAttributeChange(ET.AbilityKit.Demo.ET.Share.ActorAttributeChangeEvent evt);
    }

    /// <summary>
    /// View Skill SubFeature Interface
    ///
    /// For SubFeatures that need to handle skill events
    /// </summary>
    public interface IViewSkillSubFeature : IViewSubFeature
    {
        /// <summary>
        /// Called when a skill is cast
        /// </summary>
        void OnSkillCast(ET.AbilityKit.Demo.ET.Share.SkillCastEvent evt);

        /// <summary>
        /// Called when a skill hits a target
        /// </summary>
        void OnSkillHit(ET.AbilityKit.Demo.ET.Share.SkillHitEvent evt);
    }

    /// <summary>
    /// View VFX SubFeature Interface
    ///
    /// For SubFeatures that need to handle VFX events
    /// </summary>
    public interface IViewVfxSubFeature : IViewSubFeature
    {
        /// <summary>
        /// Called when a VFX spawns
        /// </summary>
        void OnVfxSpawn(ET.AbilityKit.Demo.ET.Share.VfxSpawnEvent evt);

        /// <summary>
        /// Called when floating text should be displayed
        /// </summary>
        void OnFloatingText(ET.AbilityKit.Demo.ET.Share.FloatingTextEvent evt);
    }
}
