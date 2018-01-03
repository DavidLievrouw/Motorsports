using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Motorsports.Scaffolding.Core.Dapper;
using Motorsports.Scaffolding.Core.Models;

namespace Motorsports.Scaffolding.Core.Services {
  public interface INextUpService {
    Task<IEnumerable<NextUp>> GetRoundsNextUp();
  }

  public class NextUpService : INextUpService {
    readonly IQueryExecutor _queryExecutor;

    public NextUpService(IQueryExecutor queryExecutor) {
      _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
    }

    public Task<IEnumerable<NextUp>> GetRoundsNextUp() {
      return _queryExecutor.NewQuery(@"
          ;WITH Partitioned AS (
            SELECT
              S.[Sport],
	            R.[Id],
	            R.[Number],
	            R.[Date],
	            R.[Name],
	            R.[Venue],
	            ROW_NUMBER() OVER(PARTITION BY S.[Sport] ORDER BY R.[Date]) AS seq
            FROM
	            [dbo].[Round] R
	            INNER JOIN [dbo].[Season] S ON S.[Id] = R.[Season]
            WHERE
	            R.[Status] = 'Scheduled'
          )
          SELECT 
            P.[Sport],
            P.[Id],
            P.[Number],
            P.[Date],
            P.[Name],
            P.[Venue]
          FROM Partitioned P 
          WHERE P.seq = 1")
        .WithCommandType(CommandType.Text)
        .ExecuteAsync<NextUp>();
    }
  }
}