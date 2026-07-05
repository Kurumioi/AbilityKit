#nullable enable

using AbilityKit.Demo.Shooter.View.Hosting;
using UnityEngine;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public enum ShooterUnityViewRenderBackend
    {
        GameObject = 0,
        GpuInstancedDotsReady = 1,
        EntitiesGraphics = 2
    }

    public readonly struct ShooterUnityViewRenderBackendDescriptor
    {
        public ShooterUnityViewRenderBackendDescriptor(
            ShooterUnityViewRenderBackend backend,
            string displayName,
            string capabilitySummary,
            bool isHighDensity,
            bool requiresDotsPackages,
            bool isAvailable)
        {
            Backend = backend;
            DisplayName = displayName;
            CapabilitySummary = capabilitySummary;
            IsHighDensity = isHighDensity;
            RequiresDotsPackages = requiresDotsPackages;
            IsAvailable = isAvailable;
        }

        public ShooterUnityViewRenderBackend Backend { get; }

        public string DisplayName { get; }

        public string CapabilitySummary { get; }

        public bool IsHighDensity { get; }

        public bool RequiresDotsPackages { get; }

        public bool IsAvailable { get; }
    }

    public static class ShooterUnityViewRenderBackendCatalog
    {
        private static readonly ShooterUnityViewRenderBackendDescriptor[] Backends =
        {
            new ShooterUnityViewRenderBackendDescriptor(
                ShooterUnityViewRenderBackend.GameObject,
                "GameObject",
                "兼容路径，保留调试友好的对象层级和池化表现。",
                isHighDensity: false,
                requiresDotsPackages: false,
                isAvailable: true),
            new ShooterUnityViewRenderBackendDescriptor(
                ShooterUnityViewRenderBackend.GpuInstancedDotsReady,
                "GPU Instanced",
                "高密度演示路径，通过批量实例化承接后续 DOTS/Entities Graphics 后端。",
                isHighDensity: true,
                requiresDotsPackages: false,
                isAvailable: true),
            new ShooterUnityViewRenderBackendDescriptor(
                ShooterUnityViewRenderBackend.EntitiesGraphics,
                "Entities Graphics",
                "真实 DOTS 渲染路径占位；安装 Entities/Entities Graphics 包后在此分支接入。",
                isHighDensity: true,
                requiresDotsPackages: true,
                isAvailable: false)
        };

        private static readonly string[] DisplayNames = CreateDisplayNamesCore();

        public static ShooterUnityViewRenderBackend DefaultBackend => ShooterUnityViewRenderBackend.GpuInstancedDotsReady;

        public static int Count => Backends.Length;

        public static ShooterUnityViewRenderBackendDescriptor Get(int index)
        {
            return Backends[ClampIndex(index)];
        }

        public static ShooterUnityViewRenderBackendDescriptor Get(ShooterUnityViewRenderBackend backend)
        {
            for (var i = 0; i < Backends.Length; i++)
            {
                if (Backends[i].Backend == backend)
                {
                    return Backends[i];
                }
            }

            return Get(DefaultBackend);
        }

        public static string[] GetDisplayNames()
        {
            return DisplayNames;
        }

        private static string[] CreateDisplayNamesCore()
        {
            var names = new string[Backends.Length];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = Backends[i].IsAvailable ? Backends[i].DisplayName : $"{Backends[i].DisplayName} (planned)";
            }

            return names;
        }

        public static int IndexOf(ShooterUnityViewRenderBackend backend)
        {
            for (var i = 0; i < Backends.Length; i++)
            {
                if (Backends[i].Backend == backend)
                {
                    return i;
                }
            }

            return IndexOf(DefaultBackend);
        }

        public static ShooterUnityViewRenderBackend Normalize(ShooterUnityViewRenderBackend backend)
        {
            var descriptor = Get(backend);
            return descriptor.IsAvailable ? descriptor.Backend : DefaultBackend;
        }

        private static int ClampIndex(int index)
        {
            if (index < 0)
            {
                return 0;
            }

            return index >= Backends.Length ? Backends.Length - 1 : index;
        }
    }

    internal interface IUnityShooterViewSink : IShooterHostViewSink
    {
        ShooterUnityViewRenderBackend Backend { get; }

        void RebuildAll();
    }

    internal sealed class UnityShooterSwitchableViewSink : IUnityShooterViewSink
    {
        private IUnityShooterViewSink _inner;

        public UnityShooterSwitchableViewSink()
            : this(ShooterUnityViewRenderBackendCatalog.DefaultBackend)
        {
        }

        public UnityShooterSwitchableViewSink(ShooterUnityViewRenderBackend backend)
        {
            _inner = Create(ShooterUnityViewRenderBackendCatalog.Normalize(backend));
        }

        public ShooterUnityViewRenderBackend Backend => _inner.Backend;

        public void SetBackend(ShooterUnityViewRenderBackend backend)
        {
            var normalized = ShooterUnityViewRenderBackendCatalog.Normalize(backend);
            if (_inner.Backend == normalized)
            {
                return;
            }

            _inner.Clear();
            _inner = Create(normalized);
        }

        public void Render(in ShooterHostPresentationFrame frame)
        {
            _inner.Render(in frame);
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public void RebuildAll()
        {
            _inner.RebuildAll();
        }

        private static IUnityShooterViewSink Create(ShooterUnityViewRenderBackend backend)
        {
            return backend switch
            {
                ShooterUnityViewRenderBackend.GpuInstancedDotsReady => new UnityShooterGpuInstancedViewSink(),
                _ => new UnityShooterGameObjectViewSink()
            };
        }
    }

    internal sealed class UnityShooterGpuInstancedViewSink : IUnityShooterViewSink
    {
        private const int MaxInstancesPerDraw = 1023;
        private readonly ShooterSnapshotViewProjection _clientProjection = new();
        private readonly ShooterSnapshotViewProjection _authorityProjection = new();
        private readonly Matrix4x4[] _playerMatrices = new Matrix4x4[MaxInstancesPerDraw];
        private readonly Matrix4x4[] _bulletMatrices = new Matrix4x4[MaxInstancesPerDraw];
        private readonly Matrix4x4[] _enemyMatrices = new Matrix4x4[MaxInstancesPerDraw];
        private readonly MaterialPropertyBlock _properties = new();
        private Transform? _viewRoot;
        private Camera? _camera;
        private Light? _light;
        private HudBehaviour? _hudBehaviour;
        private Mesh? _playerMesh;
        private Mesh? _bulletMesh;
        private Mesh? _enemyMesh;
        private Material? _playerMaterial;
        private Material? _controlledPlayerMaterial;
        private Material? _bulletMaterial;
        private Material? _enemyMaterial;
        private Material? _authorityMaterial;
        private int _lastControlledPlayerId;
        private float _lastWorldScale = 1f;
        private int _lastPlayerCount;
        private int _lastBulletCount;
        private int _lastEnemyCount;
        private int _lastControlledHp = -1;
        private bool _hasHudData;
        private bool _hasAuthorityProjection;
        private ShooterCrossLayerDiagnostics _lastCrossLayerDiagnostics;
        private ulong _lastClientSequence;
        private ulong _lastAuthoritySequence;

        public ShooterUnityViewRenderBackend Backend => ShooterUnityViewRenderBackend.GpuInstancedDotsReady;

        public void Render(in ShooterHostPresentationFrame frame)
        {
            EnsureResources();
            _lastControlledPlayerId = frame.ControlledPlayerId;
            _lastWorldScale = frame.WorldScale;
            CaptureHudData(in frame);

            var clientBatch = frame.ClientBatch;
            if (clientBatch.Sequence != _lastClientSequence)
            {
                _clientProjection.Apply(in clientBatch);
                _lastClientSequence = clientBatch.Sequence;
            }

            DrawStore(_clientProjection.Store, frame.ControlledPlayerId, frame.WorldScale, isAuthority: false);

            if (frame.HasAuthorityBatch)
            {
                var authorityBatch = frame.AuthorityBatch;
                if (authorityBatch.Sequence != _lastAuthoritySequence)
                {
                    _authorityProjection.Apply(in authorityBatch);
                    _lastAuthoritySequence = authorityBatch.Sequence;
                }

                _hasAuthorityProjection = true;
                DrawStore(_authorityProjection.Store, frame.ControlledPlayerId, frame.WorldScale, isAuthority: true);
            }
            else
            {
                _authorityProjection.Clear();
                _hasAuthorityProjection = false;
            }
        }

        public void Clear()
        {
            _clientProjection.Clear();
            _authorityProjection.Clear();
            _hasAuthorityProjection = false;
            _hasHudData = false;
            _lastClientSequence = 0UL;
            _lastAuthoritySequence = 0UL;
            _lastPlayerCount = 0;
            _lastBulletCount = 0;
            _lastEnemyCount = 0;
            _lastControlledHp = -1;
            _lastCrossLayerDiagnostics = default;

            if (_viewRoot != null)
            {
                UnityEngine.Object.Destroy(_viewRoot.gameObject);
                _viewRoot = null;
                _camera = null;
                _light = null;
                _hudBehaviour = null;
            }

            DestroyRuntimeObject(ref _playerMesh);
            DestroyRuntimeObject(ref _bulletMesh);
            DestroyRuntimeObject(ref _enemyMesh);
            DestroyRuntimeObject(ref _playerMaterial);
            DestroyRuntimeObject(ref _controlledPlayerMaterial);
            DestroyRuntimeObject(ref _bulletMaterial);
            DestroyRuntimeObject(ref _enemyMaterial);
            DestroyRuntimeObject(ref _authorityMaterial);
        }

        public void RebuildAll()
        {
            EnsureResources();
        }

        private void CaptureHudData(in ShooterHostPresentationFrame frame)
        {
            CaptureHudCountsAndHealth(frame.ClientBatch, frame.ControlledPlayerId);
            _lastCrossLayerDiagnostics = frame.CrossLayerDiagnostics;
            _hasHudData = true;
        }

        private void DrawStore(ShooterViewEntityStore store, int controlledPlayerId, float worldScale, bool isAuthority)
        {
            var playerCount = 0;
            var bulletCount = 0;
            var enemyCount = 0;
            var hasControlledPlayer = false;
            var controlledPlayerMatrix = Matrix4x4.identity;

            foreach (var kvp in store.Entities)
            {
                var entity = kvp.Value;
                if (!entity.Alive || !store.TryGetTransform(entity.Key, out var transform))
                {
                    continue;
                }

                var kind = entity.Kind;
                var y = isAuthority ? 0.15f : 0f;
                var position = new Vector3(transform.X * worldScale, y, transform.Y * worldScale);
                var rotation = CreateFacingRotation(transform.FacingX, transform.FacingY);
                var matrix = Matrix4x4.TRS(position, rotation, ScaleFor(kind, isAuthority));

                if (!isAuthority && kind == ShooterViewEntityKind.Player && entity.EntityId == controlledPlayerId)
                {
                    controlledPlayerMatrix = matrix;
                    hasControlledPlayer = true;
                    continue;
                }

                AddMatrix(kind, isAuthority, in matrix, ref playerCount, ref bulletCount, ref enemyCount);
            }

            FlushIfNeeded(ShooterViewEntityKind.Enemy, _enemyMatrices, enemyCount, isAuthority);
            FlushIfNeeded(ShooterViewEntityKind.Bullet, _bulletMatrices, bulletCount, isAuthority);
            FlushIfNeeded(ShooterViewEntityKind.Player, _playerMatrices, playerCount, isAuthority);

            if (hasControlledPlayer)
            {
                _playerMatrices[0] = controlledPlayerMatrix;
                Flush(ShooterViewEntityKind.Player, _controlledPlayerMaterial, _playerMatrices, 1);
            }
        }

        private void AddMatrix(
            ShooterViewEntityKind kind,
            bool isAuthority,
            in Matrix4x4 matrix,
            ref int playerCount,
            ref int bulletCount,
            ref int enemyCount)
        {
            switch (kind)
            {
                case ShooterViewEntityKind.Player:
                    _playerMatrices[playerCount++] = matrix;
                    if (playerCount == MaxInstancesPerDraw)
                    {
                        Flush(ShooterViewEntityKind.Player, isAuthority, _playerMatrices, playerCount);
                        playerCount = 0;
                    }

                    break;
                case ShooterViewEntityKind.Bullet:
                    _bulletMatrices[bulletCount++] = matrix;
                    if (bulletCount == MaxInstancesPerDraw)
                    {
                        Flush(ShooterViewEntityKind.Bullet, isAuthority, _bulletMatrices, bulletCount);
                        bulletCount = 0;
                    }

                    break;
                case ShooterViewEntityKind.Enemy:
                    _enemyMatrices[enemyCount++] = matrix;
                    if (enemyCount == MaxInstancesPerDraw)
                    {
                        Flush(ShooterViewEntityKind.Enemy, isAuthority, _enemyMatrices, enemyCount);
                        enemyCount = 0;
                    }

                    break;
            }
        }

        private void FlushIfNeeded(ShooterViewEntityKind kind, Matrix4x4[] matrices, int count, bool isAuthority)
        {
            if (count > 0)
            {
                Flush(kind, isAuthority, matrices, count);
            }
        }

        private void Flush(ShooterViewEntityKind kind, bool isAuthority, Matrix4x4[] matrices, int count)
        {
            Flush(kind, MaterialFor(kind, isAuthority), matrices, count);
        }

        private void Flush(ShooterViewEntityKind kind, Material? material, Matrix4x4[] matrices, int count)
        {
            var mesh = MeshFor(kind);
            if (mesh == null || material == null)
            {
                return;
            }

            Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count, _properties, UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows: false);
        }

        private Mesh? MeshFor(ShooterViewEntityKind kind)
        {
            return kind switch
            {
                ShooterViewEntityKind.Player => _playerMesh,
                ShooterViewEntityKind.Bullet => _bulletMesh,
                ShooterViewEntityKind.Enemy => _enemyMesh,
                _ => null
            };
        }

        private Material? MaterialFor(ShooterViewEntityKind kind, bool isAuthority)
        {
            if (isAuthority)
            {
                return _authorityMaterial;
            }

            return kind switch
            {
                ShooterViewEntityKind.Player => _playerMaterial,
                ShooterViewEntityKind.Bullet => _bulletMaterial,
                ShooterViewEntityKind.Enemy => _enemyMaterial,
                _ => null
            };
        }

        private static Vector3 ScaleFor(ShooterViewEntityKind kind, bool isAuthority)
        {
            var authorityScale = isAuthority ? 0.85f : 1f;
            return kind switch
            {
                ShooterViewEntityKind.Player => new Vector3(0.75f, 1.1f, 0.75f) * authorityScale,
                ShooterViewEntityKind.Bullet => Vector3.one * (isAuthority ? 0.45f : 0.35f),
                ShooterViewEntityKind.Enemy => new Vector3(0.75f, 0.75f, 0.75f) * authorityScale,
                _ => Vector3.one
            };
        }

        private void EnsureResources()
        {
            if (_viewRoot != null)
            {
                return;
            }

            var root = new GameObject("ShooterPlayModeGpuInstancedViews");
            UnityEngine.Object.DontDestroyOnLoad(root);
            _viewRoot = root.transform;
            _hudBehaviour = root.AddComponent<HudBehaviour>();
            _hudBehaviour.Initialize(this);

            var cameraObject = new GameObject("ShooterGpuInstancedCamera");
            cameraObject.transform.SetParent(_viewRoot, false);
            cameraObject.transform.localPosition = new Vector3(4f, 18f, -12f);
            cameraObject.transform.localRotation = Quaternion.Euler(58f, 0f, 0f);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 18f;
            _camera.clearFlags = CameraClearFlags.Skybox;
            _camera.depth = 10f;

            var lightObject = new GameObject("ShooterGpuInstancedLight");
            lightObject.transform.SetParent(_viewRoot, false);
            lightObject.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.intensity = 1.2f;

            _playerMesh = CreatePrimitiveMesh(PrimitiveType.Capsule, "ShooterGpuPlayerMesh");
            _bulletMesh = CreatePrimitiveMesh(PrimitiveType.Sphere, "ShooterGpuBulletMesh");
            _enemyMesh = CreatePrimitiveMesh(PrimitiveType.Cube, "ShooterGpuEnemyMesh");
            _playerMaterial = CreateMaterial("ShooterGpuPlayerMaterial", Color.cyan);
            _controlledPlayerMaterial = CreateMaterial("ShooterGpuControlledPlayerMaterial", Color.green);
            _bulletMaterial = CreateMaterial("ShooterGpuBulletMaterial", Color.yellow);
            _enemyMaterial = CreateMaterial("ShooterGpuEnemyMaterial", Color.red);
            _authorityMaterial = CreateMaterial("ShooterGpuAuthorityMaterial", new Color(1f, 0.2f, 0.65f, 0.55f));
        }

        private void DrawHud()
        {
            if (!_hasHudData)
            {
                return;
            }

            var descriptor = ShooterUnityViewRenderBackendCatalog.Get(Backend);
            GUI.Box(new Rect(12f, 12f, 380f, 214f), "Shooter GPU Instanced HUD");
            GUILayout.BeginArea(new Rect(24f, 40f, 356f, 178f));
            GUILayout.Label($"Backend: {descriptor.DisplayName}");
            GUILayout.Label($"战场 玩家/子弹/怪物: {_lastPlayerCount}/{_lastBulletCount}/{_lastEnemyCount}");
            GUILayout.Label(_lastControlledHp >= 0 ? $"主控HP: {_lastControlledHp}" : "主控HP: N/A");
            GUILayout.Label($"客户端投影实体: {_clientProjection.Store.Entities.Count}");
            GUILayout.Label($"权威投影: {(_hasAuthorityProjection ? _authorityProjection.Store.Entities.Count.ToString() : "关闭")}");
            GUILayout.Label($"框架包/派发: {_lastCrossLayerDiagnostics.FrameworkPacketCount}/{_lastCrossLayerDiagnostics.FrameworkDispatchedSnapshotCount}");
            GUILayout.Label($"PureState 帧: apply={_lastCrossLayerDiagnostics.LastPureStateAppliedFrame} resync={_lastCrossLayerDiagnostics.LastPureStateResyncFrame}");
            GUILayout.EndArea();
        }

        private void CaptureHudCountsAndHealth(in ShooterSnapshotViewBatch batch, int controlledPlayerId)
        {
            var playerCount = 0;
            var bulletCount = 0;
            var enemyCount = 0;
            foreach (var entity in batch.EntityChanges)
            {
                if (!entity.Alive)
                {
                    continue;
                }

                switch (entity.Kind)
                {
                    case ShooterViewEntityKind.Player:
                        playerCount++;
                        break;
                    case ShooterViewEntityKind.Bullet:
                        bulletCount++;
                        break;
                    case ShooterViewEntityKind.Enemy:
                        enemyCount++;
                        break;
                }
            }

            _lastPlayerCount = playerCount;
            _lastBulletCount = bulletCount;
            _lastEnemyCount = enemyCount;
            _lastControlledHp = -1;
            if (controlledPlayerId <= 0)
            {
                return;
            }

            foreach (var change in batch.HealthChanges)
            {
                if (change.Key.Kind == ShooterViewEntityKind.Player && change.Key.EntityId == controlledPlayerId)
                {
                    _lastControlledHp = change.Hp;
                    return;
                }
            }
        }

        private static Quaternion CreateFacingRotation(float facingX, float facingY)
        {
            var direction = new Vector3(facingX, 0f, facingY);
            return direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
        }

        private static Mesh CreatePrimitiveMesh(PrimitiveType primitiveType, string name)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.hideFlags = HideFlags.HideAndDontSave;
            var source = primitive.GetComponent<MeshFilter>().sharedMesh;
            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = name;
            mesh.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.Destroy(primitive);
            return mesh;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = name,
                color = color,
                enableInstancing = true,
                hideFlags = HideFlags.HideAndDontSave
            };
            return material;
        }

        private static void DestroyRuntimeObject<T>(ref T? obj) where T : UnityEngine.Object
        {
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj);
                obj = null;
            }
        }

        private sealed class HudBehaviour : MonoBehaviour
        {
            private UnityShooterGpuInstancedViewSink? _sink;

            public void Initialize(UnityShooterGpuInstancedViewSink sink)
            {
                _sink = sink;
            }

            private void OnGUI()
            {
                _sink?.DrawHud();
            }
        }
    }
}
