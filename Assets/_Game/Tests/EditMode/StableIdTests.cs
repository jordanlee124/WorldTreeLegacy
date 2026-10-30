using System;
using NUnit.Framework;
using Worldtree.Domain.Ids;

namespace Worldtree.Tests
{
    public class StableIdTests
    {
        [TestCase("class.ironwarden")]
        [TestCase("region.rootbound_haven")]
        [TestCase("boss.thornroot_sentinel.hard")]
        [TestCase("item.ring_2")]
        public void Parses_WellFormedIds(string text)
        {
            Assert.IsTrue(StableId.TryParse(text, out var id));
            Assert.AreEqual(text, id.Value);
            Assert.IsFalse(id.IsEmpty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("ironwarden")]          // no kind prefix
        [TestCase("Class.ironwarden")]    // uppercase
        [TestCase("class.iron warden")]
        [TestCase("class.iron-warden")]
        [TestCase("class.")]
        [TestCase(".ironwarden")]
        [TestCase("class..ironwarden")]
        [TestCase("class.1st")]           // part must start with a letter
        [TestCase("class.ironwarden\n")]  // trailing newline must not slip past the end anchor
        [TestCase(" class.ironwarden")]
        public void Rejects_MalformedIds(string text)
        {
            Assert.IsFalse(StableId.TryParse(text, out var id));
            Assert.IsTrue(id.IsEmpty);
        }

        [Test]
        public void Rejects_TooLong()
        {
            var text = "class." + new string('a', StableId.MaxLength);
            Assert.IsFalse(StableId.TryParse(text, out _));
        }

        [Test]
        public void Parse_ThrowsOnMalformed() => Assert.Throws<FormatException>(() => StableId.Parse("Bad Id"));

        [Test]
        public void Equality_IsOrdinalByValue()
        {
            Assert.AreEqual(StableId.Parse("class.ironwarden"), StableId.Parse("class.ironwarden"));
            Assert.IsTrue(StableId.Parse("class.ironwarden") == StableId.Parse("class.ironwarden"));
            Assert.IsTrue(StableId.Parse("class.ironwarden") != StableId.Parse("class.starweaver"));
            Assert.AreEqual(StableId.Parse("class.ironwarden").GetHashCode(), StableId.Parse("class.ironwarden").GetHashCode());
        }

        [Test]
        public void Default_IsEmpty() => Assert.IsTrue(default(StableId).IsEmpty);
    }
}
