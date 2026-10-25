namespace Motorsports.Home;

internal sealed record HomeRound(
    int Id,
    string Sport,
    int SeasonId,
    DateOnly Date,
    short Number,
    string? Name,
    string Venue,
    string VenueCountry,
    HomeHistoryItem[] EventHistory
);

internal sealed record HomeHistoryItem(
    int Id,
    DateOnly Date,
    short Number,
    string? Name,
    decimal? Rating,
    decimal? Rain,
    string? WinningTeamName,
    string[] Winners
);

internal sealed record HomeOverview(HomeRound? VeryNextUp, HomeRound[] NextUp, HomeRound[] RoundsToAcquire);
