using System;
using System.Linq;
using System.Security.Claims;

namespace Motorsports.Scaffolding.Core.Security {
  public abstract class AuthenticateResponse { }

  public class AuthenticationSuccess : AuthenticateResponse {
    public AuthenticationSuccess(ClaimsPrincipal principal) {
      Principal = principal ?? throw new ArgumentNullException(nameof(principal));
    }

    public ClaimsPrincipal Principal { get; }
  }

  public class AuthenticationFailure<TFailureReason> : AuthenticateResponse where TFailureReason : struct {
    public AuthenticationFailure(params TFailureReason[] reasons) {
      FailureReasons = reasons ?? new TFailureReason[0];
    }

    public TFailureReason[] FailureReasons { get; }

    public override bool Equals(object obj) {
      return Equals(obj as AuthenticationFailure<TFailureReason>);
    }

    bool Equals(AuthenticationFailure<TFailureReason> other) {
      if (ReferenceEquals(this, null)) return ReferenceEquals(other, null);
      if (ReferenceEquals(other, null)) return false;
      return FailureReasons.SequenceEqual(other.FailureReasons);
    }

    public override int GetHashCode() {
      return FailureReasons?.GetHashCode() ?? 0;
    }
  }
}