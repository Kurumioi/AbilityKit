using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// Module host for managing sub-modules within a Feature
    /// 对齐 Unity ModuleHost
    /// </summary>
    public sealed class ModuleHost<TContext, TModule> : IDisposable
        where TContext : class
        where TModule : class
    {
        private readonly List<TModule> _modules = new();
        private readonly List<TModule> _sortedModules = new();
        private readonly Dictionary<string, TModule> _moduleById = new();
        private bool _attached;
        private bool _sorted;
        private TContext? _context;

        /// <summary>
        /// Add a module to the host
        /// </summary>
        public ModuleHost<TContext, TModule> Add(TModule module)
        {
            if (module == null) return this;

            var moduleId = module is IModuleId id ? id.Id : module.GetType().Name;
            if (_moduleById.ContainsKey(moduleId))
            {
                Platform.Log.Warn($"[ModuleHost] Duplicate module Id: {moduleId}");
                return this;
            }
            _moduleById[moduleId] = module;

            _modules.Add(module);
            _sorted = false;
            return this;
        }

        /// <summary>
        /// Add multiple modules
        /// </summary>
        public ModuleHost<TContext, TModule> AddRange(IEnumerable<TModule> modules)
        {
            foreach (var m in modules)
            {
                Add(m);
            }
            return this;
        }

        /// <summary>
        /// Get a module by Id
        /// </summary>
        public TModule? Get(string moduleId)
        {
            return _moduleById.TryGetValue(moduleId, out var m) ? m : null;
        }

        /// <summary>
        /// Check if a module exists
        /// </summary>
        public bool Has(string moduleId) => _moduleById.ContainsKey(moduleId);

        /// <summary>
        /// Try to sort modules by dependencies
        /// </summary>
        public bool TrySort()
        {
            if (_sorted) return true;

            _sortedModules.Clear();
            var visited = new HashSet<string>();

            foreach (var module in _modules)
            {
                if (!Visit(module, _sortedModules, visited))
                {
                    var moduleId = module is IModuleId id ? id.Id : module.GetType().Name;
                    Platform.Log.Error($"[ModuleHost] Circular dependency detected involving: {moduleId}");
                    return false;
                }
            }

            _sorted = true;
            return true;
        }

        private bool Visit(TModule module, List<TModule> result, HashSet<string> visited)
        {
            var moduleId = module is IModuleId id ? id.Id : module.GetType().Name;

            if (visited.Contains(moduleId)) return true;

            if (module is IModuleDependencies deps && deps.Dependencies != null)
            {
                foreach (var depId in deps.Dependencies)
                {
                    if (string.IsNullOrEmpty(depId)) continue;
                    if (!_moduleById.TryGetValue(depId, out var dep))
                    {
                        Platform.Log.Error($"[ModuleHost] Module '{moduleId}' depends on missing: {depId}");
                        return false;
                    }
                    if (!Visit(dep, result, visited)) return false;
                }
            }

            visited.Add(moduleId);
            result.Add(module);
            return true;
        }

        /// <summary>
        /// Attach all modules to the context
        /// </summary>
        public void Attach(in TContext context)
        {
            if (_attached)
            {
                Platform.Log.Warn("[ModuleHost] Already attached");
                return;
            }

            if (!TrySort())
            {
                Platform.Log.Error("[ModuleHost] Failed to sort modules, cannot attach");
                return;
            }

            _context = context;

            foreach (var module in _sortedModules)
            {
                if (module is IGameModule<TContext> gameModule)
                {
                    try
                    {
                        gameModule.OnAttach(context);
                        var moduleId = module is IModuleId id ? id.Id : module.GetType().Name;
                        Platform.Log.Trace($"[ModuleHost] Attached: {moduleId}");
                    }
                    catch (Exception ex)
                    {
                        var moduleId = module is IModuleId id ? id.Id : module.GetType().Name;
                        Platform.Log.Error($"[ModuleHost] Attach failed for '{moduleId}': {ex.Message}");
                    }
                }
            }

            _attached = true;
        }

        /// <summary>
        /// Detach all modules from the context (in reverse order)
        /// </summary>
        public void Detach(in TContext context)
        {
            if (!_attached)
            {
                Platform.Log.Warn("[ModuleHost] Not attached");
                return;
            }

            for (int i = _sortedModules.Count - 1; i >= 0; i--)
            {
                var module = _sortedModules[i];
                if (module is IGameModule<TContext> gameModule)
                {
                    try
                    {
                        gameModule.OnDetach(context);
                        var moduleId = module is IModuleId id ? id.Id : module.GetType().Name;
                        Platform.Log.Trace($"[ModuleHost] Detached: {moduleId}");
                    }
                    catch (Exception ex)
                    {
                        var moduleId = module is IModuleId id ? id.Id : module.GetType().Name;
                        Platform.Log.Error($"[ModuleHost] Detach failed for '{moduleId}': {ex.Message}");
                    }
                }
            }

            _attached = false;
            _context = null;
        }

        /// <summary>
        /// Tick all modules that support tick
        /// </summary>
        public void Tick(in TContext context, float deltaTime)
        {
            if (!_attached || _context == null) return;

            foreach (var module in _sortedModules)
            {
                if (module is IGameModuleTick<TContext> tick)
                {
                    try
                    {
                        tick.Tick(context, deltaTime);
                    }
                    catch (Exception ex)
                    {
                        var moduleId = module is IModuleId id ? id.Id : module.GetType().Name;
                        Platform.Log.Error($"[ModuleHost] Tick failed for '{moduleId}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Rebind all modules
        /// </summary>
        public void RebindAll(in TContext context)
        {
            if (!_attached) return;

            foreach (var module in _sortedModules)
            {
                if (module is IGameModuleRebind<TContext> rebind)
                {
                    try
                    {
                        rebind.Rebind(context);
                        var moduleId = module is IModuleId id ? id.Id : module.GetType().Name;
                        Platform.Log.Trace($"[ModuleHost] Rebind: {moduleId}");
                    }
                    catch (Exception ex)
                    {
                        var moduleId = module is IModuleId id ? id.Id : module.GetType().Name;
                        Platform.Log.Error($"[ModuleHost] Rebind failed for '{moduleId}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Number of modules
        /// </summary>
        public int Count => _modules.Count;

        /// <summary>
        /// Whether the host is attached
        /// </summary>
        public bool IsAttached => _attached;

        public void Dispose()
        {
            if (_attached && _context != null)
            {
                Detach(_context);
            }
            _modules.Clear();
            _sortedModules.Clear();
            _moduleById.Clear();
        }
    }
}
