using Worldtree.Domain.Profiles;

namespace Worldtree.Application.Profiles
{
    public readonly struct ProfileLoaded
    {
        public readonly CharacterProfile Profile;
        public readonly bool FromBackup;
        public ProfileLoaded(CharacterProfile profile, bool fromBackup) { Profile = profile; FromBackup = fromBackup; }
    }

    public readonly struct ProfileLoadFailed
    {
        public readonly ProfileLoadStatus Status;
        public readonly string Error;
        public ProfileLoadFailed(ProfileLoadStatus status, string error) { Status = status; Error = error; }
    }

    public readonly struct ProfileSaved
    {
        public readonly CharacterProfile Profile;
        public ProfileSaved(CharacterProfile profile) { Profile = profile; }
    }
}
