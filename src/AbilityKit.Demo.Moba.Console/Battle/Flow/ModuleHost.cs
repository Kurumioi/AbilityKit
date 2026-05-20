using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// Module host
    /// Manages module lifecycle: Attach/Detach/Tick/Rebind
    /// </summary>
    public sealed class ModuleHost<TContext, TModule> : IDisposable where TModule : class, IGameModule<TContext> where TContext : class
    {
        private readonly List<TModule> _modules = new();
        private TContext _context;
        private bool _isAttached;

        /// <summary>
        /// Add module
        /// </summary>
        public void Add(TModule module)
        {
            if (module == null) return;
            _modules.Add(module);

            if (_isAttached && module is IGameModule<TContext> gm)
            {
                gm.OnAttach(_context);
            }
        }

        /// <summary>
        /// Attach all modules
        /// </summary>
        public void Attach(TContext context)
        {
            if (_isAttached) return;
            _isAttached = true;
            _context = context;

            foreach (var module in _modules)
            {
                try
                {
                    module.OnAttach(context);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[ModuleHost] OnAttach failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Detach all modules (reverse order)
        /// </summary>
        public void Detach(TContext context)
        {
            if (!_isAttached) return;
            _isAttached = false;

            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                try
                {
                    _modules[i].OnDetach(context);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[ModuleHost] OnDetach failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Tick all modules
        /// </summary>
        public void Tick(TContext context, float deltaTime)
        {
            foreach (var module in _modules)
            {
                if (module is IGameModuleTick<TContext> tickModule)
                {
                    try
                    {
                        tickModule.Tick(context, deltaTime);
                    }
                    catch (Exception ex)
                    {
                        Platform.Log.Error($"[ModuleHost] Tick failed: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Rebind all modules
        /// </summary>
        public void RebindAll(TContext context)
        {
            foreach (var module in _modules)
            {
                if (module is IGameModuleRebind<TContext> rebindModule)
                {
                    try
                    {
                        rebindModule.Rebind(context);
                    }
                    catch (Exception ex)
                    {
                        Platform.Log.Error($"[ModuleHost] Rebind failed: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Get module count
        /// </summary>
        public int ModuleCount => _modules.Count;

        public void Dispose()
        {
            Detach(default);
            _modules.Clear();
        }
    }
}
