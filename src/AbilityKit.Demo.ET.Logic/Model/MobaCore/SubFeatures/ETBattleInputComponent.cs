using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// ?? SubFeature Component
    /// ????????
    /// </summary>
    [ComponentOf(typeof(ETBattleComponent))]
    public class ETBattleInputComponent : Entity, IAwake, IUpdate, IDestroy
    {
        // ????
        public Queue<InputFrame> InputBuffer { get; set; } = new Queue<InputFrame>();

        // ????????
        public InputFrame CurrentInput { get; set; }

        // ???? Actor ID
        public int LocalPlayerActorId { get; set; }

        public void Awake()
        {
        }

        public void Update(ETBattleInputComponent self)
        {
            // ??????
            while (self.InputBuffer.Count > 0)
            {
                var input = self.InputBuffer.Dequeue();
                ProcessInput(input);
            }
        }

        public void OnDestroy(ETBattleInputComponent self)
        {
            self.InputBuffer.Clear();
        }

        private void ProcessInput(InputFrame input)
        {
            var battle = GetParent<ETBattleComponent>();
            if (battle == null)
                return;

            switch (input.Type)
            {
                case InputType.Move:
                    battle.SubmitMoveInput(input.ActorId, input.TargetX, input.TargetZ);
                    break;
                case InputType.Skill:
                    battle.SubmitSkillInput(input.ActorId, input.Slot, input.TargetX, input.TargetZ);
                    break;
            }
        }
    }

    /// <summary>
    /// ????
    /// </summary>
    public enum InputType
    {
        Move = 0,
        Skill = 1,
        Attack = 2,
    }

    /// <summary>
    /// ???
    /// </summary>
    public struct InputFrame
    {
        public int ActorId;
        public InputType Type;
        public int Slot;
        public float TargetX;
        public float TargetZ;
        public int Frame;
    }

    /// <summary>
    /// ET Player Input Command
    /// </summary>
    public struct ETPlayerInputCmd
    {
        public int ActorId;
        public float TargetX;
        public float TargetY;
        public float TargetZ;
    }

    /// <summary>
    /// ?? SubFeature System
    /// </summary>
    [EntitySystemOf(typeof(ETBattleInputComponent))]
    [FriendOf(typeof(ETBattleInputComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    public static partial class ETBattleInputSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleInputComponent self)
        {
            Log.Info("[ETBattleInput] Awake");
            self.InputBuffer = new Queue<InputFrame>();
        }

        [EntitySystem]
        private static void Update(this ETBattleInputComponent self)
        {
            // ????? Console ??? SetInput ??
        }

        [EntitySystem]
        private static void Destroy(this ETBattleInputComponent self)
        {
            self.InputBuffer?.Clear();
        }

        public static void SetLocalPlayer(this ETBattleInputComponent self, int actorId)
        {
            self.LocalPlayerActorId = actorId;
            Log.Debug($"[ETBattleInput] LocalPlayerActorId set to {actorId}");
        }

        public static void EnqueueInput(this ETBattleInputComponent self, InputFrame input)
        {
            self.InputBuffer.Enqueue(input);
        }

        public static void EnqueueMoveInput(this ETBattleInputComponent self, int actorId, float targetX, float targetZ)
        {
            self.EnqueueInput(new InputFrame
            {
                ActorId = actorId,
                Type = InputType.Move,
                TargetX = targetX,
                TargetZ = targetZ,
                Frame = 0
            });
        }

        public static void EnqueueSkillInput(this ETBattleInputComponent self, int actorId, int slot, float targetX, float targetZ)
        {
            self.EnqueueInput(new InputFrame
            {
                ActorId = actorId,
                Type = InputType.Skill,
                Slot = slot,
                TargetX = targetX,
                TargetZ = targetZ,
                Frame = 0
            });
        }

        public static void ClearInputs(this ETBattleInputComponent self)
        {
            self.InputBuffer.Clear();
        }
    }
}
