using System;
using System.Collections.Generic;

namespace AbilityKit.Core.Pooling
{
    /// <summary>
    /// Central registry for named pool scopes. Use it to isolate pool lifetimes by global, scene, battle, UI, or feature domain.
    /// </summary>
    public static class PoolRegistry
    {
        public const string GlobalScopeName = "Global";

        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, PoolScope> Scopes = new Dictionary<string, PoolScope>(StringComparer.Ordinal);
        private static readonly PoolScope GlobalScopeInstance = new PoolScope(GlobalScopeName, destroyOnDispose: false);

        static PoolRegistry()
        {
            Scopes[GlobalScopeName] = GlobalScopeInstance;
        }

        public static PoolScope Global => GlobalScopeInstance;

        public static PoolScope GetOrCreateScope(string name, bool destroyOnDispose = true)
        {
            name = NormalizeName(name);

            lock (SyncRoot)
            {
                if (Scopes.TryGetValue(name, out var scope) && scope != null && !scope.IsDisposed)
                {
                    return scope;
                }

                scope = name == GlobalScopeName
                    ? GlobalScopeInstance
                    : new PoolScope(name, destroyOnDispose);

                Scopes[name] = scope;
                return scope;
            }
        }

        public static bool TryGetScope(string name, out PoolScope scope)
        {
            name = NormalizeName(name);

            lock (SyncRoot)
            {
                if (Scopes.TryGetValue(name, out scope) && scope != null && !scope.IsDisposed)
                {
                    return true;
                }

                scope = null;
                return false;
            }
        }

        public static bool DestroyScope(string name, bool destroy = true)
        {
            name = NormalizeName(name);
            if (name == GlobalScopeName)
            {
                GlobalScopeInstance.Clear(destroy);
                return true;
            }

            PoolScope scope;
            lock (SyncRoot)
            {
                if (!Scopes.TryGetValue(name, out scope)) return false;
                Scopes.Remove(name);
            }

            scope?.Dispose(destroy);
            return true;
        }

        public static void ClearAll(bool destroy = false, bool includeGlobal = true)
        {
            List<PoolScope> scopes;
            lock (SyncRoot)
            {
                scopes = new List<PoolScope>(Scopes.Values);
                if (!includeGlobal)
                {
                    scopes.Remove(GlobalScopeInstance);
                }
            }

            for (var i = 0; i < scopes.Count; i++)
            {
                var scope = scopes[i];
                if (scope == null || scope.IsDisposed) continue;
                scope.Clear(destroy);
            }
        }

#if UNITY_EDITOR
        public static IReadOnlyList<PoolDebugSnapshot> GetDebugSnapshots(string scopeName = null)
        {
            if (!string.IsNullOrEmpty(scopeName))
            {
                return TryGetScope(scopeName, out var scope) ? scope.GetDebugSnapshots() : Array.Empty<PoolDebugSnapshot>();
            }

            var result = new List<PoolDebugSnapshot>();
            List<PoolScope> scopes;
            lock (SyncRoot)
            {
                scopes = new List<PoolScope>(Scopes.Values);
            }

            for (var i = 0; i < scopes.Count; i++)
            {
                var scope = scopes[i];
                if (scope == null || scope.IsDisposed) continue;
                result.AddRange(scope.GetDebugSnapshots());
            }

            return result;
        }
#endif

        private static string NormalizeName(string name)
        {
            return string.IsNullOrEmpty(name) ? GlobalScopeName : name;
        }
    }
}
