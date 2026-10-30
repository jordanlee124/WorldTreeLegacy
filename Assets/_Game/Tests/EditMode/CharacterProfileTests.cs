using NUnit.Framework;
using Worldtree.Domain.Ids;
using Worldtree.Domain.Profiles;

namespace Worldtree.Tests
{
    public class CharacterProfileTests
    {
        internal static readonly StableId Ironwarden = StableId.Parse("class.ironwarden");
        internal static readonly StableId RootboundHaven = StableId.Parse("region.rootbound_haven");

        internal static CharacterProfile Valid(string name = "Aren", int level = 12, long xp = 3400, long lumen = 125000) =>
            new CharacterProfile(name, Ironwarden, level, xp, lumen, RootboundHaven);

        [Test]
        public void ValidProfile_HasNoError() => Assert.IsNull(Valid().Validate());

        [Test]
        public void Boundaries_AreAccepted()
        {
            Assert.IsNull(Valid(level: 1, xp: 0, lumen: 0).Validate());
            Assert.IsNull(Valid(level: CharacterProfile.MaxLevel).Validate());
            Assert.IsNull(Valid(name: new string('a', CharacterProfile.MaxNameLength)).Validate());
        }

        [TestCase(null, "Name")]
        [TestCase("", "Name")]
        [TestCase("   ", "Name")]
        [TestCase(" Aren", "whitespace")]
        [TestCase("Aren\t", "whitespace")]
        [TestCase("Ar\u0000en", "control")]
        [TestCase("ABCDEFGHIJKLMNOPQ", "at most")] // 17 characters
        public void BadName_IsReported(string name, string expected) =>
            StringAssert.Contains(expected, Valid(name: name).Validate());

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(CharacterProfile.MaxLevel + 1)]
        public void LevelOutOfRange_IsReported(int level) => StringAssert.Contains("Level", Valid(level: level).Validate());

        [Test]
        public void NegativeCurrencies_AreReported()
        {
            StringAssert.Contains("Experience", Valid(xp: -1).Validate());
            StringAssert.Contains("Lumen", Valid(lumen: -1).Validate());
        }

        [Test]
        public void MissingIds_AreReported()
        {
            StringAssert.Contains("ClassId", new CharacterProfile("Aren", default, 1, 0, 0, RootboundHaven).Validate());
            StringAssert.Contains("RegionId", new CharacterProfile("Aren", Ironwarden, 1, 0, 0, default).Validate());
        }
    }
}
