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
    Task<IEnumerable<RoundToAcquire>> GetRoundsToAcquire();
    Task<HomeDisplayModel> GetHomeDisplayModel();
  }

  public class HomeService : IHomeService {
    readonly MotorsportsContext _context;
    readonly IQueryExecutor _queryExecutor;
    readonly IRoundService _roundService;

    public HomeService(
      MotorsportsContext context,
      IQueryExecutor queryExecutor,
      IRoundService roundService) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
      _roundService = roundService ?? throw new ArgumentNullException(nameof(roundService));
    }

    public Task<IEnumerable<RoundToAcquire>> GetRoundsToAcquire() {
      return _queryExecutor.NewQuery(
          @"
        SELECT
          S.[Sport],
          R.[Id],
          R.[Number],
          R.[Date],
          R.[Name],
          R.[Venue]
        FROM
          [dbo].[Round] R
          INNER JOIN [dbo].[Season] S ON S.[Id] = R.[Season]
          INNER JOIN [dbo].[Status] ST ON ST.[Name] = R.[Status]
        WHERE
          ST.[Step] = 0 -- Scheduled but not ReadyToWatch
          AND R.[Date] <= @Today")
        .WithCommandType(CommandType.Text)
        .WithParameters(new { Today = DateTime.Now.Date })
        .ExecuteAsync<RoundToAcquire>();
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
              INNER JOIN [dbo].[Status] ST ON ST.[Name] = R.[Status]
            WHERE
	            ST.[Step] < 2 -- Scheduled or ReadyToWatch
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
      var roundsNextUpDisplayModels = new List<NextUpDisplayModel>();
      foreach (var nextUp in roundsNextUp) {
        var eventHistory = await _roundService.GetEventHistory(nextUp.Id);
        roundsNextUpDisplayModels.Add(new NextUpDisplayModel(nextUp, roundsNextUp, eventHistory));
      }

      var veryNextUp = roundsNextUpDisplayModels
        .FirstOrDefault(n => n.IsVeryNextUp);
      var allSeasons = await _context.Season
        .AsNoTracking()
        .Include(s => s.RelatedRounds)
        .AsNoTracking()
        .Select(s => new HomeDisplayModel.SeasonDisplayModelForHome(s))
        .ToListAsync();
      var roundsToAcquire = (await GetRoundsToAcquire()).ToList();
      return new HomeDisplayModel {
        VeryNextUp = veryNextUp,
        NextUp = roundsNextUpDisplayModels
          .Where(n => veryNextUp == null || n != veryNextUp)
          .ToList(),
        RoundsToAcquire = roundsToAcquire
          .Select(r => new RoundToAcquireDisplayModel(r))
          .OrderBy(r => r.Date)
          .ThenBy(r => r.Sport)
          .ToList(),
        LatestSeasons = allSeasons
          .GroupBy(s => s.Sport)
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