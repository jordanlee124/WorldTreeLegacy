using Worldtree.Domain.Ids;

namespace Worldtree.Domain.Profiles
{
    /// <summary>Persistent character state. Plain C#; the on-disk format and its version live in Infrastructure.</summary>
    public sealed class CharacterProfile
    {
        public const int MaxNameLength = 16;
        public const int MaxLevel = 300; // GAME_DESIGN level bands end at 300

        public string Name { get; }
        public StableId ClassId { get; }
        public int Level { get; }
        public long Experience { get; }
        public long Lumen { get; }
        public StableId RegionId { get; }

        public CharacterProfile(string name, StableId classId, int level, long experience, long lumen, StableId regionId)
        {
            Name = name;
            ClassId = classId;
            Level = level;
            Experience = experience;
            Lumen = lumen;
            RegionId = regionId;
        }

        /// <summary>Returns the first problem found, or null when the profile is usable.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(Name)) return nameof(Name) + " must not be empty";
            if (Name.Length > MaxNameLength) return nameof(Name) + " must be at most " + MaxNameLength + " characters";
            if (Name.Trim() != Name) return nameof(Name) + " must not start or end with whitespace";
            foreach (var ch in Name)
                if (char.IsControl(ch)) return nameof(Name) + " must not contain control characters";
            if (ClassId.IsEmpty) return nameof(ClassId) + " is missing";
            if (Level < 1 || Level > MaxLevel) return nameof(Level) + " must be 1.." + MaxLevel;
            if (Experience < 0) return nameof(Experience) + " must be >= 0";
            if (Lumen < 0) return nameof(Lumen) + " must be >= 0";
            if (RegionId.IsEmpty) return nameof(RegionId) + " is missing";
            return null;
        }
    }
}
