using System;
using System.IO;
using System.Text;
using UnityEngine;
using Worldtree.Domain.Ids;
using Worldtree.Domain.Profiles;

namespace Worldtree.Infrastructure.Saves
{
    /// <summary>
    /// One character profile as versioned JSON in a directory. A save goes to a temp file, is flushed to disk,
    /// then replaces the old save, which is kept as <c>.bak</c>; a crash mid-write never leaves a half-written save.
    /// A corrupt or invalid save falls back to the backup; a save from a newer schema is never overwritten.
    /// </summary>
    public sealed class JsonProfileStore : IProfileStore
    {
        public const int CurrentSchemaVersion = 1;
        public const long MaxFileBytes = 64 * 1024; // a profile is well under 1 KB; anything this big is not ours
        public const string FileName = "profile.json";

        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public string SavePath { get; }
        public string BackupPath => SavePath + ".bak";
        string TempPath => SavePath + ".tmp";

        public JsonProfileStore(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Save directory is required", nameof(directory));
            SavePath = Path.Combine(directory, FileName);
        }

        public ProfileLoadResult Load()
        {
            var main = Read(SavePath);
            // Only damaged saves fall back. A newer-version save must not: playing on the backup would later overwrite it.
            if (main.Status != ProfileLoadStatus.Corrupt && main.Status != ProfileLoadStatus.Invalid) return main;
            var backup = Read(BackupPath);
            return backup.Status == ProfileLoadStatus.Loaded ? ProfileLoadResult.Ok(backup.Profile, fromBackup: true) : main;
        }

        public void Save(CharacterProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var error = profile.Validate();
            if (error != null) throw new ArgumentException(error, nameof(profile));
            var existing = Read(SavePath);
            if (existing.Status == ProfileLoadStatus.UnsupportedVersion)
                throw new InvalidOperationException("Refusing to overwrite a save from a newer game version: " + existing.Error);

            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            var bytes = Utf8.GetBytes(JsonUtility.ToJson(ToData(profile), true));
            using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true); // on disk before the swap, not just in the OS cache
            }
            if (File.Exists(SavePath)) File.Replace(TempPath, SavePath, BackupPath);
            else File.Move(TempPath, SavePath);
        }

        static ProfileLoadResult Read(string path)
        {
            if (!File.Exists(path)) return ProfileLoadResult.Fail(ProfileLoadStatus.NotFound, path + " does not exist");

            ProfileSaveData data;
            try
            {
                if (new FileInfo(path).Length > MaxFileBytes)
                    return ProfileLoadResult.Fail(ProfileLoadStatus.Corrupt, "file is larger than " + MaxFileBytes + " bytes");
                data = JsonUtility.FromJson<ProfileSaveData>(File.ReadAllText(path, Utf8));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                return ProfileLoadResult.Fail(ProfileLoadStatus.Corrupt, e.Message);
            }
            if (data == null) return ProfileLoadResult.Fail(ProfileLoadStatus.Corrupt, "file has no JSON object");

            if (data.schemaVersion > CurrentSchemaVersion)
                return ProfileLoadResult.Fail(ProfileLoadStatus.UnsupportedVersion,
                    "schemaVersion " + data.schemaVersion + " is newer than " + CurrentSchemaVersion);
            if (data.schemaVersion < 1) return ProfileLoadResult.Fail(ProfileLoadStatus.Invalid, "schemaVersion is missing");
            // Migrations from older schema versions go here once version 2 exists.

            if (!StableId.TryParse(data.classId, out var classId))
                return ProfileLoadResult.Fail(ProfileLoadStatus.Invalid, "classId '" + data.classId + "' is not a stable ID");
            if (!StableId.TryParse(data.regionId, out var regionId))
                return ProfileLoadResult.Fail(ProfileLoadStatus.Invalid, "regionId '" + data.regionId + "' is not a stable ID");

            var profile = new CharacterProfile(data.name, classId, data.level, data.experience, data.lumen, regionId);
            var error = profile.Validate();
            return error == null ? ProfileLoadResult.Ok(profile) : ProfileLoadResult.Fail(ProfileLoadStatus.Invalid, error);
        }

        static ProfileSaveData ToData(CharacterProfile p) => new ProfileSaveData
        {
            schemaVersion = CurrentSchemaVersion,
            name = p.Name,
            classId = p.ClassId.Value,
            level = p.Level,
            experience = p.Experience,
            lumen = p.Lumen,
            regionId = p.RegionId.Value,
        };

        /// <summary>On-disk shape, schema version 1. Field names are the file format: rename only with a migration.</summary>
        [Serializable]
        sealed class ProfileSaveData
        {
            public int schemaVersion;
            public string name;
            public string classId;
            public int level;
            public long experience;
            public long lumen;
            public string regionId;
        }
    }
}
