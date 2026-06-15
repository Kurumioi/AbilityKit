using System;
using System.Collections.Generic;
using AbilityKit.Core.Markers;

namespace AbilityKit.Demo.Moba.Services
{
    public abstract class MobaMarkerRegistryBase<TService> : IMarkerRegistry
    {
        private readonly List<Entry> _entries;

        protected MobaMarkerRegistryBase(int initialCapacity)
        {
            _entries = new List<Entry>(initialCapacity);
        }

        public int Count => _entries.Count;

        public void Register(Type implType)
        {
        }

        protected bool TryRegister(int key, Type implType)
        {
            if (implType == null || !typeof(TService).IsAssignableFrom(implType)) return false;
            _entries.Add(new Entry(key, implType));
            return true;
        }

        protected List<Entry> GetEntriesSnapshot(bool sortByKey)
        {
            List<Entry> entries = new List<Entry>(_entries);
            if (sortByKey)
            {
                entries.Sort((a, b) => a.Key.CompareTo(b.Key));
            }

            return entries;
        }

        protected readonly struct Entry
        {
            public readonly int Key;
            public readonly Type ImplType;

            public Entry(int key, Type implType)
            {
                Key = key;
                ImplType = implType;
            }
        }
    }
}
