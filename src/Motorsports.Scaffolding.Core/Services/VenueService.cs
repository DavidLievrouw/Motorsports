using System;
using System.Data;
using System.Threading.Tasks;
using Motorsports.Scaffolding.Core.Dapper;
using Motorsports.Scaffolding.Core.Models;

namespace Motorsports.Scaffolding.Core.Services {
  public interface IVenueService {
    Task UpdateVenue(string venueName, Venue newState);
  }

  public class VenueService : IVenueService {
    readonly IQueryExecutor _queryExecutor;

    public VenueService(IQueryExecutor queryExecutor) {
      _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
    }

    public async Task UpdateVenue(string venueName, Venue newState) {
      if (venueName == null) throw new ArgumentNullException(nameof(venueName));
      if (newState == null) throw new ArgumentNullException(nameof(newState));

      var numChanged = await _queryExecutor
        .NewQuery("UPDATE [dbo].[Venue] SET [Name]=@Name, [Country]=@Country WHERE [Name]=@id")
        .WithCommandType(CommandType.Text)
        .WithParameters(
          new {
            Id = venueName,
            Name = newState.Name,
            Country = newState.Country
          })
        .ExecuteAsync();

      if (numChanged < 0) throw new InvalidIdentifierException($"No Venue with id '{venueName}' was found.");
    }
  }
}