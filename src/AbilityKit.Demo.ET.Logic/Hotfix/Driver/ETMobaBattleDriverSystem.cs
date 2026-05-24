using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Services;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Share;
using EC = AbilityKit.World.ECS;

namespace ET.Logic
{
    /// <summary>
    /// ETMobaBattleDriver System
    /// ?? moba.core ????
    ///
    /// Design:
    /// - All business logic is in this System (?? ET ??)
    /// - Component only stores data
    /// - Lifecycle methods delegate to System methods
    /// </summary>
    [EntitySystemOf(typeof(ETMobaBattleDriver))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    public static partial class ETMobaBattleDriverSystem
    {
        // ============== Input OpCodes ==============

        private static class InputOpCode
        {
            public const int Move = 1001;
            public const int SkillInput = 1002;
        }

        // ============== Lifecycle ==============

        [EntitySystem]
        private static void Awake(this ETMobaBattleDriver self)
        {
            self.CurrentFrame = 0;
            self.LogicTimeSeconds = 0;
            self.IsRunning = false;
            Log.Info("[ETMobaBattleDriver] System awake");
        }

        [EntitySystem]
        private static void Update(this ETMobaBattleDriver self)
        {
            if (!self.IsRunning)
                return;

            double currentTime = GetCurrentTimeSeconds();
            double deltaTime = currentTime - self.LastTickTime;

            if (deltaTime >= (1.0 / self.TickRate))
            {
                Tick(self, (float)deltaTime);
                self.LastTickTime = currentTime;
            }
        }

        [EntitySystem]
        private static void Destroy(this ETMobaBattleDriver self)
        {
            StopBattle(self);
            DestroyDriver(self);
            Log.Info("[ETMobaBattleDriver] System destroyed");
        }

        // ============== Battle Control ==============

        /// <summary>
        /// Initialize battle (without config loader - will use default spawn data)
        /// </summary>
        public static void Initialize(this ETMobaBattleDriver self, in BattleStartPlan plan, IBattleViewEventSink viewSink)
        {
            Initialize(self, plan, viewSink, null);
        }

        /// <summary>
        /// Initialize battle with config loader
        /// </summary>
        public static void Initialize(this ETMobaBattleDriver self, in BattleStartPlan plan, IBattleViewEventSink viewSink, ITextAssetLoader textAssetLoader)
        {
            self.Plan = plan;
            self.ViewSink = viewSink;
            self.TextAssetLoader = textAssetLoader;
            self.TickRate = plan.TickRate > 0 ? plan.TickRate : 30;

            try
            {
                self.ConfigLoader = new ETConfigLoaderService(textAssetLoader ?? new ETTextAssetLoader(""));
                self.ConfigLoader.LoadAll();

                self.SnapshotDispatcher = new FrameSnapshotDispatcher();
                SubscribeSnapshotEvents(self);

                var args = new BattleInitArgs
                {
                    Plan = plan,
                    ViewSink = viewSink,
                    TextAssetLoader = textAssetLoader
                };

                var initRunner = BattleInitRunner.CreateDefault();
                var initContext = initRunner.Execute(args);

                self.WorldManager = initContext.WorldManager;
                self.HostRuntime = initContext.HostRuntime;
                self.World = initContext.World;

                Log.Info($"[ETMobaBattleDriver] Initialized via Flow: TickRate={self.TickRate}, WorldId={self.Plan.WorldId}, ConfigLoader={self.ConfigLoader != null}");
            }
            catch (Exception ex)
            {
                Log.Error($"[ETMobaBattleDriver] Initialize failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Start battle
        /// </summary>
        public static void StartBattle(this ETMobaBattleDriver self)
        {
            if (self.HostRuntime == null)
            {
                Log.Warning("[ETMobaBattleDriver] Cannot start, HostRuntime is null");
                return;
            }

            self.IsRunning = true;
            self.LastTickTime = GetCurrentTimeSeconds();
            self.CurrentFrame = 0;
            self.LogicTimeSeconds = 0;

            Log.Info("[ETMobaBattleDriver] Battle started (waiting for all players ready...)");
        }

        /// <summary>
        /// All players ready callback
        /// </summary>
        public static void OnAllPlayersReady(this ETMobaBattleDriver self, List<ETPlayerSpawnData> players)
        {
            if (!self.IsRunning)
            {
                Log.Warning("[ETMobaBattleDriver] Cannot spawn entities, battle not started");
                return;
            }

            self.PlayerSpawnData.Clear();
            if (players != null)
            {
                self.PlayerSpawnData.AddRange(players);
            }

            Log.Info($"[ETMobaBattleDriver] ========== OnAllPlayersReady ==========");
            Log.Info($"[ETMobaBattleDriver] Player count: {self.PlayerSpawnData.Count}");
            foreach (var p in self.PlayerSpawnData)
            {
                Log.Info($"[ETMobaBattleDriver]   - ActorId={p.ActorId}, Hero={p.CharacterName}, Team={p.TeamId}");
            }

            TriggerEnterGameSnapshot(self);
            self.ViewSink?.OnBattleStart(0);

            Log.Info($"[ETMobaBattleDriver] ========== All players ready! ==========");
        }

        /// <summary>
        /// Stop battle
        /// </summary>
        public static void StopBattle(this ETMobaBattleDriver self)
        {
            self.IsRunning = false;
            Log.Info("[ETMobaBattleDriver] Battle stopped");
        }

        /// <summary>
        /// Destroy driver
        /// </summary>
        private static void DestroyDriver(this ETMobaBattleDriver self)
        {
            if (self.HostRuntime != null && self.World != null)
            {
                self.HostRuntime.DestroyWorld(self.World.Id);
            }

            self.World = null;
            self.ViewSink = null;
            self.SnapshotDispatcher = null;
        }

        // ============== Tick ==============

        /// <summary>
        /// Tick
        /// </summary>
        public static void Tick(this ETMobaBattleDriver self, float deltaTime)
        {
            if (self.HostRuntime == null)
                return;

            self.CurrentFrame++;
            self.LogicTimeSeconds += deltaTime;

            try
            {
                self.HostRuntime.Tick(deltaTime);
                self.ViewSink?.OnFrameSyncComplete(self.CurrentFrame);
            }
            catch (Exception ex)
            {
                Log.Error($"[ETMobaBattleDriver] Tick error at frame {self.CurrentFrame}: {ex.Message}");
            }
        }

        // ============== Enter Game Snapshot ==============

        /// <summary>
        /// Trigger enter game snapshot
        /// </summary>
        private static void TriggerEnterGameSnapshot(this ETMobaBattleDriver self)
        {
            if (self.SnapshotDispatcher == null)
                return;

            Log.Info($"[ETMobaBattleDriver] >>> TriggerEnterGameSnapshot called");

            var playerIds = new List<int>(self.Plan.PlayerId > 0 ? new[] { self.Plan.PlayerId } : Array.Empty<int>());
            var teams = new List<TeamData>
            {
                new TeamData(1, playerIds)
            };

            var enterGameData = new EnterGameData(self.Plan.MapId, self.Plan.PlayerId, playerIds, teams);
            var spawns = new List<ActorSpawnData>();
            BuildActorSpawnsFromConfig(self, spawns);

            Log.Info($"[ETMobaBattleDriver] >>> Publishing OnEnterGameSnapshot with {spawns.Count} spawns");

            var snapshot = new FrameSnapshotData(0, 0, SnapshotType.Full, enterGame: enterGameData, actorSpawns: spawns);
            self.ViewSink?.OnEnterGameSnapshot(in snapshot);

            Log.Info($"[ETMobaBattleDriver] >>> EnterGameSnapshot published: MapId={enterGameData.MapId}, PlayerId={enterGameData.LocalPlayerId}, SpawnCount={spawns.Count}");
        }

        /// <summary>
        /// Build actor spawn data from pre-set player data
        /// </summary>
        private static void BuildActorSpawnsFromConfig(this ETMobaBattleDriver self, List<ActorSpawnData> spawns)
        {
            if (self.PlayerSpawnData.Count > 0)
            {
                BuildActorSpawnsFromPlayerList(self, spawns);
                return;
            }

            if (self.ConfigLoader != null && self.ConfigLoader.Characters.Count > 0)
            {
                var attributeConfigs = self.ConfigLoader.AttributeTemplates;
                int actorIdBase = self.Plan.PlayerId > 0 ? self.Plan.PlayerId : 1;

                if (self.ConfigLoader.TryGetCharacter(1001, out var heroConfig))
                {
                    attributeConfigs.TryGetValue(heroConfig.AttributeTemplateId, out var heroAttrs);
                    float hp = heroAttrs?.Hp ?? 200f;
                    float maxHp = (heroAttrs?.MaxHp > 0 ? heroAttrs.MaxHp : hp);

                    spawns.Add(new ActorSpawnData(
                        actorIdBase, heroConfig.Id, heroConfig.Name,
                        0f, 0f, 0f, 0f, 1f,
                        1, hp, maxHp));

                    Log.Info($"[ETMobaBattleDriver] Built spawn: ActorId={actorIdBase}, Character={heroConfig.Name}, Team=1, HP={hp}");
                }

                for (int i = 2; i <= 3; i++)
                {
                    int heroId = 1000 + i;
                    if (self.ConfigLoader.TryGetCharacter(heroId, out var aiConfig))
                    {
                        attributeConfigs.TryGetValue(aiConfig.AttributeTemplateId, out var aiAttrs);
                        float hp = aiAttrs?.Hp ?? 200f;
                        float maxHp = (aiAttrs?.MaxHp > 0 ? aiAttrs.MaxHp : hp);
                        int actorId = actorIdBase + i;

                        spawns.Add(new ActorSpawnData(
                            actorId, aiConfig.Id, aiConfig.Name,
                            10f * (i - 1), 0f, 0f, 0f, 1f,
                            1, hp, maxHp));

                        Log.Info($"[ETMobaBattleDriver] Built spawn: ActorId={actorId}, Character={aiConfig.Name}, Team=1, HP={hp}");
                    }
                }

                for (int i = 1; i <= 3; i++)
                {
                    int heroId = 1000 + i;
                    if (self.ConfigLoader.TryGetCharacter(heroId, out var enemyConfig))
                    {
                        attributeConfigs.TryGetValue(enemyConfig.AttributeTemplateId, out var enemyAttrs);
                        float hp = enemyAttrs?.Hp ?? 200f;
                        float maxHp = (enemyAttrs?.MaxHp > 0 ? enemyAttrs.MaxHp : hp);
                        int actorId = 2000 + i;

                        spawns.Add(new ActorSpawnData(
                            actorId, enemyConfig.Id, enemyConfig.Name,
                            0f, 0f, 50f + 10f * (i - 1), 0f, 1f,
                            2, hp, maxHp));

                        Log.Info($"[ETMobaBattleDriver] Built spawn: ActorId={actorId}, Character={enemyConfig.Name}, Team=2, HP={hp}");
                    }
                }

                return;
            }

            Log.Warning("[ETMobaBattleDriver] No config loader or characters, using default spawn");
            AddDefaultSpawns(self, spawns);
        }

        /// <summary>
        /// Build actor spawn data from pre-set player list
        /// </summary>
        private static void BuildActorSpawnsFromPlayerList(this ETMobaBattleDriver self, List<ActorSpawnData> spawns)
        {
            foreach (var player in self.PlayerSpawnData)
            {
                int actorId = player.ActorId;
                float hp = player.Hp > 0 ? player.Hp : 200f;
                float maxHp = player.MaxHp > 0 ? player.MaxHp : hp;

                spawns.Add(new ActorSpawnData(
                    actorId,
                    player.CharacterId,
                    player.CharacterName,
                    player.PositionX,
                    player.PositionY,
                    player.PositionZ,
                    player.RotationY,
                    player.Scale > 0 ? player.Scale : 1f,
                    player.TeamId,
                    hp,
                    maxHp));

                Log.Info($"[ETMobaBattleDriver] Built spawn from player data: ActorId={actorId}, Character={player.CharacterName}, Team={player.TeamId}, HP={hp}");
            }
        }

        /// <summary>
        /// Add default spawn data when config is not available
        /// </summary>
        private static void AddDefaultSpawns(this ETMobaBattleDriver self, List<ActorSpawnData> spawns)
        {
            int playerActorId = self.Plan.PlayerId > 0 ? self.Plan.PlayerId : 1;

            spawns.Add(new ActorSpawnData(
                playerActorId, 1001, "Hero_001",
                0f, 0f, 0f, 0f, 1f,
                1, 200f, 200f));

            spawns.Add(new ActorSpawnData(
                2001, 2001, "Enemy_Minion_1",
                10f, 0f, 5f, 0f, 1f,
                2, 80f, 80f));

            spawns.Add(new ActorSpawnData(
                2002, 2001, "Enemy_Minion_2",
                10f, 0f, -5f, 0f, 1f,
                2, 80f, 80f));

            Log.Info($"[ETMobaBattleDriver] Added default spawns (3 entities)");
        }

        // ============== Input Processing ==============

        /// <summary>
        /// Submit inputs to the world (called by IWorldInputSink.Submit)
        /// </summary>
        public static void SubmitInputs(this ETMobaBattleDriver self, FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (self.World == null || !self.IsRunning)
                return;

            if (inputs == null || inputs.Count == 0)
                return;

            foreach (var input in inputs)
            {
                RouteInput(self, frame, input);
            }
        }

        /// <summary>
        /// Submit move input
        /// </summary>
        public static void SubmitMoveInput(this ETMobaBattleDriver self, int actorId, float targetX, float targetZ)
        {
            MoveActor(self, actorId, targetX, targetZ);
        }

        /// <summary>
        /// Submit skill input
        /// </summary>
        public static bool SubmitSkillInput(this ETMobaBattleDriver self, int actorId, int slot, float targetX, float targetZ)
        {
            return CastSkill(self, actorId, slot, targetX, targetZ);
        }

        /// <summary>
        /// Submit stop input
        /// </summary>
        public static void SubmitStopInput(this ETMobaBattleDriver self, int actorId)
        {
            StopActor(self, actorId);
        }

        /// <summary>
        /// Move actor
        /// </summary>
        private static void MoveActor(this ETMobaBattleDriver self, int actorId, float targetX, float targetZ)
        {
            // TODO: Route to moba.core services when implemented
            Log.Debug($"[ETMobaBattleDriver] MoveActor: ActorId={actorId}, Target=({targetX}, {targetZ})");
        }

        /// <summary>
        /// Stop actor
        /// </summary>
        private static void StopActor(this ETMobaBattleDriver self, int actorId)
        {
            // TODO: Route to moba.core services when implemented
            Log.Debug($"[ETMobaBattleDriver] StopActor: ActorId={actorId}");
        }

        /// <summary>
        /// Cast skill
        /// </summary>
        private static bool CastSkill(this ETMobaBattleDriver self, int actorId, int slot, float targetX, float targetZ)
        {
            // TODO: Route to moba.core services when implemented
            return false;
        }

        /// <summary>
        /// Route individual input to appropriate handler
        /// </summary>
        private static void RouteInput(ETMobaBattleDriver self, FrameIndex frame, PlayerInputCommand input)
        {
            switch (input.OpCode)
            {
                case InputOpCode.Move:
                    HandleMoveInput(self, frame, input);
                    break;

                case InputOpCode.SkillInput:
                    HandleSkillInput(self, frame, input);
                    break;

                default:
                    Log.Debug($"[ETMobaBattleDriver] Unknown input OpCode: {input.OpCode}");
                    break;
            }
        }

        /// <summary>
        /// Handle move input
        /// </summary>
        private static void HandleMoveInput(ETMobaBattleDriver self, FrameIndex frame, PlayerInputCommand input)
        {
            if (input.Payload == null || input.Payload.Length < 12)
                return;

            int actorId = System.BitConverter.ToInt32(input.Payload, 0);
            float targetX = System.BitConverter.ToSingle(input.Payload, 4);
            float targetZ = System.BitConverter.ToSingle(input.Payload, 8);

            MoveActor(self, actorId, targetX, targetZ);
            Log.Debug($"[ETMobaBattleDriver] MoveInput: ActorId={actorId}, Target=({targetX}, {targetZ})");
        }

        /// <summary>
        /// Handle skill input
        /// </summary>
        private static void HandleSkillInput(ETMobaBattleDriver self, FrameIndex frame, PlayerInputCommand input)
        {
            if (input.Payload == null || input.Payload.Length < 16)
                return;

            int actorId = System.BitConverter.ToInt32(input.Payload, 0);
            int slot = System.BitConverter.ToInt32(input.Payload, 4);
            float targetX = System.BitConverter.ToSingle(input.Payload, 8);
            float targetZ = System.BitConverter.ToSingle(input.Payload, 12);

            CastSkill(self, actorId, slot, targetX, targetZ);
            Log.Debug($"[ETMobaBattleDriver] SkillInput: ActorId={actorId}, Slot={slot}, Target=({targetX}, {targetZ})");
        }

        // ============== Service Resolution ==============

        /// <summary>
        /// Try to resolve service
        /// </summary>
        public static bool TryResolve<T>(this ETMobaBattleDriver self, out T service) where T : class
        {
            service = null;
            if (self.World?.Services != null)
            {
                return self.World.Services.TryResolve(out service);
            }
            return false;
        }

        /// <summary>
        /// Resolve service
        /// </summary>
        public static T Resolve<T>(this ETMobaBattleDriver self) where T : class
        {
            if (self.World?.Services != null)
            {
                return self.World.Services.Resolve<T>();
            }
            throw new InvalidOperationException($"Failed to resolve service {typeof(T).Name}");
        }

        // ============== Snapshot Event Subscription ==============

        private static void SubscribeSnapshotEvents(this ETMobaBattleDriver self)
        {
            if (self.SnapshotDispatcher == null || self.ViewSink == null)
                return;

            self.SnapshotDispatcher.Subscribe<EnterGameData>((int)MobaOpCode.EnterGameSnapshot, OnEnterGameData);
            self.SnapshotDispatcher.Subscribe<ActorTransformData[]>((int)MobaOpCode.ActorTransformSnapshot, OnActorTransformData);
            self.SnapshotDispatcher.Subscribe<DamageEventData[]>((int)MobaOpCode.DamageEventSnapshot, OnDamageEventData);

            Log.Info("[ETMobaBattleDriver] Snapshot events subscribed");
        }

        private static void OnEnterGameData(int frame, EnterGameData data)
        {
            Log.Debug($"[ETMobaBattleDriver] OnEnterGameData: frame={frame}");
        }

        private static void OnActorTransformData(int frame, ActorTransformData[] data)
        {
            Log.Debug($"[ETMobaBattleDriver] OnActorTransformData: frame={frame}, count={data?.Length ?? 0}");
        }

        private static void OnDamageEventData(int frame, DamageEventData[] data)
        {
            Log.Debug($"[ETMobaBattleDriver] OnDamageEventData: frame={frame}, count={data?.Length ?? 0}");
        }

        // ============== Utility ==============

        private static double GetCurrentTimeSeconds()
        {
            return (double)Environment.TickCount64 / 1000.0;
        }
    }
}
