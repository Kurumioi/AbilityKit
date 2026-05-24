using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Input component System
    /// Corresponds to Moba.Console ConsoleInputFeature
    /// </summary>
    [EntitySystemOf(typeof(ETInputComponent))]
    [FriendOf(typeof(ETInputComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETInputComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETInputComponent self)
        {
            Log.Info("[ETInput] ETInputComponent awake");
        }

        /// <summary>
        /// Submit move input
        /// </summary>
        public static void SubmitMoveInput(this ETInputComponent self, int frame, long actorId, float x, float y)
        {
            self.AddMoveCommand(frame, (int)actorId, x, y);
            Log.Debug($"[ETInput] Move input: Actor {actorId} -> ({x}, {y}) at frame {frame}");
        }

        /// <summary>
        /// Submit skill input
        /// </summary>
        public static void SubmitSkillInput(this ETInputComponent self, int frame, long actorId, int skillSlot, float targetX, float targetY)
        {
            self.AddSkillCommand(frame, (int)actorId, skillSlot, targetX, targetY);
            Log.Debug($"[ETInput] Skill input: Actor {actorId} Skill {skillSlot} -> ({targetX}, {targetY}) at frame {frame}");
        }

        /// <summary>
        /// Submit stop input
        /// </summary>
        public static void SubmitStopInput(this ETInputComponent self, int frame, long actorId)
        {
            self.AddStopCommand(frame, (int)actorId);
            Log.Debug($"[ETInput] Stop input: Actor {actorId} at frame {frame}");
        }

        /// <summary>
        /// Process input
        /// </summary>
        public static void ProcessInput(this ETInputComponent self, int currentFrame)
        {
            Log.Info($"[ETInput] ProcessInput called at frame {currentFrame}");

            var commands = self.GetInputsForFrame(currentFrame);
            if (commands == null)
            {
                Log.Info($"[ETInput] No commands found for frame {currentFrame}");
                return;
            }

            Log.Info($"[ETInput] Found {commands.Count} commands at frame {currentFrame}");

            var unitComponent = self.Scene().GetComponent<ETUnitComponent>();
            if (unitComponent == null)
            {
                Log.Warning("[ETInput] ETUnitComponent not found!");
                return;
            }

            foreach (var cmd in commands)
            {
                switch (cmd)
                {
                    case MoveCommand move:
                        self.ProcessMoveCommand(unitComponent, move);
                        break;
                    case SkillCommand skill:
                        self.ProcessSkillCommand(unitComponent, skill);
                        break;
                    case StopCommand stop:
                        self.ProcessStopCommand(unitComponent, stop);
                        break;
                }
            }

            // Clear processed inputs
            self.ClearProcessedInputs(currentFrame);
        }

        private static void ProcessMoveCommand(this ETInputComponent self, ETUnitComponent unitComponent, MoveCommand cmd)
        {
            var unit = unitComponent.GetUnit(cmd.ActorId);
            if (unit == null || unit.IsDead)
            {
                Log.Warning($"[ETInput] Cannot process move: unit {cmd.ActorId} not found or dead");
                return;
            }

            unit.TargetX = cmd.X;
            unit.TargetY = cmd.Y;
            Log.Info($"[ETInput] Move processed: Actor {cmd.ActorId} -> ({cmd.X}, {cmd.Y})");
        }

        private static void ProcessSkillCommand(this ETInputComponent self, ETUnitComponent unitComponent, SkillCommand cmd)
        {
            Log.Info($"[ETInput] ProcessSkillCommand: Actor {cmd.ActorId}, Slot={cmd.SkillSlot}, Target=({cmd.TargetX}, {cmd.TargetY})");

            var unit = unitComponent.GetUnit(cmd.ActorId);
            if (unit == null || unit.IsDead)
            {
                Log.Warning($"[ETInput] Cannot process skill: unit {cmd.ActorId} not found or dead");
                return;
            }

            // Check cooldown
            if (cmd.SkillSlot >= 0 && cmd.SkillSlot < unit.SkillCooldowns.Length)
            {
                if (unit.SkillCooldowns[cmd.SkillSlot] > 0)
                {
                    Log.Info($"[ETInput] Skill {cmd.SkillSlot} is on cooldown: {unit.SkillCooldowns[cmd.SkillSlot]:F1}s");
                    return;
                }
            }

            // Set skill target
            self.CurrentSkillSlot = cmd.SkillSlot;
            self.SkillTargetX = cmd.TargetX;
            self.SkillTargetY = cmd.TargetY;

            // Simulate skill release - deal damage to enemies in range
            var targets = unitComponent.FindUnitsInRange(cmd.TargetX, cmd.TargetY, 3f);
            foreach (var target in targets)
            {
                if (target.Kind == ActorKind.Monster && !target.IsDead && target.ActorId != cmd.ActorId)
                {
                    // Deal damage
                    unitComponent.ExecuteDamage(cmd.ActorId, target.ActorId, unit.Attack * 1.5f);

                    // Set cooldown
                    if (cmd.SkillSlot >= 0 && cmd.SkillSlot < unit.SkillCooldowns.Length)
                    {
                        unit.SkillCooldowns[cmd.SkillSlot] = 2f; // 2 second cooldown
                    }
                }
            }

            Log.Info($"[ETInput] Actor {cmd.ActorId} cast skill {cmd.SkillSlot} at ({cmd.TargetX}, {cmd.TargetY})");

            // Reset skill state
            self.CurrentSkillSlot = -1;
        }

        private static void ProcessStopCommand(this ETInputComponent self, ETUnitComponent unitComponent, StopCommand cmd)
        {
            var unit = unitComponent.GetUnit(cmd.ActorId);
            if (unit == null)
                return;

            unit.StopMove();
        }
    }
}
