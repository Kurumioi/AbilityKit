using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Services;
using AbilityKit.Demo.Moba.Console.Events;

namespace AbilityKit.Demo.Moba.Console.Core.Input
{
    /// <summary>
    /// 玩家输入命令
    /// </summary>
    public readonly struct PlayerInputCommand
    {
        public int Frame { get; init; }
        public int PlayerId { get; init; }
        public int OpCode { get; init; }
        public byte[] Payload { get; init; }

        public PlayerInputCommand(int frame, int playerId, int opCode, byte[] payload)
        {
            Frame = frame;
            PlayerId = playerId;
            OpCode = opCode;
            Payload = payload;
        }

        public override string ToString()
        {
            return $"[Cmd Frame={Frame} Player={PlayerId} OpCode={OpCode}]";
        }
    }

    /// <summary>
    /// 本地玩家输入事件
    /// </summary>
    public readonly struct LocalPlayerInputEvent
    {
        public int PlayerId { get; init; }
        public int OpCode { get; init; }
        public byte[] Payload { get; init; }

        public LocalPlayerInputEvent(int playerId, int opCode, byte[] payload)
        {
            PlayerId = playerId;
            OpCode = opCode;
            Payload = payload;
        }
    }

    /// <summary>
    /// 本地输入队列
    /// </summary>
    public sealed class BattleLocalInputQueue
    {
        private readonly System.Collections.Generic.Queue<LocalPlayerInputEvent> _queue = new();

        public void Enqueue(LocalPlayerInputEvent evt)
        {
            _queue.Enqueue(evt);
        }

        public void Flush()
        {
            _queue.Clear();
        }

        public int Count => _queue.Count;
    }

    /// <summary>
    /// 移动编码器
    /// </summary>
    public static class MobaMoveCodec
    {
        public static byte[] Serialize(float dx, float dz)
        {
            return System.Text.Encoding.UTF8.GetBytes($"{{\"dx\":{dx:F4},\"dz\":{dz:F4}}}");
        }

        public static (float dx, float dz) Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length < 4)
                return (0f, 0f);

            var json = System.Text.Encoding.UTF8.GetString(payload);
            float dx = 0f, dz = 0f;

            foreach (var pair in json.Trim('{', '}').Split(','))
            {
                var kv = pair.Split(':');
                if (kv.Length != 2) continue;

                var key = kv[0].Trim('"');
                var val = float.TryParse(kv[1].Trim(), out var f) ? f : 0f;

                if (key == "dx") dx = f;
                else if (key == "dz") dz = f;
            }

