using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class GameEntryLifecycleTests
    {
        [TearDown]
        public void TearDown()
        {
            if (GameEntry.IsInitialized)
            {
                Object.DestroyImmediate(GameEntry.Instance.gameObject);
            }
        }

        [Test]
        public void GameEntry_AwakeAttachesBootstrapModuleAndOnDestroyDetachesIt()
        {
            var go = new GameObject("GameEntryLifecycleTests.GameEntry");
            var entry = go.AddComponent<GameEntry>();

            InvokePrivate(entry, "Awake");

            Assert.IsTrue(GameEntry.IsInitialized);
            Assert.IsTrue(entry.Root.IsValid);
            Assert.IsTrue(entry.TryGet(out GameManager gm));
            Assert.IsTrue(gm.IsInGame);
            Assert.IsTrue(entry.TryGetNode(1, out var systems));
            Assert.IsTrue(systems.IsValid);

            InvokePrivate(entry, "OnDestroy");
            Object.DestroyImmediate(go);

            Assert.IsFalse(GameEntry.IsInitialized);
            Assert.IsFalse(gm.IsInGame);
        }

        [Test]
        public void GameEntry_IsOnlyUnityLifecycleOwnerForEntryBootstrap()
        {
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(GameEntry)));
            Assert.IsFalse(typeof(MonoBehaviour).IsAssignableFrom(typeof(GameEntryBootstrap)));
            Assert.IsTrue(typeof(IGameEntryModule).IsAssignableFrom(typeof(GameEntryBootstrap)));
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method: {methodName}");
            method.Invoke(target, null);
        }
    }
}
