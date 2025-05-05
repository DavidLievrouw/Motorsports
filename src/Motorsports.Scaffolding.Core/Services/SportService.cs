using System;
using System.Data;
using System.Threading.Tasks;
using Motorsports.Scaffolding.Core.Dapper;
using Motorsports.Scaffolding.Core.Models;

namespace Motorsports.Scaffolding.Core.Services {
  public interface ISportService {
    Task UpdateSport(string sportName, Sport newState);
  }

  public class SportService : ISportService {
    readonly IQueryExecutor _queryExecutor;

    public SportService(IQueryExecutor queryExecutor) {
      _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
    }

    public async Task UpdateSport(string sportName, Sport newState) {
      if (sportName == null) throw new ArgumentNullException(nameof(sportName));
      if (newState == null) throw new ArgumentNullException(nameof(newState));

      var numChanged = await _queryExecutor
        .NewQuery("UPDATE Sport SET Name=@Name, FullName=@FullName WHERE Name=@id")
        .WithCommandType(CommandType.Text)
        .WithParameters(
          new {
            Id = sportName,
            Name = newState.Name,
            FullName = newState.FullName
          })
          .ExecuteAsync();

      if (numChanged < 0) throw new InvalidIdentifierException($"No Sport with id '{sportName}' was found.");
    }
  }
}