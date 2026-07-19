using System;
using System.Collections;
using System.Collections.Generic;
using AbilityKit.World.ECS;
using AbilityKit.Game.EntityCreation;
using AbilityKit.Game.Flow;
using AbilityKit.Game.View.Modules;
using UnityEngine;

namespace AbilityKit.Game
{
    public sealed class GameEntry : MonoBehaviour, IGameHost
    {
        private static GameEntry _instance;

        [SerializeField] private bool _debugEnabled;
        [SerializeField] private BattleGatewayConfigSO _multiplayerGatewayConfig;

        public static GameEntry Instance
        {
            get
            {
                if (_instance == null) throw new InvalidOperationException("GameEntry is not initialized");
                return _instance;
            }
        }

        public static bool IsInitialized => _instance != null;

        public bool DebugEnabled
        {
            get => _debugEnabled;
            set => _debugEnabled = value;
        }

        public EntityWorld World { get; private set; }
        public IEntity Root { get; private set; }

        private ModuleHost<GameEntryModuleContext, IGameEntryModule> _entryModules;
        private GameEntryModuleContext _entryModuleContext;
        private GameEntryRuntimeGuiBridge _runtimeGuiBridge;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (_instance == this && _entryModules != null && _entryModules.IsAttached)
            {
                return;
            }

            _instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            World = new EntityWorld();
            Root = EntityGenerator.CreateRoot(World, "GameRoot");

            if (!Root.TryGetRef<GameFlowDomain>(out var existingFlow))
            {
                var flow = new GameFlowDomain(this, Root, new GamePresentationSink());
                Root.WithRef(flow);
                Root.WithRef<IGameFlowFeatureInstaller>(flow);
            }
            else
            {
                Root.WithRef<IGameFlowFeatureInstaller>(existingFlow);
            }

            Root.WithRef(new LobbyBattleEntrySelection());
            EnsureRuntimeGuiBridge();

            _entryModuleContext = new GameEntryModuleContext(this, Root);
            _entryModules = CreateEntryModules();
            if (_entryModules.TrySortByDependencies())
            {
                _entryModules.Attach(in _entryModuleContext);
            }
        }

        private void Start()
        {
            if (!Root.IsValid) return;
            if (Root.TryGetRef<GameFlowDomain>(out var flow))
            {
                flow.Start();
            }
        }

        private void Update()
        {
            if (!Root.IsValid) return;

            _entryModules?.Tick(in _entryModuleContext, Time.deltaTime);

            if (Root.TryGetRef<GameFlowDomain>(out var flow))
            {
                flow.Tick(Time.deltaTime);
            }
        }

        private void OnGUI()
        {
            if (_runtimeGuiBridge == null)
            {
                DispatchRuntimeGUI(drawBridgeStatus: false);
            }
        }

        internal void DispatchRuntimeGUI(bool drawBridgeStatus)
        {
            if (drawBridgeStatus)
            {
                DrawRuntimeGuiBridgeStatus();
            }

            if (!Root.IsValid) return;
            if (Root.TryGetRef<GameFlowDomain>(out var flow))
            {
                flow.OnGUI();
            }
        }

        private void EnsureRuntimeGuiBridge()
        {
            _runtimeGuiBridge = GetComponent<GameEntryRuntimeGuiBridge>();
            if (_runtimeGuiBridge == null)
            {
                _runtimeGuiBridge = gameObject.AddComponent<GameEntryRuntimeGuiBridge>();
            }

            _runtimeGuiBridge.Bind(this);
        }

