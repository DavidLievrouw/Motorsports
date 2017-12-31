using System.Threading.Tasks;

namespace Motorsports.Scaffolding.Core.Security {
  public interface IAuthenticateUserService<in TCredentials> {
    Task<AuthenticateResponse> Authenticate(TCredentials credentials);
  }
}