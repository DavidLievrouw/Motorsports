using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;

namespace Motorsports.Scaffolding.Core.Services {
  public interface ISeasonEntryService {
    Task<List<SeasonEntry>> LoadSeasonEntryList(int seasonId);
  }

  public class SeasonEntryService : ISeasonEntryService {
    readonly MotorsportsContext _context;

    public SeasonEntryService(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<List<SeasonEntry>> LoadSeasonEntryList(int seasonId) {
      return _context.SeasonEntry
        .Include(se => se.RelatedTeam)
        .Include(se => se.RelatedSeason)
        .Where(se => se.Season == seasonId)
        .OrderBy(se => se.RelatedTeam.Name)
        .ThenBy(se => se.Name)
        .ToListAsync();
    }
  }
}