        private void DrawRuntimeGuiBridgeStatus()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GUILayout.BeginArea(new Rect(10f, Screen.height - 78f, 300f, 68f), "GameEntry GUI", GUI.skin.window);
            GUILayout.Label($"Debug: {_debugEnabled}");
            GUILayout.Label($"Root: {(Root.IsValid ? "valid" : "invalid")}");
            GUILayout.EndArea();
#endif
        }

        private void OnDestroy()
        {
            if (_entryModules != null && _entryModules.IsAttached)
            {
                _entryModules.Detach(in _entryModuleContext);
            }

            _entryModules = null;
            _entryModuleContext = default;

            if (_instance == this) _instance = null;
        }

        private ModuleHost<GameEntryModuleContext, IGameEntryModule> CreateEntryModules()
        {
            var modules = new List<IGameEntryModule>
            {
                new GameEntryBootstrap()
            };
            if (_multiplayerGatewayConfig != null)
            {
                modules.Add(new MultiplayerGatewayEntryModule(_multiplayerGatewayConfig));
            }

            return new ModuleHost<GameEntryModuleContext, IGameEntryModule>(
                modules,
                message => Debug.LogError($"[GameEntry] {message}"));
        }

        public T Get<T>() where T : class
        {
            if (!Root.IsValid) throw new InvalidOperationException("Root entity is not valid");
            return Root.GetRef<T>();
        }

        public bool TryGet<T>(out T component) where T : class
        {
            if (!Root.IsValid)
            {
                component = default(T);
                return false;
            }

            return Root.TryGetRef(out component);
        }

        public void Set<T>(T component) where T : class
        {
            if (!Root.IsValid) throw new InvalidOperationException("Root entity is not valid");
            Root.WithRef(component);
        }

        public IEntity CreateNode(int childId)
        {
            if (!Root.IsValid) throw new InvalidOperationException("Root entity is not valid");
            return Root.World.CreateChild(Root, childId);
        }

        public IEntity GetNode(int childId)
        {
            if (!Root.IsValid) throw new InvalidOperationException("Root entity is not valid");
            Root.TryGetChildById(childId, out var node);
            return node;
        }

        public bool TryGetNode(int childId, out IEntity node)
        {
            if (!Root.IsValid)
            {
                node = default(IEntity);
                return false;
            }

            return Root.TryGetChildById(childId, out node);
        }

        public void RunCoroutine(IEnumerator coroutine)
        {
            StartCoroutine(coroutine);
        }
    }

    internal sealed class GameEntryRuntimeGuiBridge : MonoBehaviour
    {
        private GameEntry _entry;
        private BattleLocalDebugController _localDebug;
        private string _localDebugMessage;

        public void Bind(GameEntry entry)
        {
            if (!ReferenceEquals(_entry, entry))
            {
                _localDebug = null;
                _localDebugMessage = null;
            }

            _entry = entry;
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_entry == null && GameEntry.IsInitialized)
            {
                _entry = GameEntry.Instance;
            }

            if (_entry == null)
            {
                GUILayout.BeginArea(new Rect(10f, Screen.height - 58f, 300f, 48f), "GameEntry GUI", GUI.skin.window);
                GUILayout.Label("Entry: missing");
                GUILayout.EndArea();
                return;
            }

            DrawLocalDebugShortcuts();
            _entry.DispatchRuntimeGUI(drawBridgeStatus: true);
#endif
        }

        private void DrawLocalDebugShortcuts()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_entry == null || !_entry.DebugEnabled) return;

            _entry.TryGet(out IFlowCommandSink sink);
            var inBattle = sink != null && sink.CurrentRootPhase == MobaRootState.Battle;
            EnsureLocalDebugController();

            const float width = 280f;
            GUILayout.BeginArea(new Rect(Screen.width - width - 10f, 10f, width, 262f), "Local Shortcuts", GUI.skin.window);
            GUILayout.Label("Revision: local-debug-enabled-v4");
            GUILayout.Label($"Root: {(sink != null ? sink.CurrentRootPhase.ToString() : "missing")}");

            if (!inBattle)
            {
                GUILayout.Label("Status: waiting for battle");
            }
            else if (_localDebug == null)
            {
                GUILayout.Label("Status: controller missing");
            }
            else
            {
                GUILayout.Label($"Status: {(_localDebug.IsAvailable ? "ready" : _localDebug.UnavailableReason)}");
                GUILayout.Label($"Mode: {_localDebug.HostModeName}");
                GUILayout.Label($"Player: {_localDebug.CurrentPlayerId}");
                GUILayout.Label($"Actor: {_localDebug.CurrentActorId}");
            }

            var previousEnabled = GUI.enabled;
            GUI.enabled = true;
            if (GUILayout.Button("Switch Control", GUILayout.Height(30f)))
            {
                RunLocalDebugAction(_localDebug.TrySwitchControl);
            }

            if (GUILayout.Button("Reset Cooldowns", GUILayout.Height(30f)))
            {
                RunLocalDebugAction(_localDebug.TryResetCooldowns);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Ally", GUILayout.Height(30f)))
            {
                RunLocalDebugAction(_localDebug.TrySpawnAlly);
            }

            if (GUILayout.Button("Spawn Enemy", GUILayout.Height(30f)))
            {
                RunLocalDebugAction(_localDebug.TrySpawnEnemy);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = previousEnabled;

            if (!string.IsNullOrEmpty(_localDebugMessage))
            {
                GUILayout.Label(_localDebugMessage);
            }

            GUILayout.EndArea();
#endif
        }

        private void EnsureLocalDebugController()
        {
            if (_localDebug != null) return;
            _localDebug = new BattleLocalDebugController(ResolveBattleContext, ResolveBattleHudFeature, RefreshBattleViews);
        }

        private void RefreshBattleViews()
        {
            var view = BattleFlowDebugProvider.CurrentView;
            if (view == null && _entry != null)
            {
                _entry.TryGet(out view);
            }
            view?.RebindAll();

            var confirmed = BattleFlowDebugProvider.CurrentConfirmedView;
            if (confirmed == null && _entry != null)
            {
                _entry.TryGet(out confirmed);
            }
            confirmed?.RebindAll();
        }

        private BattleContext ResolveBattleContext()
        {
            var current = BattleFlowDebugProvider.Current;
            if (current != null) return current;
            return _entry != null && _entry.TryGet(out BattleContext ctx) ? ctx : null;
        }

        private BattleHudFeature ResolveBattleHudFeature()
        {
            var current = BattleFlowDebugProvider.CurrentHud;
            if (current != null) return current;
            return _entry != null && _entry.TryGet(out BattleHudFeature hud) ? hud : null;
        }

        private void RunLocalDebugAction(LocalDebugAction action)
        {
            if (action == null) return;
            action(out _localDebugMessage);
        }

        private delegate bool LocalDebugAction(out string message);
    }
}
