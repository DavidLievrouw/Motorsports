using Motorsports.Common;

namespace Motorsports.Home;

internal sealed record HomeRound(
    RoundId Id,
    SportName Sport,
    SeasonId SeasonId,
    DateOnly Date,
    short Number,
    string? Name,
    VenueName Venue,
    CountryIso VenueCountry,
    HomeHistoryItem[] EventHistory
);

internal sealed record HomeHistoryItem(
    RoundId Id,
    DateOnly Date,
    short Number,
    string? Name,
    decimal? Rating,
    decimal? Rain,
    string? WinningTeamName,
    string[] Winners
);

internal sealed record HomeOverview(HomeRound? VeryNextUp, HomeRound[] NextUp, HomeRound[] RoundsToAcquire);
