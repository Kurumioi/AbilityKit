using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Share;
using EC = AbilityKit.World.ECS;

namespace ET.Logic
{
    /// <summary>
    /// ET version of moba.core battle driver (Pure Data Component)
    ///
    /// Responsibilities:
    /// - Integrate AbilityKit.Host.Extension framework
    /// - Manage snapshot dispatching
    /// - Host World for moba.core services
    ///
    /// Design:
    /// - This Component stores data
    /// - All business logic is in ETMobaBattleDriverSystem
    /// - Lifecycle methods (Awake/Update/Destroy) are handled by the System
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETMobaBattleDriver : Entity, IAwake, IUpdate, IDestroy, IBattleDriver
    {
        // ============== IBattleDriver Implementation ==============

        public int CurrentFrame { get; set; }
        public double LogicTimeSeconds { get; set; }
        public int TickRate { get; set; } = 30;
        public bool IsRunning { get; set; }
        public IBattleViewEventSink ViewEventSink { get; set; }
        public BattleStartPlan Plan { get; set; }

        // ============== Core (World Management) ==============

        public IWorldManager WorldManager { get; set; }
        public HostRuntime HostRuntime { get; set; }
        public IWorld World { get; set; }

        // ============== View Sink (for ETBridge) ==============

        public IBattleViewEventSink ViewSink { get; set; }

        // ============== Config Loader ==============

        public ITextAssetLoader TextAssetLoader { get; set; }
        public ETConfigLoaderService ConfigLoader { get; set; }

        // ============== Player Spawn Data ==============

        public List<ETPlayerSpawnData> PlayerSpawnData { get; set; } = new();

        // ============== Snapshot Dispatcher ==============

        public FrameSnapshotDispatcher SnapshotDispatcher { get; set; }

        // ============== Sync Adapter (for Coordinator) ==============

        public IETBattleSyncAdapter SyncAdapter { get; set; }

        // ============== State ==============

        public double LastTickTime { get; set; }

        // ============== IBattleDriver Explicit Implementation ==============

        IBattleViewEventSink IBattleDriver.ViewEventSink
        {
            get => ViewSink;
            set => ViewSink = value;
        }

        // ============== Lifecycle Methods (Empty - Handled by System) ==============

        public void Awake()
        {
        }

        public void Update(ETMobaBattleDriver self)
        {
        }

        public void OnDestroy(ETMobaBattleDriver self)
        {
        }

        // ============== IBattleDriver Methods (Delegated to System) ==============

        public void Initialize(in BattleStartPlan plan, IBattleViewEventSink viewSink)
        {
            // Delegates to ETMobaBattleDriverSystem.Initialize
            SystemInitializer.Initialize(this, plan, viewSink, null);
        }

        public void Start()
        {
            SystemInitializer.StartBattle(this);
        }

        public void Stop()
        {
            SystemInitializer.StopBattle(this);
        }

        public void Destroy()
        {
            SystemInitializer.DestroyDriver(this);
        }

        public void Tick(float deltaTime)
        {
            SystemInitializer.Tick(this, deltaTime);
        }

        // Placeholder methods - actual implementation in System
        public void CreateActor(int actorId, int characterId, int teamId, float x, float y, float z) { }
        public ActorTransformData? GetActorTransform(int actorId) => null;
        public IReadOnlyList<ActorTransformData> GetAllActorTransforms() => null;
        public IReadOnlyList<int> GetAliveActorIds() => null;
        public float GetActorAttribute(int actorId, ActorAttributeType attributeType) => 0;
        public void SetActorAttribute(int actorId, ActorAttributeType attributeType, float value) { }
        public float ModifyActorAttribute(int actorId, ActorAttributeType attributeType, float delta) => 0;
        public bool IsActorDead(int actorId) => false;
        public void MarkActorDead(int actorId, int killerId) { }
        public void MoveActor(int actorId, float targetX, float targetZ) { }
        public bool CanCastSkill(int actorId, int slot) => false;
        public bool CastSkill(int actorId, int slot, float targetX, float targetZ) => false;
        public bool CastSkillOnTarget(int actorId, int slot, int targetActorId) => false;
        public float GetSkillCooldown(int actorId, int slot) => 0;
        public bool IsSkillReady(int actorId, int slot) => false;
        public int AddBuff(int actorId, int casterId, int buffId) => -1;
        public void RemoveBuff(int actorId, int buffInstanceId) { }
        public int GetBuffStack(int actorId, int buffId) => 0;
        public IReadOnlyList<int> FindActorsInRange(float x, float z, float radius, int teamFilter = -1) => null;
        public int FindNearestActor(float x, float z, float radius, int teamFilter = -1) => -1;
        public float ApplyDamage(int attackerId, int targetId, float damage, int damageType) => 0;
        public float ApplyHeal(int healerId, int targetId, float heal) => 0;

        // Additional methods used by System
        public void SubmitMoveInput(int actorId, float targetX, float targetZ)
        {
            SystemInitializer.MoveActor(this, actorId, targetX, targetZ);
        }

        public bool SubmitSkillInput(int actorId, int slot, float targetX, float targetZ)
        {
            return SystemInitializer.CastSkill(this, actorId, slot, targetX, targetZ);
        }

        public void SubmitStopInput(int actorId)
        {
            SystemInitializer.StopActor(this, actorId);
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            if (World?.Services != null)
            {
                return World.Services.TryResolve(out service);
            }
            return false;
        }

        // ============== Additional Methods ==============

        public void StartBattle()
        {
            SystemInitializer.StartBattle(this);
        }

        public void StopBattle()
        {
            SystemInitializer.StopBattle(this);
        }

        public void OnAllPlayersReady(System.Collections.Generic.List<ETPlayerSpawnData> players)
        {
            if (!IsRunning)
            {
                Log.Warning("[ETMobaBattleDriver] Cannot spawn entities, battle not started");
                return;
            }

            PlayerSpawnData.Clear();
            if (players != null)
            {
                PlayerSpawnData.AddRange(players);
            }

            Log.Info($"[ETMobaBattleDriver] ========== OnAllPlayersReady ==========");
            Log.Info($"[ETMobaBattleDriver] Player count: {PlayerSpawnData.Count}");
            foreach (var p in PlayerSpawnData)
            {
                Log.Info($"[ETMobaBattleDriver]   - ActorId={p.ActorId}, Hero={p.CharacterName}, Team={p.TeamId}");
            }

            TriggerEnterGameSnapshot();
            ViewSink?.OnBattleStart(0);

            Log.Info($"[ETMobaBattleDriver] ========== All players ready! ==========");
        }

        public void SubmitInputs(int frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (World == null || !IsRunning)
                return;

            if (inputs == null || inputs.Count == 0)
                return;

            foreach (var input in inputs)
            {
                RouteInput(this, frame, input);
            }
        }

        private static void RouteInput(ETMobaBattleDriver self, int frame, PlayerInputCommand input)
        {
            switch (input.OpCode)
            {
                case InputOpCode.Move:
                    var actorId = BitConverter.ToInt32(input.Payload, 0);
                    var targetX = BitConverter.ToSingle(input.Payload, 4);
                    var targetZ = BitConverter.ToSingle(input.Payload, 8);
                    SystemInitializer.MoveActor(self, actorId, targetX, targetZ);
                    break;

                case InputOpCode.Skill:
                    var skillActorId = BitConverter.ToInt32(input.Payload, 0);
                    var slot = BitConverter.ToInt32(input.Payload, 4);
                    var skillTargetX = BitConverter.ToSingle(input.Payload, 8);
                    var skillTargetZ = BitConverter.ToSingle(input.Payload, 12);
                    SystemInitializer.CastSkill(self, skillActorId, slot, skillTargetX, skillTargetZ);
                    break;

                default:
                    Log.Debug($"[ETMobaBattleDriver] Unknown input OpCode: {input.OpCode}");
                    break;
            }
        }

        private void TriggerEnterGameSnapshot()
        {
            if (SnapshotDispatcher == null)
                return;

            Log.Info($"[ETMobaBattleDriver] >>> TriggerEnterGameSnapshot called");

            var playerIds = new System.Collections.Generic.List<int>(Plan.PlayerId > 0 ? new[] { Plan.PlayerId } : System.Array.Empty<int>());
            var teams = new System.Collections.Generic.List<TeamData>
            {
                new TeamData(1, playerIds)
            };

            var enterGameData = new EnterGameData(Plan.MapId, Plan.PlayerId, playerIds, teams);
            var spawns = new System.Collections.Generic.List<ActorSpawnData>();
            BuildActorSpawnsFromConfig(spawns);

            Log.Info($"[ETMobaBattleDriver] >>> Publishing OnEnterGameSnapshot with {spawns.Count} spawns");

            var snapshot = new FrameSnapshotData(0, 0, SnapshotType.Full, enterGame: enterGameData, actorSpawns: spawns);
            ViewSink?.OnEnterGameSnapshot(in snapshot);

            Log.Info($"[ETMobaBattleDriver] >>> EnterGameSnapshot published: MapId={enterGameData.MapId}, PlayerId={enterGameData.LocalPlayerId}, SpawnCount={spawns.Count}");
        }

        private void BuildActorSpawnsFromConfig(System.Collections.Generic.List<ActorSpawnData> spawns)
        {
            if (PlayerSpawnData.Count > 0)
            {
                foreach (var player in PlayerSpawnData)
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
                return;
            }

            if (ConfigLoader != null && ConfigLoader.Characters.Count > 0)
            {
                var attributeConfigs = ConfigLoader.AttributeTemplates;
                int actorIdBase = Plan.PlayerId > 0 ? Plan.PlayerId : 1;

                if (ConfigLoader.TryGetCharacter(1001, out var heroConfig))
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
                    if (ConfigLoader.TryGetCharacter(heroId, out var aiConfig))
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
                    if (ConfigLoader.TryGetCharacter(heroId, out var enemyConfig))
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
            AddDefaultSpawns(spawns);
        }

        private void AddDefaultSpawns(System.Collections.Generic.List<ActorSpawnData> spawns)
        {
            int playerActorId = Plan.PlayerId > 0 ? Plan.PlayerId : 1;

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

        // Internal class for static initialization methods
        internal static class SystemInitializer
        {
            private static class InputOpCode
            {
                public const int Move = 1001;
                public const int SkillInput = 1002;
            }

            public static void Initialize(ETMobaBattleDriver self, in BattleStartPlan plan, IBattleViewEventSink viewSink, ITextAssetLoader textAssetLoader)
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

                    self.CurrentFrame = 0;
                    self.LogicTimeSeconds = 0;
                    self.IsRunning = false;

                    Log.Info($"[ETMobaBattleDriver] Initialized: TickRate={self.TickRate}, WorldId={self.Plan.WorldId}");
                }
                catch (Exception ex)
                {
                    Log.Error($"[ETMobaBattleDriver] Initialize failed: {ex.Message}");
                    throw;
                }
            }

            public static void StartBattle(ETMobaBattleDriver self)
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

                Log.Info("[ETMobaBattleDriver] Battle started");
            }

            public static void StopBattle(ETMobaBattleDriver self)
            {
                self.IsRunning = false;
                Log.Info("[ETMobaBattleDriver] Battle stopped");
            }

            public static void DestroyDriver(ETMobaBattleDriver self)
            {
                if (self.HostRuntime != null && self.World != null)
                {
                    self.HostRuntime.DestroyWorld(self.World.Id);
                }

                self.World = null;
                self.ViewSink = null;
                self.SnapshotDispatcher = null;
            }

            public static void Tick(ETMobaBattleDriver self, float deltaTime)
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

            public static void MoveActor(ETMobaBattleDriver self, int actorId, float targetX, float targetZ)
            {
                Log.Debug($"[ETMobaBattleDriver] MoveActor: ActorId={actorId}, Target=({targetX}, {targetZ})");
            }

            public static void StopActor(ETMobaBattleDriver self, int actorId)
            {
                Log.Debug($"[ETMobaBattleDriver] StopActor: ActorId={actorId}");
            }

            public static bool CastSkill(ETMobaBattleDriver self, int actorId, int slot, float targetX, float targetZ)
            {
                Log.Debug($"[ETMobaBattleDriver] CastSkill: ActorId={actorId}, Slot={slot}");
                return false;
            }

            private static double GetCurrentTimeSeconds()
            {
                return (double)Environment.TickCount64 / 1000.0;
            }
        }
    }
}
