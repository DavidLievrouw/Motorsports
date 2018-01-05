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
    Task<IEnumerable<EventHistoryItem>> GetEventHistory(string venue, string sport);
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

    public Task<IEnumerable<EventHistoryItem>> GetEventHistory(string venue, string sport) {
      return _queryExecutor.NewQuery(@"
        SELECT TOP 5
	        R.[Id],
	        R.[Date],
	        R.[Rating],
	        R.[Rain],
	        T.[Name] AS WinningTeam,
	        STRING_AGG(P.[FirstName] + ' ' + P.[LastName], ', ') WITHIN GROUP (ORDER BY P.[LastName] ASC, P.[FirstName] ASC) AS WinningParticipants
        FROM
	        [dbo].[Round] R
	        INNER JOIN [dbo].[Season] S ON R.[Season] = S.[Id]
	        LEFT JOIN [dbo].[Team] T ON R.[WinningTeam] = T.[Id]
	        LEFT JOIN [dbo].[RoundWinner] RW ON R.[Id] = RW.[Round]
	        LEFT JOIN [dbo].[Participant] P ON P.[Id] = RW.[Participant]
        WHERE
	        R.[Venue] = @Venue
	        AND R.[Status] <> 'Scheduled'
	        AND S.[Sport] = @Sport
        GROUP BY
	        R.[Id],
	        R.[Date],
	        R.[Rating],
	        R.[Rain],
	        T.[Name]
        ORDER BY
        	R.[Date] DESC")
        .WithCommandType(CommandType.Text)
        .WithParameters(new {
          Venue = venue,
          Sport = sport
        })
        .ExecuteAsync<EventHistoryItem>();
    }

    public async Task<HomeDisplayModel> GetHomeDisplayModel() {
      var roundsNextUp = (await GetRoundsNextUp()).ToList();
      var roundsNextUpDisplayModels = (await roundsNextUp
        .OrderBy(n => n.Date)
        .SelectAsync(async n => {
          var eventHistory = await GetEventHistory(n.Venue, n.Sport);
          return new NextUpDisplayModel(n, roundsNextUp, eventHistory);
        }))
        .ToList();
      var veryNextUp = roundsNextUpDisplayModels
        .FirstOrDefault(n => n.IsVeryNextUp);
      var allSeasons = await _context.Season
        .Include(s => s.RelatedSport)
        .Include(s => s.RelatedRounds)
        .Select(s => new HomeDisplayModel.SeasonDisplayModelForHome(s))
        .ToListAsync();
      return new HomeDisplayModel {
        VeryNextUp = veryNextUp,
        NextUpPerSport = roundsNextUpDisplayModels
          .Where(n => veryNextUp == null || n != veryNextUp)
          .ToList(),
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