            return (dx, dz);
        }
    }

    /// <summary>
    /// 输入特征模块
    /// </summary>
    public sealed class ConsoleInputFeature : IInputFeature, IGameModule<ConsoleBattleContext>, IGameModuleTick<ConsoleBattleContext>
    {
        private ConsoleBattleContext _ctx;
        private BattleLocalInputQueue _inputQueue;
        private ConsoleSkillExecutor _skillExecutor;
        private BattleServices _battleServices;
        private bool _initialized;

        private float _lastMoveDx;
        private float _lastMoveDz;
        private bool _wasMoving;

        public int LocalActorId => _ctx?.LocalActorId ?? 0;

        public void SetServices(ConsoleSkillExecutor skillExecutor, BattleServices battleServices)
        {
            Platform.Log.Trace("[TRACE] ConsoleInputFeature.SetServices called");
            _skillExecutor = skillExecutor;
            _battleServices = battleServices;
            Platform.Log.Trace($"[TRACE] ConsoleInputFeature.SetServices - SkillExecutor: {_skillExecutor != null}, BattleServices: {_battleServices != null}");
        }

        public void OnAttach(ConsoleBattleContext context)
        {
            _ctx = context ?? throw new ArgumentNullException(nameof(context));
            _inputQueue = new BattleLocalInputQueue();
            _initialized = true;
            Platform.Log.Trace($"[TRACE] ConsoleInputFeature.OnAttach - LocalActorId: {_ctx.LocalActorId}, State: {_ctx.State}");
            Log.Input($"[Input] Attached (Event-based) - PlayerId: {_ctx.LocalActorId}");
        }

        public void OnDetach(ConsoleBattleContext context)
        {
            Platform.Log.Trace("[TRACE] ConsoleInputFeature.OnDetach");
            _ctx = null;
            _inputQueue = null;
            _initialized = false;
            Log.Input($"[Input] Detached");
        }

        public void Tick(ConsoleBattleContext context, float deltaTime)
        {
            Platform.Log.Trace($"[TRACE] ConsoleInputFeature.Tick - Initialized: {_initialized}, Ctx: {_ctx != null}, State: {_ctx?.State}");
            if (!_initialized || _ctx == null || _ctx.State != BattleState.InMatch)
            {
                if (_ctx != null && _ctx.State != BattleState.InMatch)
                {
                    Platform.Log.Trace($"[TRACE] ConsoleInputFeature.Tick - Skipped (State={_ctx.State} != InMatch)");
                }
                return;
            }
            Platform.Log.Trace("[TRACE] ConsoleInputFeature.Tick - Processing input");
            ProcessInput();
        }

        public void ProcessInput()
        {
            Platform.Log.Trace("[TRACE] ConsoleInputFeature.ProcessInput - Entry");
            if (_ctx == null || _inputQueue == null)
            {
                Platform.Log.Trace("[TRACE] ConsoleInputFeature.ProcessInput - Skipped (null context or queue)");
                return;
            }

            _ctx.LastFrame++;

            ProcessMoveInput();
            ProcessSkillInput();

            // 执行技能系统
            _skillExecutor?.Step(_ctx.LocalActorId);

            _inputQueue.Flush();
            Platform.Log.Trace("[TRACE] ConsoleInputFeature.ProcessInput - Exit");
        }

        private void ProcessMoveInput()
        {
            var dx = _ctx.HudMoveDx;
            var dz = _ctx.HudMoveDz;
            var isMoving = Math.Abs(dx) > 0.01f || Math.Abs(dz) > 0.01f;

            if (isMoving || _wasMoving)
            {
                var payload = MobaMoveCodec.Serialize(dx, dz);
                _inputQueue.Enqueue(new LocalPlayerInputEvent(_ctx.LocalActorId, (int)MobaOpCode.Move, payload));

                // 通过事件通知移动输入
                BattleEventBus.Publish(new MoveInputProcessedEvent
                {
                    ActorId = _ctx.LocalActorId,
                    Dx = dx,
                    Dz = dz
                });

                Log.Input($"[Input] Move: dx={dx:F2}, dz={dz:F2}");
            }

            _wasMoving = isMoving;
            _lastMoveDx = dx;
            _lastMoveDz = dz;
        }

        private void ProcessSkillInput()
        {
            // 处理技能点击
            var slot = _ctx.HudSkillClickSlot;
            if (slot > 0)
            {
                Platform.Log.Trace($"[TRACE] ProcessSkillInput - Skill{slot} clicked");
                var opCode = slot switch
                {
                    1 => (int)MobaOpCode.Skill1,
                    2 => (int)MobaOpCode.Skill2,
                    3 => (int)MobaOpCode.Skill3,
                    _ => (int)MobaOpCode.SkillInput
                };

                // 通过 SkillExecutor 执行
                if (_skillExecutor != null)
                {
                    Platform.Log.Trace($"[TRACE] ProcessSkillInput - Calling SkillExecutor.CastBySlot for Actor#{_ctx.LocalActorId} Slot{slot}");
                    var result = _skillExecutor.CastBySlot(_ctx.LocalActorId, slot);
                    Platform.Log.Trace($"[TRACE] ProcessSkillInput - SkillExecutor result: Success={result.Success}, Reason={result.FailReason}");

                    // 通过事件通知技能执行结果
                    BattleEventBus.Publish(new SkillExecutedEvent
                    {
                        ActorId = _ctx.LocalActorId,
                        Slot = slot,
                        Success = result.Success,
                        FailReason = result.FailReason
                    });

                    if (result.Success)
                    {
                        Log.Skill($"[Input] Skill{slot} executed successfully");
                    }
                    else
                    {
                        Log.Skill($"[Input] Skill{slot} failed: {result.FailReason}");
                    }
                }
                else
                {
                    // 回退到队列
                    Platform.Log.Trace("[TRACE] ProcessSkillInput - SkillExecutor is null, using queue fallback");
                    var skillEvent = SkillInputEvent.CreatePress(slot);
                    var payload = SkillInputCodec.Serialize(in skillEvent);
                    _inputQueue.Enqueue(new LocalPlayerInputEvent(_ctx.LocalActorId, opCode, payload));

                    // 发布失败事件
                    BattleEventBus.Publish(new SkillExecutedEvent
                    {
                        ActorId = _ctx.LocalActorId,
                        Slot = slot,
                        Success = false,
                        FailReason = "SkillExecutor not available"
                    });
                }

                Log.Input($"[Input] Skill{slot} pressed");
                _ctx.HudSkillClickSlot = 0;
            }

            // 处理技能瞄准释放
            if (_ctx.HudSkillAimSubmit && _ctx.HudSkillAimSubmitSlot > 0)
            {
                Platform.Log.Trace($"[TRACE] ProcessSkillInput - Skill{_ctx.HudSkillAimSubmitSlot} aimed release");
                var skillEvent = SkillInputEvent.CreateRelease(
                    _ctx.HudSkillAimSubmitSlot,
                    _ctx.HudSkillAimSubmitDx,
                    _ctx.HudSkillAimSubmitDz);

                // 通过 SkillExecutor 执行
                if (_skillExecutor != null)
                {
                    var result = _skillExecutor.CastBySlot(
                        _ctx.LocalActorId,
                        _ctx.HudSkillAimSubmitSlot,
                        _ctx.HudSkillAimSubmitDx,
                        _ctx.HudSkillAimSubmitDz);

                    Platform.Log.Trace($"[TRACE] ProcessSkillInput - Aimed skill result: Success={result.Success}, Reason={result.FailReason}");

                    // 通过事件通知
                    BattleEventBus.Publish(new SkillExecutedEvent
                    {
                        ActorId = _ctx.LocalActorId,
                        Slot = _ctx.HudSkillAimSubmitSlot,
                        Success = result.Success,
                        FailReason = result.FailReason
                    });

                    if (result.Success)
                    {
                        Log.Skill($"[Input] Skill{_ctx.HudSkillAimSubmitSlot} (aimed) executed successfully");
                    }
                    else
                    {
                        Log.Skill($"[Input] Skill{_ctx.HudSkillAimSubmitSlot} (aimed) failed: {result.FailReason}");
                    }
                }
                else
                {
                    var payload = SkillInputCodec.Serialize(in skillEvent);
                    _inputQueue.Enqueue(new LocalPlayerInputEvent(_ctx.LocalActorId, (int)MobaOpCode.SkillInput, payload));

                    BattleEventBus.Publish(new SkillExecutedEvent
                    {
                        ActorId = _ctx.LocalActorId,
                        Slot = _ctx.HudSkillAimSubmitSlot,
                        Success = false,
                        FailReason = "SkillExecutor not available"
                    });
                }

                Log.Input($"[Input] Skill{_ctx.HudSkillAimSubmitSlot} aim released at ({_ctx.HudSkillAimSubmitDx:F1}, {_ctx.HudSkillAimSubmitDz:F1})");
                _ctx.HudSkillAimSubmit = false;
            }
        }

        public void SetMoveInput(float dx, float dz)
        {
            if (_ctx == null) return;
            _ctx.HudMoveDx = dx;
            _ctx.HudMoveDz = dz;
            _ctx.HudHasMove = Math.Abs(dx) > 0.01f || Math.Abs(dz) > 0.01f;
        }

        public void ClickSkill(int slot)
        {
            if (_ctx == null) return;
            Platform.Log.Trace($"[TRACE] ConsoleInputFeature.ClickSkill({slot})");
            _ctx.HudSkillClickSlot = slot;
        }

        public void AimSkill(int slot, float dx, float dz)
        {
            if (_ctx == null) return;
            _ctx.HudSkillAiming = true;
            _ctx.HudSkillAimSlot = slot;
            _ctx.HudSkillAimDx = dx;
            _ctx.HudSkillAimDz = dz;
        }

        public void ReleaseSkillAim(int slot, float dx, float dz)
        {
            if (_ctx == null) return;
            _ctx.HudSkillAimSubmit = true;
            _ctx.HudSkillAimSubmitSlot = slot;
            _ctx.HudSkillAimSubmitDx = dx;
            _ctx.HudSkillAimSubmitDz = dz;
        }
    }
}
