using System.Collections;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AbilityKit.Demo.Shooter.PlayMode.Tests
{
    public sealed class ShooterSynchronizationPlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator DefaultLaunchSpecUsesSharedSynchronizationContractInPlayMode()
        {
            yield return null;

            Assert.IsTrue(Application.isPlaying);

            ShooterRoomLaunchSpec spec = ShooterRoomLaunchSpec.CreateDefault("unity-playmode-smoke");
            Assert.AreEqual(ShooterSyncTemplateIds.PredictRollbackAuthority, spec.SyncTemplateId);
            Assert.AreEqual((int)NetworkSyncModel.PredictRollback, spec.SyncModel);
        }
    }
}
