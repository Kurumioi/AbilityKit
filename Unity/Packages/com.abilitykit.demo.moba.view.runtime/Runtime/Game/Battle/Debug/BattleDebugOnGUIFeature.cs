using System;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityConstruction;
using AbilityKit.Protocol.Moba;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    public sealed class BattleDebugOnGUIFeature : IGamePhaseFeature, IOnGUIFeature
    {
        private BattleContext _ctx;
        private BattleLocalDebugController _localDebug;
        private string _localDebugMessage;

        public void OnAttach(in GamePhaseContext ctx)
        {
            ctx.Features.TryGet(out _ctx);
            ctx.Features.TryGet(out BattleHudFeature hud);
            _localDebug = new BattleLocalDebugController(_ctx, () => hud);
            BattleFlowDebugProvider.Current = _ctx;
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            if (ReferenceEquals(BattleFlowDebugProvider.Current, _ctx))
            {
                BattleFlowDebugProvider.Current = null;
            }
            _localDebug = null;
            _ctx = null;
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
        }

        public void OnGUI(in GamePhaseContext ctx)
        {
#if UNITY_EDITOR
            if (!ctx.Entry.DebugEnabled) return;

            var sink = ctx.Entry.Get<IFlowCommandSink>();
            if (sink == null || sink.CurrentRootPhase != MobaRootState.Battle) return;

            GUILayout.BeginArea(new Rect(10, 10, 170, 110), GUI.skin.window);
            if (GUILayout.Button("Exit Battle", GUILayout.Height(34)))
            {
                sink.RequestReturnLobby();
            }

            if (GUILayout.Button("Rebind Views", GUILayout.Height(34)))
            {
                if (ctx.Features.TryGet(out BattleViewFeature view) && view != null)
                {
                    view.RebindAll();
                }
                if (ctx.Features.TryGet(out ConfirmedBattleViewFeature confirmed) && confirmed != null)
                {
                    confirmed.RebindAll();
                }
            }
            GUILayout.EndArea();

            DrawLocalDebugPanel();
#endif
        }

        private void DrawLocalDebugPanel()
        {
#if UNITY_EDITOR
            if (_localDebug == null || !_localDebug.IsAvailable) return;

            var width = 240f;
            GUILayout.BeginArea(new Rect(Screen.width - width - 10f, 10f, width, 185f), "Local Debug", GUI.skin.window);
            GUILayout.Label($"Player: {_localDebug.CurrentPlayerId}");
            GUILayout.Label($"Actor: {_localDebug.CurrentActorId}");

            if (GUILayout.Button("Switch Control", GUILayout.Height(28)))
            {
                RunLocalDebugAction(_localDebug.TrySwitchControl);
            }

            if (GUILayout.Button("Reset Cooldowns", GUILayout.Height(28)))
            {
                RunLocalDebugAction(_localDebug.TryResetCooldowns);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Ally", GUILayout.Height(28)))
            {
                RunLocalDebugAction(_localDebug.TrySpawnAlly);
            }

            if (GUILayout.Button("Spawn Enemy", GUILayout.Height(28)))
            {
                RunLocalDebugAction(_localDebug.TrySpawnEnemy);
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_localDebugMessage))
            {
                GUILayout.Label(_localDebugMessage);
            }
            GUILayout.EndArea();
#endif
        }

        private void RunLocalDebugAction(LocalDebugAction action)
        {
            if (action == null) return;
            action(out _localDebugMessage);
        }

        private delegate bool LocalDebugAction(out string message);
    }

    internal sealed class BattleLocalDebugController
    {
        private const float SpawnForwardOffset = 2f;
        private const float SpawnSideOffset = 1.25f;

        private readonly BattleContext _ctx;
        private readonly Func<BattleHudFeature> _hudResolver;

        public BattleLocalDebugController(BattleContext ctx, Func<BattleHudFeature> hudResolver)
        {
            _ctx = ctx;
            _hudResolver = hudResolver;
        }

        public bool IsAvailable => _ctx != null && _ctx.Session != null && _ctx.Plan.HostMode == BattleStartConfig.BattleHostMode.Local;

        public string CurrentPlayerId => _ctx != null ? _ctx.ResolveLocalControlPlayerId() : string.Empty;

        public int CurrentActorId => _ctx != null ? _ctx.LocalActorId : 0;

        public bool TrySwitchControl(out string message)
        {
            message = string.Empty;
            if (!IsAvailable)
            {
                message = "local battle unavailable";
                return false;
            }

            var players = _ctx.Plan.LaunchSpec.Players;
            if (players == null || players.Length <= 1)
            {
                message = "need at least 2 players";
                return false;
            }

            var current = CurrentPlayerId;
            var currentIndex = 0;
            for (var i = 0; i < players.Length; i++)
            {
                if (string.Equals(players[i].PlayerId.Value, current, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }

            for (var step = 1; step <= players.Length; step++)
            {
                var next = players[(currentIndex + step) % players.Length];
                if (TrySetControlPlayer(next.PlayerId, out message))
                {
                    return true;
                }
            }

            if (string.IsNullOrEmpty(message)) message = "no controllable player found";
            return false;
        }

        public bool TrySetControlPlayer(PlayerId playerId, out string message)
        {
            message = string.Empty;
            if (!IsAvailable)
            {
                message = "local battle unavailable";
                return false;
            }

            if (string.IsNullOrEmpty(playerId.Value))
            {
                message = "player id is empty";
                return false;
            }

            if (!TryResolveWorldService<MobaPlayerActorMapService>(out var playerActors) || playerActors == null)
            {
                message = "player actor map missing";
                return false;
            }

            if (!playerActors.TryGetActorId(playerId, out var actorId) || actorId <= 0)
            {
                message = $"actor not found for {playerId.Value}";
                return false;
            }

            _ctx.LocalControlPlayerId = playerId.Value;
            _ctx.LocalActorId = actorId;
            _hudResolver?.Invoke()?.RefreshLocalControlSkillTemplates();
            message = $"control {playerId.Value} actor={actorId}";
            return true;
        }

        public bool TryResetCooldowns(out string message)
        {
            message = string.Empty;
            if (!TryResolveCurrentActor(out var actor, out message)) return false;
            if (!actor.hasSkillLoadout || actor.skillLoadout.ActiveSkills == null)
            {
                message = "active skills missing";
                return false;
            }

            var count = 0;
            var skills = actor.skillLoadout.ActiveSkills;
            for (var i = 0; i < skills.Length; i++)
            {
                var skill = skills[i];
                if (skill == null) continue;
                skill.CooldownEndTimeMs = 0L;
                skill.CooldownDurationMs = 0;
                count++;
            }

            message = $"reset cd count={count}";
            return count > 0;
        }

        public bool TrySpawnAlly(out string message)
        {
            return TrySpawnUnit(enemy: false, out message);
        }

        public bool TrySpawnEnemy(out string message)
        {
            return TrySpawnUnit(enemy: true, out message);
        }

        private bool TrySpawnUnit(bool enemy, out string message)
        {
            message = string.Empty;
            if (!TryResolveCurrentActor(out var controlledActor, out message)) return false;
            if (!TryFindSpawnTemplate(enemy, controlledActor, out var template, out message)) return false;
            if (!TryResolveWorldService<IMobaActorSpawnService>(out var spawn) || spawn == null)
            {
                message = "spawn service missing";
                return false;
            }

            var basePos = controlledActor.hasTransform ? controlledActor.transform.Value.Position : Vec3.Zero;
            var offset = enemy ? new Vec3(SpawnSideOffset, 0f, SpawnForwardOffset) : new Vec3(-SpawnSideOffset, 0f, SpawnForwardOffset);
            var spawnPos = basePos + offset;
            var playerId = new PlayerId($"debug_{(enemy ? "enemy" : "ally")}_{DateTime.UtcNow.Ticks}");
            var loadout = new MobaPlayerLoadout(
                playerId,
                template.TeamId,
                template.HeroId,
                template.AttributeTemplateId,
                template.Level,
                template.BasicAttackSkillId,
                template.SkillIds,
                template.SpawnIndex,
                (int)UnitSubType.Minion,
                (int)EntityMainType.Unit,
                hasSpawnPosition: 1,
                spawnX: spawnPos.X,
                spawnY: spawnPos.Y,
                spawnZ: spawnPos.Z);

            var info = new MobaEntityInfo(
                actorId: 0,
                kind: MobaEntityKind.Minion,
                transform: new Transform3(spawnPos, Quat.Identity, Vec3.One),
                team: (Team)loadout.TeamId,
                mainType: EntityMainType.Unit,
                unitSubType: UnitSubType.Minion,
                ownerPlayer: playerId,
                templateId: loadout.AttributeTemplateId);
            var spec = new MobaActorBuildSpec(in info, MobaActorBuildSourceKind.PlayerLoadout, loadout.HeroId, ownerActorId: 0);
            var request = MobaActorSpawnRequest.FromSpec(in spec);
            request.AllocateActorIdIfMissing = true;
            request.Initializer = (entity, _) =>
            {
                if (TryResolveWorldService<ActorEntityInitPipeline>(out var init) && init != null)
                {
                    init.InitializeFromLoadout(entity, in loadout);
                }
            };

            if (!spawn.TrySpawn(in request, out var result) || !result.Success)
            {
                message = string.IsNullOrEmpty(result.Error) ? "spawn failed" : result.Error;
                return false;
            }

            message = $"spawn {(enemy ? "enemy" : "ally")} actor={result.ActorId}";
            return true;
        }

        private bool TryFindSpawnTemplate(bool enemy, global::ActorEntity controlledActor, out MobaPlayerLoadout template, out string message)
        {
            template = default;
            message = string.Empty;
            var players = _ctx.Plan.LaunchSpec.Players;
            if (players == null || players.Length == 0)
            {
                message = "launch players missing";
                return false;
            }

            var controlledTeam = controlledActor != null && controlledActor.hasTeam ? (int)controlledActor.team.Value : 0;
            for (var i = 0; i < players.Length; i++)
            {
                var candidate = players[i];
                var isEnemy = controlledTeam > 0 && candidate.TeamId > 0 && candidate.TeamId != controlledTeam;
                if (enemy == isEnemy)
                {
                    template = candidate;
                    return true;
                }
            }

            template = players[0];
            if (enemy && controlledTeam > 0)
            {
                var fallbackTeam = controlledTeam == (int)Team.Team1 ? (int)Team.Team2 : (int)Team.Team1;
                template = new MobaPlayerLoadout(
                    template.PlayerId,
                    fallbackTeam,
                    template.HeroId,
                    template.AttributeTemplateId,
                    template.Level,
                    template.BasicAttackSkillId,
                    template.SkillIds,
                    template.SpawnIndex,
                    template.UnitSubType,
                    template.MainType,
                    template.HasSpawnPosition,
                    template.SpawnX,
                    template.SpawnY,
                    template.SpawnZ);
            }

            return true;
        }

        private bool TryResolveCurrentActor(out global::ActorEntity actor, out string message)
        {
            actor = null;
            message = string.Empty;
            if (!IsAvailable)
            {
                message = "local battle unavailable";
                return false;
            }

            if (_ctx.LocalActorId <= 0)
            {
                if (!TryRefreshCurrentActorId(out message)) return false;
            }

            if (!TryResolveWorldService<MobaActorLookupService>(out var actors) || actors == null)
            {
                message = "actor lookup missing";
                return false;
            }

            if (!actors.TryGetActorEntity(_ctx.LocalActorId, out actor) || actor == null)
            {
                message = $"actor missing id={_ctx.LocalActorId}";
                return false;
            }

            return true;
        }

        private bool TryRefreshCurrentActorId(out string message)
        {
            var playerId = new PlayerId(CurrentPlayerId);
            return TrySetControlPlayer(playerId, out message);
        }

        private bool TryResolveWorldService<T>(out T service) where T : class
        {
            service = null;
            if (_ctx?.Session == null) return false;
            if (!_ctx.Session.TryGetWorld(out var world) || world?.Services == null) return false;
            return world.Services.TryResolve(out service) && service != null;
        }
    }
}
