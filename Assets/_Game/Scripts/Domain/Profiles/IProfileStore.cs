namespace Worldtree.Domain.Profiles
{
    public enum ProfileLoadStatus { Loaded, NotFound, Corrupt, Invalid, UnsupportedVersion }

    public readonly struct ProfileLoadResult
    {
        public readonly ProfileLoadStatus Status;
        public readonly CharacterProfile Profile; // set only when Loaded
        public readonly string Error;             // null when Loaded
        public readonly bool FromBackup;          // the main save was unusable and the backup was loaded instead

        ProfileLoadResult(ProfileLoadStatus status, CharacterProfile profile, string error, bool fromBackup)
        {
            Status = status;
            Profile = profile;
            Error = error;
            FromBackup = fromBackup;
        }

        public static ProfileLoadResult Ok(CharacterProfile profile, bool fromBackup = false) =>
            new ProfileLoadResult(ProfileLoadStatus.Loaded, profile, null, fromBackup);

        public static ProfileLoadResult Fail(ProfileLoadStatus status, string error) =>
            new ProfileLoadResult(status, null, error, false);
    }

    /// <summary>Persistence port for the character profile; Infrastructure implements it (domain stays IO-free).</summary>
    public interface IProfileStore
    {
        /// <summary>Never throws for bad or missing data; the status says what went wrong.</summary>
        ProfileLoadResult Load();

        /// <summary>
        /// Writes atomically. Throws ArgumentException for an invalid profile, InvalidOperationException when the
        /// existing save is from a newer schema version, and IOException for disk errors.
        /// </summary>
        void Save(CharacterProfile profile);
    }
}
