using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Features;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// Feature 上下文接口
    /// 提供 Feature 运行所需的上下文信息
    /// </summary>
    public interface IFeatureContext
    {
        int LastFrame { get; }
        double LogicTimeSeconds { get; }
        int LocalActorId { get; }
    }

    /// <summary>
    /// Feature 标识接口
    /// </summary>
    public interface IFeatureId
    {
        string Id { get; }
    }

    /// <summary>
    /// Feature 依赖接口
    /// </summary>
    public interface IFeatureDependencies
    {
        string[] Dependencies { get; }
    }

    /// <summary>
    /// Feature Tick 接口（可选实现）
    /// </summary>
    public interface IFeatureTick
    {
        void Tick(float deltaTime);
    }

    /// <summary>
    /// Feature 基础接口
    /// </summary>
    public interface IFeature : IFeatureId
    {
        void OnAttach(IFeatureContext ctx);
        void OnDetach(IFeatureContext ctx);
    }

    /// <summary>
    /// 特性上下文适配器
    /// 将 ConsoleBattleContext 适配为 IFeatureContext
    /// </summary>
    public sealed class FeatureContextAdapter : IFeatureContext
    {
        private readonly ConsoleBattleContext _battleContext;

        public ConsoleBattleContext? Context => _battleContext;
        public int LastFrame => _battleContext?.LastFrame ?? 0;
        public double LogicTimeSeconds => _battleContext?.LogicTimeSeconds ?? 0d;
        public int LocalActorId => _battleContext?.LocalActorId ?? 0;

        public FeatureContextAdapter(ConsoleBattleContext battleContext)
        {
            _battleContext = battleContext ?? throw new ArgumentNullException(nameof(battleContext));
        }

        public static FeatureContextAdapter Wrap(ConsoleBattleContext ctx) => new FeatureContextAdapter(ctx);
    }

    /// <summary>
    /// Feature 宿主
    /// 管理一组 Feature 的生命周期和依赖排序
    /// </summary>
    public sealed class FeatureHost : IDisposable
    {
        private readonly List<IFeature> _features = new();
        private readonly Dictionary<string, IFeature> _featureById = new();
        private readonly List<IFeature> _sortedFeatures = new();
        private bool _attached;
        private bool _sorted;
        private IFeatureContext? _context;

        /// <summary>
        /// 添加 Feature
        /// </summary>
        public FeatureHost Add(IFeature feature)
        {
            if (feature == null) return this;
            if (_featureById.ContainsKey(feature.Id))
            {
                Platform.Log.Warn($"[FeatureHost] Duplicate feature Id: {feature.Id}");
                return this;
            }
            _features.Add(feature);
            _featureById[feature.Id] = feature;
            _sorted = false;
            return this;
        }

        /// <summary>
        /// 批量添加 Features
        /// </summary>
        public FeatureHost AddRange(IEnumerable<IFeature> features)
        {
            foreach (var f in features)
            {
                Add(f);
            }
            return this;
        }

        /// <summary>
        /// 获取 Feature
        /// </summary>
        public T? Get<T>(string featureId) where T : class, IFeature
        {
            return _featureById.TryGetValue(featureId, out var f) ? f as T : null;
        }

        /// <summary>
        /// 检查是否包含 Feature
        /// </summary>
        public bool Has(string featureId) => _featureById.ContainsKey(featureId);

        /// <summary>
        /// 尝试排序（按依赖关系）
        /// </summary>
        public bool TrySort()
        {
            if (_sorted) return true;

            _sortedFeatures.Clear();
            var visited = new HashSet<string>();
            var idToFeature = new Dictionary<string, IFeature>(_featureById);

            foreach (var feature in _features)
            {
                if (!Visit(feature, idToFeature, _sortedFeatures, visited))
                {
                    Platform.Log.Error($"[FeatureHost] Circular dependency detected involving: {feature.Id}");
                    return false;
                }
            }

            _sorted = true;
            Platform.Log.System($"[FeatureHost] Sorted {_sortedFeatures.Count} features by dependencies");
            return true;
        }

        private bool Visit(
            IFeature feature,
            Dictionary<string, IFeature> idToFeature,
            List<IFeature> result,
            HashSet<string> visited)
        {
            if (visited.Contains(feature.Id)) return true;

            if (feature is IFeatureDependencies deps && deps.Dependencies != null)
            {
                foreach (var depId in deps.Dependencies)
                {
                    if (string.IsNullOrEmpty(depId)) continue;
                    if (!idToFeature.TryGetValue(depId, out var dep))
                    {
                        Platform.Log.Error($"[FeatureHost] Feature '{feature.Id}' depends on missing: {depId}");
                        return false;
                    }
                    if (!Visit(dep, idToFeature, result, visited)) return false;
                }
            }

            visited.Add(feature.Id);
            result.Add(feature);
            return true;
        }

        /// <summary>
        /// 附加所有 Feature
        /// </summary>
        public void Attach(IFeatureContext context)
        {
            if (_attached)
            {
                Platform.Log.Warn("[FeatureHost] Already attached");
                return;
            }

            if (!TrySort())
            {
                Platform.Log.Error("[FeatureHost] Failed to sort features, cannot attach");
                return;
            }

            _context = context;
            foreach (var feature in _sortedFeatures)
            {
                try
                {
                    feature.OnAttach(context);
                    Platform.Log.Trace($"[FeatureHost] Attached: {feature.Id}");
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[FeatureHost] Attach failed for '{feature.Id}': {ex.Message}");
                }
            }
            _attached = true;
        }

        /// <summary>
        /// 分离所有 Feature（反向顺序）
        /// </summary>
        public void Detach()
        {
            if (!_attached)
            {
                Platform.Log.Warn("[FeatureHost] Not attached");
                return;
            }

            for (int i = _sortedFeatures.Count - 1; i >= 0; i--)
            {
                var feature = _sortedFeatures[i];
                try
                {
                    feature.OnDetach(_context!);
                    Platform.Log.Trace($"[FeatureHost] Detached: {feature.Id}");
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[FeatureHost] Detach failed for '{feature.Id}': {ex.Message}");
                }
            }
            _attached = false;
            _context = null;
        }

        /// <summary>
        /// Tick 所有支持 Tick 的 Feature
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_attached)
            {
                return;
            }

            foreach (var feature in _sortedFeatures)
            {
                if (feature is IFeatureTick tick)
                {
                    try
                    {
                        tick.Tick(deltaTime);
                    }
                    catch (Exception ex)
                    {
                        Platform.Log.Error($"[FeatureHost] Tick failed for '{feature.Id}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 获取 Feature 数量
        /// </summary>
        public int Count => _features.Count;

        /// <summary>
        /// 是否已附加
        /// </summary>
        public bool IsAttached => _attached;

        /// <summary>
        /// 获取已排序的 Feature 列表
        /// </summary>
        public IReadOnlyList<IFeature> Features => _sortedFeatures;

        public void Dispose()
        {
            if (_attached) Detach();
            _features.Clear();
            _featureById.Clear();
            _sortedFeatures.Clear();
        }
    }

    /// <summary>
    /// FeatureHost 扩展方法
    /// </summary>
    public static class FeatureHostExtensions
    {
        /// <summary>
        /// 添加 Console SubFeature 到 FeatureHost
        /// 自动包装为 IFeature
        /// </summary>
        public static FeatureHost AddConsoleFeature(this FeatureHost host, IConsoleSubFeature feature)
        {
            if (feature != null)
            {
                host.Add(new SubFeatureAdapter(feature));
            }
            return host;
        }

        /// <summary>
        /// 附加到 ConsoleBattleContext
        /// </summary>
        public static void AttachTo(this FeatureHost host, ConsoleBattleContext context)
        {
            host.Attach(new FeatureContextAdapter(context));
        }
    }

    /// <summary>
    /// 适配器：将 IConsoleSubFeature 适配为 IFeature
    /// 允许现有实现复用
    /// </summary>
    public sealed class SubFeatureAdapter : IFeature, IFeatureTick, IFeatureDependencies
    {
        private readonly IConsoleSubFeature _inner;
        private ConsoleBattleContext? _context;

        public string Id => _inner.Id;
        public string[] Dependencies => _inner.Dependencies;

        public SubFeatureAdapter(IConsoleSubFeature inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public void OnAttach(IFeatureContext ctx)
        {
            if (ctx is FeatureContextAdapter adapter)
            {
                _context = adapter.Context;
                if (_context != null)
                {
                    _inner.OnAttach(_context);
                }
            }
        }

        public void OnDetach(IFeatureContext ctx)
        {
            if (_context != null)
            {
                _inner.OnDetach(_context);
                _context = null;
            }
        }

        public void Tick(float deltaTime)
        {
            if (_context != null)
            {
                _inner.Tick(_context, deltaTime);
            }
        }
    }
}
