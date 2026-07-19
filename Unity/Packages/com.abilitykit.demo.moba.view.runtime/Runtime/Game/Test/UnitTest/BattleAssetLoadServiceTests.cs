using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Game.Battle.Shared.Assets;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class BattleAssetManifestResolverTests
    {
        private static IBattleAssetManifestSource NewSnapshot(params ClientRoomPlayer[] players)
        {
            return new ClientRoomSnapshotAssetSource(new ClientRoomSnapshot
            {
                RoomId = "room-1",
                Phase = ClientRoomPhase.Loading,
                LaunchGeneration = 7L,
                LaunchManifestVersion = 3,
                LaunchManifestHash = "hash-abc",
                Players = players
            });
        }

        private static ClientRoomPlayer NewPlayer(int heroId, params int[] skillIds)
        {
            return new ClientRoomPlayer
            {
                AccountId = "acc-" + heroId,
                HeroId = heroId,
                SkillIds = skillIds
            };
        }

        [Test]
        public void SameSnapshot_ProducesSameManifest_Deterministic()
        {
            var snapshot = NewSnapshot(NewPlayer(1001, 100101), NewPlayer(1002, 100201));

            var m1 = BattleAssetManifestResolver.Resolve(snapshot);
            var m2 = BattleAssetManifestResolver.Resolve(snapshot);

            Assert.AreEqual(m1.Entries.Count, m2.Entries.Count);
            for (var i = 0; i < m1.Entries.Count; i++)
            {
                Assert.AreEqual(m1.Entries[i], m2.Entries[i], "Entry " + i + " differs");
            }
            Assert.AreEqual(3, m1.ManifestVersion);
            Assert.AreEqual("hash-abc", m1.ManifestHash);
            Assert.AreEqual(7L, m1.LaunchGeneration);
        }

        [Test]
        public void DifferentHeroId_ProducesDifferentEntries()
        {
            var m1 = BattleAssetManifestResolver.Resolve(NewSnapshot(NewPlayer(1001)));
            var m2 = BattleAssetManifestResolver.Resolve(NewSnapshot(NewPlayer(1002)));

            CollectionAssert.AreNotEqual(m1.Entries, m2.Entries);
            Assert.IsTrue(m1.Entries.Any(e => e.AssetKey == "character:1001"));
            Assert.IsTrue(m2.Entries.Any(e => e.AssetKey == "character:1002"));
        }

        [Test]
        public void Entries_AreSortedByAssetKey()
        {
            // 故意乱序输入 hero id
            var manifest = BattleAssetManifestResolver.Resolve(
                NewSnapshot(NewPlayer(2002), NewPlayer(1001)));

            var keys = manifest.Entries.Select(e => e.AssetKey).ToList();
            var sorted = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            CollectionAssert.AreEqual(sorted, keys);
        }

        [Test]
        public void EmptyPlayers_StillContainsFixedConfigEntries()
        {
            var manifest = BattleAssetManifestResolver.Resolve(NewSnapshot());

            Assert.IsTrue(manifest.Entries.Any(e => e.AssetKey == "config:skills"));
            Assert.IsTrue(manifest.Entries.Any(e => e.AssetKey == "config:characters"));
            Assert.IsTrue(manifest.Entries.Any(e => e.AssetKey == "config:projectiles"));
            Assert.IsTrue(manifest.Entries.Any(e => e.AssetKey == "map:classic"));
        }

        [Test]
        public void DuplicateHeroIds_AreDeduplicated()
        {
            var manifest = BattleAssetManifestResolver.Resolve(
                NewSnapshot(NewPlayer(1001), NewPlayer(1001)));

            var characterCount = manifest.Entries.Count(e => e.AssetKey == "character:1001");
            Assert.AreEqual(1, characterCount);
        }
    }

    public sealed class BattleAssetLoadServiceTests
    {
        private sealed class MockAssetSource : IBattleAssetSource
        {
            private readonly HashSet<string> _existing;

            public MockAssetSource(params string[] existing)
            {
                _existing = new HashSet<string>(existing);
            }

            public bool TryLoad(string path, out object asset)
            {
                if (_existing.Contains(path))
                {
                    asset = new object();
                    return true;
                }

                asset = null;
                return false;
            }
        }

        private sealed class ImmediateProgress<T> : IProgress<T>
        {
            private readonly Action<T> _report;

            public ImmediateProgress(Action<T> report)
            {
                _report = report ?? throw new ArgumentNullException(nameof(report));
            }

            public void Report(T value)
            {
                _report(value);
            }
        }

        private static BattleAssetManifest NewManifest(params BattleAssetEntry[] entries)
        {
            return new BattleAssetManifest(3, "hash-abc", 7L, entries);
        }

        private static BattleAssetEntry Entry(string key, string path)
        {
            return new BattleAssetEntry(path, key, BattleAssetKind.Generic);
        }

        [Test]
        public void AllAssetsExist_ReturnsSuccess()
        {
            var source = new MockAssetSource("a", "b", "c");
            var service = new BattleAssetLoadService(source);
            var manifest = NewManifest(Entry("k1", "a"), Entry("k2", "b"), Entry("k3", "c"));

            var result = service.LoadAsync(manifest).GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.Errors.Count);
            Assert.IsNotNull(result.Lease);
            Assert.IsTrue(result.Lease.IsActive);
        }

        [Test]
        public void MissingAsset_ReturnsFailureWithError()
        {
            var source = new MockAssetSource("a", "c");
            var service = new BattleAssetLoadService(source);
            var manifest = NewManifest(Entry("k1", "a"), Entry("k2", "b"), Entry("k3", "c"));

            var result = service.LoadAsync(manifest).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual("b", result.Errors[0].AssetPath);
            Assert.AreEqual("AssetNotFound", result.Errors[0].Reason);
            Assert.IsNull(result.Lease);
        }

        [Test]
        public void CancellationToken_ReturnsFailureWithCancelledReason()
        {
            var source = new MockAssetSource("a", "b");
            var service = new BattleAssetLoadService(source);
            var manifest = NewManifest(Entry("k1", "a"), Entry("k2", "b"));
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = service.LoadAsync(manifest, null, cts.Token).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Errors.Any(e => e.Reason == "Cancelled"));
        }

        [Test]
        public void RepeatLoadSameManifest_IsIdempotentSuccess()
        {
            var source = new MockAssetSource("a", "b");
            var service = new BattleAssetLoadService(source);
            var manifest = NewManifest(Entry("k1", "a"), Entry("k2", "b"));

            var r1 = service.LoadAsync(manifest).GetAwaiter().GetResult();
            var r2 = service.LoadAsync(manifest).GetAwaiter().GetResult();

            Assert.IsTrue(r1.Success);
            Assert.IsTrue(r2.Success);
        }

        [Test]
        public void ProgressCallback_IncrementsCorrectly()
        {
            var source = new MockAssetSource("a", "b", "c");
            var service = new BattleAssetLoadService(source);
            var manifest = NewManifest(Entry("k1", "a"), Entry("k2", "b"), Entry("k3", "c"));
            var reports = new List<BattleAssetLoadProgress>();
            var progress = new ImmediateProgress<BattleAssetLoadProgress>(p => reports.Add(p));

            service.LoadAsync(manifest, progress).GetAwaiter().GetResult();

            // 至少报告了每个条目的进度 + 最终完成
            Assert.GreaterOrEqual(reports.Count, manifest.Entries.Count);
            var last = reports[reports.Count - 1];
            Assert.AreEqual(3, last.LoadedCount);
            Assert.AreEqual(3, last.TotalCount);
            Assert.AreEqual(1f, last.Progress01, 0.0001f);
        }
    }

    public sealed class BattleAssetLeaseTests
    {
        [Test]
        public void Dispose_MarksLeaseInactive()
        {
            var lease = new BattleAssetLease(7L, new[] { "a", "b" });

            Assert.IsTrue(lease.IsActive);
            Assert.AreEqual(7L, lease.LaunchGeneration);

            lease.Dispose();

            Assert.IsFalse(lease.IsActive);
        }
    }
}
