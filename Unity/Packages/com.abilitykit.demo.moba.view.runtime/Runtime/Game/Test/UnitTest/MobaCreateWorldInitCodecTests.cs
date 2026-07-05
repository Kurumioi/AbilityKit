using AbilityKit.Ability.Host;
using AbilityKit.Protocol.MemoryPack;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.CreateWorld;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    [TestFixture]
    public sealed class MobaCreateWorldInitCodecTests
    {
        [Test]
        public void SerializeRoundTrip_PreservesPlayerLoadoutIds()
        {
            MemoryPackWireSerializerInstaller.InstallAsCurrent();

            var localPlayerId = new PlayerId("p1");
            var players = new[]
            {
                new MobaPlayerLoadout(
                    playerId: localPlayerId,
                    teamId: 1,
                    heroId: 1001,
                    attributeTemplateId: 1001,
                    level: 1,
                    basicAttackSkillId: 1,
                    skillIds: new[] { 10010101, 10010201, 10010301 },
                    spawnIndex: 0,
                    unitSubType: 1,
                    mainType: 1,
                    hasSpawnPosition: 1,
                    spawnX: 0f,
                    spawnY: 0f,
                    spawnZ: 0f)
            };
            var spec = new MobaCreateWorldSpec(
                matchId: "roundtrip_match",
                mapId: 1,
                randomSeed: 123,
                tickRate: 30,
                inputDelayFrames: 0,
                players: players,
                gameplayId: 0);
            var payload = new MobaCreateWorldInitPayload(localPlayerId, in spec, opCode: 0, payload: null);

            var bytes = MobaCreateWorldInitCodec.Serialize(in payload);
            Assert.IsTrue(MobaCreateWorldInitCodec.TryDeserialize(bytes, out var decoded, out var error), error);

            var decodedPlayers = decoded.Spec.Players;
            Assert.IsNotNull(decodedPlayers);
            Assert.AreEqual(1, decodedPlayers.Length);
            Assert.AreEqual(1001, decodedPlayers[0].HeroId);
            Assert.AreEqual(1001, decodedPlayers[0].AttributeTemplateId);
            Assert.AreEqual(1, decodedPlayers[0].Level);
            Assert.AreEqual(1, decodedPlayers[0].BasicAttackSkillId);
            CollectionAssert.AreEqual(new[] { 10010101, 10010201, 10010301 }, decodedPlayers[0].SkillIds);
        }
    }
}
