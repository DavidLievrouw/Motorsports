using System;
using System.ComponentModel;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class SeasonEntry {
    public int Season { get; set; }
    public int Team { get; set; }

    [DisplayName("Name during season")]
    public string Name { get; set; }

    [DisplayName("Season")]
    public Season RelatedSeason { get; set; }

    [DisplayName("Team")]
    public Team RelatedTeam { get; set; }

    public class SeasonEntryKey : IEquatable<SeasonEntryKey> {
      public int Season { get; set; }
      public int Team { get; set; }

      public bool Equals(SeasonEntryKey other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Season == other.Season && Team == other.Team;
      }

      public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        var other = obj as SeasonEntryKey;
        return other != null && Equals(other);
      }

      public override int GetHashCode() {
        unchecked {
          return (Season * 397) ^ Team;
        }
      }

      public static bool operator ==(SeasonEntryKey left, SeasonEntryKey right) {
        return Equals(left, right);
      }

      public static bool operator !=(SeasonEntryKey left, SeasonEntryKey right) {
        return !Equals(left, right);
      }
    }
  }
}