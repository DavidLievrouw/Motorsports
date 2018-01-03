using System;
using System.Collections.Generic;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class HomeDisplayModel {
    public HomeDisplayModel(IEnumerable<NextUp> roundsNextUp) {
      NextUpPerSport = roundsNextUp
        .OrderBy(n => n.Date)
        .Select(n => new NextUpDisplayModel(n))
        .ToList();
      VeryNextUp = NextUpPerSport
        .Where(n => n.Date.Date <= DateTime.Now.Date)
        .OrderBy(n => n.Date)
        .FirstOrDefault();
    }

    public NextUpDisplayModel VeryNextUp { get; set; }

    public bool HasVeryNextUp => VeryNextUp != null;

    public IEnumerable<NextUpDisplayModel> NextUpPerSport { get; set; }

    public bool HasNextUpPerSport => NextUpPerSport.Any();
  }
}