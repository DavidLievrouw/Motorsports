using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Motorsports.Scaffolding.Core.Security {
  public class UserForAuthentication {
    public Guid Id { get; set; }
    public string Username { get; set; }
    public HashedPassword Password { get; set; }
    public bool ForceChangePassword { get; set; }
    public string Title { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }

    public IEnumerable<Claim> AsClaims() {
      if (Id == Guid.Empty) yield return new Claim(ClaimTypes.Sid, Guid.Empty.ToString());
      else yield return new Claim(ClaimTypes.Sid, Id.ToString());
      var name = string.Join(" ", new[] {Title, FirstName, LastName}.Where(s => s != null));
      yield return new Claim(ClaimTypes.Name, name);
      if (!string.IsNullOrEmpty(EmailAddress)) yield return new Claim(ClaimTypes.Email, EmailAddress);
    }

    public override bool Equals(object obj) {
      return Equals(obj as UserForAuthentication);
    }

    protected bool Equals(UserForAuthentication other) {
      if (ReferenceEquals(this, null)) return ReferenceEquals(other, null);
      if (ReferenceEquals(other, null)) return false;
      return Id.Equals(other.Id) &&
             Username.Equals(other.Username) &&
             Equals(Password, other.Password) &&
             ForceChangePassword == other.ForceChangePassword &&
             Title.Equals(other.Title) &&
             FirstName.Equals(other.FirstName) &&
             LastName.Equals(other.LastName) &&
             EmailAddress.Equals(other.EmailAddress);
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = Id.GetHashCode();
        hashCode = (hashCode * 397) ^ Username.GetHashCode();
        hashCode = (hashCode * 397) ^
                   (Password != null
                     ? Password.GetHashCode()
                     : 0);
        hashCode = (hashCode * 397) ^ ForceChangePassword.GetHashCode();
        hashCode = (hashCode * 397) ^ Title.GetHashCode();
        hashCode = (hashCode * 397) ^ FirstName.GetHashCode();
        hashCode = (hashCode * 397) ^ LastName.GetHashCode();
        hashCode = (hashCode * 397) ^ EmailAddress.GetHashCode();
        return hashCode;
      }
    }
  }
}