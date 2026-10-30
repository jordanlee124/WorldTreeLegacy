using System;
using System.IO;
using NUnit.Framework;
using Worldtree.Domain.Profiles;
using Worldtree.Infrastructure.Saves;

namespace Worldtree.Tests
{
    /// <summary>Real files in a throwaway temp directory; no scenes or assets involved.</summary>
    public class JsonProfileStoreTests
    {
        string dir;
        JsonProfileStore store;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "worldtree-save-tests-" + Guid.NewGuid().ToString("N"));
            store = new JsonProfileStore(dir); // directory is created by the first save
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        static string Json(int schemaVersion = 1, string name = "\"Aren\"", string classId = "\"class.ironwarden\"",
            string level = "12", string lumen = "500", string regionId = "\"region.rootbound_haven\"") =>
            "{\"schemaVersion\":" + schemaVersion + ",\"name\":" + name + ",\"classId\":" + classId + ",\"level\":" + level +
            ",\"experience\":40,\"lumen\":" + lumen + ",\"regionId\":" + regionId + "}";

        void WriteMain(string text)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(store.SavePath, text);
        }

        [Test]
        public void SaveThenLoad_RoundTripsEveryField()
        {
            var saved = CharacterProfileTests.Valid(name: "Aren Vael", level: 30, xp: 9_876_543_210L, lumen: 3_000_000_000L);
            store.Save(saved);

            var result = store.Load();
            Assert.AreEqual(ProfileLoadStatus.Loaded, result.Status, result.Error);
            Assert.IsFalse(result.FromBackup);
            var p = result.Profile;
            Assert.AreEqual(saved.Name, p.Name);
            Assert.AreEqual(saved.ClassId, p.ClassId);
            Assert.AreEqual(saved.Level, p.Level);
            Assert.AreEqual(saved.Experience, p.Experience);
            Assert.AreEqual(saved.Lumen, p.Lumen);
            Assert.AreEqual(saved.RegionId, p.RegionId);
        }

        [Test]
        public void Save_WritesVersionedJson_AndLeavesNoTempFile()
        {
            store.Save(CharacterProfileTests.Valid());
            var text = File.ReadAllText(store.SavePath);
            StringAssert.Contains("\"schemaVersion\": " + JsonProfileStore.CurrentSchemaVersion, text);
            StringAssert.Contains("\"classId\": \"class.ironwarden\"", text);
            Assert.IsFalse(File.Exists(store.SavePath + ".tmp"));
            Assert.IsFalse(File.Exists(store.BackupPath), "first save has nothing to back up");
        }

        [Test]
        public void SecondSave_KeepsThePreviousAsBackup()
        {
            store.Save(CharacterProfileTests.Valid(level: 5));
            store.Save(CharacterProfileTests.Valid(level: 6));
            Assert.AreEqual(6, store.Load().Profile.Level);
            StringAssert.Contains("\"level\": 5", File.ReadAllText(store.BackupPath));
            Assert.IsFalse(File.Exists(store.SavePath + ".tmp"));
        }

        [Test]
        public void Load_NoSave_IsNotFound()
        {
            var result = store.Load();
            Assert.AreEqual(ProfileLoadStatus.NotFound, result.Status);
            Assert.IsNull(result.Profile);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not json")]
        [TestCase("{\"schemaVersion\":1,\"name\":\"Ar")] // truncated mid-write
        [TestCase("[1,2,3]")]
        [TestCase("null")]
        [TestCase("{\"schemaVersion\":1,\"name\":\"Aren\",\"level\":\"high\"}")]
        public void Load_UnreadableOrIncomplete_IsCorruptOrInvalid_NeverThrows(string text)
        {
            WriteMain(text);
            var result = store.Load();
            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.Corrupt).Or.EqualTo(ProfileLoadStatus.Invalid), result.Error);
            Assert.IsNull(result.Profile);
            Assert.IsNotEmpty(result.Error);
        }

        [Test]
        public void Load_ValidJson_IsLoaded() // guards the helper the invalid cases below rely on
        {
            WriteMain(Json());
            Assert.AreEqual(ProfileLoadStatus.Loaded, store.Load().Status);
        }

        [TestCase("level", "0", "Level")]
        [TestCase("level", "301", "Level")]
        [TestCase("lumen", "-5", "Lumen")]
        [TestCase("name", "\"\"", "Name")]
        [TestCase("name", "\"ABCDEFGHIJKLMNOPQ\"", "Name")]
        [TestCase("classId", "\"Ironwarden\"", "classId")]
        [TestCase("classId", "null", "classId")]
        [TestCase("regionId", "\"rootbound haven\"", "regionId")]
        public void Load_BadField_IsInvalid_WithReason(string field, string value, string expected)
        {
            WriteMain(field == "level" ? Json(level: value)
                : field == "lumen" ? Json(lumen: value)
                : field == "name" ? Json(name: value)
                : field == "classId" ? Json(classId: value)
                : Json(regionId: value));
            var result = store.Load();
            Assert.AreEqual(ProfileLoadStatus.Invalid, result.Status);
            StringAssert.Contains(expected, result.Error);
        }

        [Test]
        public void Load_MissingSchemaVersion_IsInvalid()
        {
            WriteMain("{\"name\":\"Aren\",\"classId\":\"class.ironwarden\",\"level\":1,\"regionId\":\"region.rootbound_haven\"}");
            var result = store.Load();
            Assert.AreEqual(ProfileLoadStatus.Invalid, result.Status);
            StringAssert.Contains("schemaVersion", result.Error);
        }

        [Test]
        public void Load_UnknownExtraFields_AreIgnored()
        {
            WriteMain(Json().TrimEnd('}') + ",\"futureField\":42}");
            Assert.AreEqual(ProfileLoadStatus.Loaded, store.Load().Status);
        }

        [Test]
        public void Load_OversizedFile_IsCorrupt()
        {
            WriteMain(Json().TrimEnd('}') + ",\"pad\":\"" + new string('x', (int)JsonProfileStore.MaxFileBytes) + "\"}");
            Assert.AreEqual(ProfileLoadStatus.Corrupt, store.Load().Status);
        }

        [Test]
        public void Load_CorruptMain_FallsBackToBackup()
        {
            store.Save(CharacterProfileTests.Valid(level: 7));
            store.Save(CharacterProfileTests.Valid(level: 8));
            File.WriteAllText(store.SavePath, "{\"schemaVersion\":1,\"na"); // simulate a damaged main save
            var result = store.Load();
            Assert.AreEqual(ProfileLoadStatus.Loaded, result.Status);
            Assert.IsTrue(result.FromBackup);
            Assert.AreEqual(7, result.Profile.Level);
        }

        [Test]
        public void Load_CorruptMain_AndCorruptBackup_ReportsMainError()
        {
            WriteMain("garbage");
            File.WriteAllText(store.BackupPath, "also garbage");
            Assert.AreEqual(ProfileLoadStatus.Corrupt, store.Load().Status);
        }

        [Test]
        public void NewerSchema_IsUnsupported_DoesNotUseBackup_AndIsNeverOverwritten()
        {
            store.Save(CharacterProfileTests.Valid(level: 7));
            store.Save(CharacterProfileTests.Valid(level: 8)); // creates a valid backup
            var newer = Json(schemaVersion: JsonProfileStore.CurrentSchemaVersion + 1);
            File.WriteAllText(store.SavePath, newer);

            var result = store.Load();
            Assert.AreEqual(ProfileLoadStatus.UnsupportedVersion, result.Status);
            Assert.IsFalse(result.FromBackup);
            Assert.Throws<InvalidOperationException>(() => store.Save(CharacterProfileTests.Valid()));
            Assert.AreEqual(newer, File.ReadAllText(store.SavePath));
        }

        [Test]
        public void Save_InvalidProfile_Throws_AndWritesNothing()
        {
            Assert.Throws<ArgumentException>(() => store.Save(CharacterProfileTests.Valid(level: 0)));
            Assert.Throws<ArgumentNullException>(() => store.Save(null));
            Assert.IsFalse(File.Exists(store.SavePath));
        }

        [Test]
        public void Save_OverInvalidMain_Succeeds()
        {
            WriteMain("garbage");
            store.Save(CharacterProfileTests.Valid(level: 9));
            Assert.AreEqual(9, store.Load().Profile.Level);
        }

        [Test]
        public void Constructor_RejectsMissingDirectory() => Assert.Throws<ArgumentException>(() => new JsonProfileStore(""));
    }
}
