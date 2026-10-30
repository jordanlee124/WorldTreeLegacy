using System;
using System.Text.RegularExpressions;

namespace Worldtree.Domain.Ids
{
    /// <summary>
    /// Stable content ID, a kind prefix plus a name: <c>class.ironwarden</c>, <c>region.rootbound_haven</c>.
    /// Lowercase ASCII letters, digits and underscores, dot-separated, each part starting with a letter.
    /// Saves and data refer to content only through these, never by display name or asset path.
    /// </summary>
    public readonly struct StableId : IEquatable<StableId>
    {
        public const int MaxLength = 64;
        // \z, not $: $ also matches before a trailing newline.
        static readonly Regex Format = new Regex(@"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+\z", RegexOptions.CultureInvariant);

        public readonly string Value;

        StableId(string value) { Value = value; }

        public bool IsEmpty => Value == null;

        public static bool TryParse(string text, out StableId id)
        {
            id = default;
            if (text == null || text.Length > MaxLength || !Format.IsMatch(text)) return false;
            id = new StableId(text);
            return true;
        }

        public static StableId Parse(string text) =>
            TryParse(text, out var id) ? id : throw new FormatException("Invalid stable ID '" + text + "'");

        public bool Equals(StableId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StableId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? "";
        public static bool operator ==(StableId a, StableId b) => a.Equals(b);
        public static bool operator !=(StableId a, StableId b) => !a.Equals(b);
    }
}
