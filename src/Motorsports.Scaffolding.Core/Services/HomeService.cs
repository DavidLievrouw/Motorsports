using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Dapper;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;

namespace Motorsports.Scaffolding.Core.Services {
  public interface IHomeService {
    Task<IEnumerable<NextUp>> GetRoundsNextUp();
    Task<HomeDisplayModel> GetHomeDisplayModel();
  }

  public class HomeService : IHomeService {
    readonly MotorsportsContext _context;
    readonly IQueryExecutor _queryExecutor;

    public HomeService(
      MotorsportsContext context,
      IQueryExecutor queryExecutor) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
    }

    public Task<IEnumerable<NextUp>> GetRoundsNextUp() {
      return _queryExecutor.NewQuery(
          @"
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

    public async Task<HomeDisplayModel> GetHomeDisplayModel() {
      var roundsNextUp = (await GetRoundsNextUp()).ToList();
      var allSeasons = await _context.Season
        .Include(s => s.RelatedSport)
        .Include(s => s.RelatedRounds)
        .Select(s => new HomeDisplayModel.SeasonDisplayModelForHome(s))
        .ToListAsync();
      return new HomeDisplayModel() {
        NextUpPerSport = roundsNextUp
          .OrderBy(n => n.Date)
          .Select(n => new NextUpDisplayModel(n))
          .ToList(),
        VeryNextUp = roundsNextUp
          .OrderBy(n => n.Date)
          .Select(n => new NextUpDisplayModel(n))
          .Where(n => n.Date.Date <= DateTime.Now.Date)
          .OrderBy(n => n.Date)
          .FirstOrDefault(),
        LatestSeasons = allSeasons
          .GroupBy(s => s.RelatedSport)
          .Select(
            group => new {
              Sport = group.Key,
              Seasons = group
                .OrderByDescending(s => s.StartDate ?? DateTime.MaxValue)
                .Take(2)
                .AsEnumerable()
            })
          .SelectMany(_ => _.Seasons)
          .OrderByDescending(s => s.StartDate ?? DateTime.MaxValue)
          .ToList()
      };
    }
  }
}