using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Motorsports.Scaffolding.Core.Dapper;

namespace Motorsports.Scaffolding.Core.Security {
  public interface IUserDataService {
    Task<UserForAuthentication> GetUserForAuthentication(string username);
  }

  public class UserDataService : IUserDataService {
    readonly IQueryExecutor _queryExecutor;

    public UserDataService(IQueryExecutor queryExecutor) {
      _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
    }

    public async Task<UserForAuthentication> GetUserForAuthentication(string username) {
      var query = @"
        SELECT [Id], [Username], [PasswordHash], [Salt], [Iterations], [Prf], [ForceChangePassword], [Title], [GivenName], [FamilyName], [EmailAddress]
        FROM [dbo].[User] 
        WHERE [Username] = @Username AND [IsDeleted] = 0";
      var results = await _queryExecutor.NewQuery(query)
        .WithParameters(new {Username = username})
        .ExecuteDynamicAsync();
      return results
        .Select(
          _ => new UserForAuthentication {
            Id = _.Id,
            Username = _.Username,
            Password = new HashedPassword(
              _.PasswordHash, 
              _.Salt, 
              _.Iterations, 
              (KeyDerivationPrf) Enum.Parse(typeof(KeyDerivationPrf), _.Prf)),
            ForceChangePassword = _.ForceChangePassword,
            Title = _.Title,
            FirstName = _.GivenName,
            LastName = _.FamilyName,
            EmailAddress = _.EmailAddress
          })
        .SingleOrDefault();
    }
  }
}