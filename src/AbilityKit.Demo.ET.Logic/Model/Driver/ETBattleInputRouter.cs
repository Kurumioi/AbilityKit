using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;

namespace ET.Logic
{
    /// <summary>
    /// Routes battle input commands to registered input handlers.
    /// </summary>
    public static class ETBattleInputRouter
    {
        public static void SubmitInputs(ETMobaBattleDriver driver, int frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (driver.World == null || !driver.IsRunning)
                return;

            if (inputs == null || inputs.Count == 0)
                return;

            foreach (var input in inputs)
            {
                RouteInput(driver, frame, input);
            }
        }

        public static void SubmitMoveInput(ETMobaBattleDriver driver, int actorId, float targetX, float targetZ)
        {
            foreach (var handler in driver.InputHandlers)
            {
                if (handler is ISubmittableInputHandler moveHandler)
                {
                    moveHandler.Submit(driver, actorId, targetX, targetZ);
                    return;
                }
            }
        }

        public static bool SubmitSkillInput(ETMobaBattleDriver driver, int actorId, int slot, float targetX, float targetZ)
        {
            foreach (var handler in driver.InputHandlers)
            {
                if (handler is ISkillInputHandler skillHandler)
                {
                    return skillHandler.Submit(driver, actorId, slot, targetX, targetZ);
                }
            }

            return false;
        }

        public static void SubmitStopInput(ETMobaBattleDriver driver, int actorId)
        {
            foreach (var handler in driver.InputHandlers)
            {
                if (handler is IStopInputHandler stopHandler)
                {
                    stopHandler.Submit(driver, actorId);
                    return;
                }
            }
        }

        private static void RouteInput(ETMobaBattleDriver driver, int frame, PlayerInputCommand input)
        {
            foreach (var handler in driver.InputHandlers)
            {
                if (handler.CanHandle(input.OpCode))
                {
                    handler.Handle(driver, frame, input);
                    return;
                }
            }

            Log.Debug($"[ETMobaBattleDriver] No handler for OpCode: {input.OpCode}");
        }
    }
}
