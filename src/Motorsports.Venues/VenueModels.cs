using FluentValidation;
using Motorsports.Common;

namespace Motorsports.Venues;

internal sealed record VenueRow(VenueName Name, CountryIso Country, string CountryName);

internal sealed record VenueRoundInfo(
    RoundId Id,
    DateOnly Date,
    short Number,
    string? Name,
    string Label,
    SportName Sport,
    bool SportHasImage,
    StatusName Status,
    decimal? Rating,
    string? Rain,
    string? WinningTeamName,
    IReadOnlyList<string> WinningParticipants
);

internal sealed record VenueInfo(
    VenueName Name,
    CountryIso Country,
    string CountryName,
    int RoundCount,
    IReadOnlyList<VenueRoundInfo> Rounds
);

/// <summary>Form model for create and edit. <see cref="RowVersion"/> is the optimistic concurrency token of an edit.</summary>
public sealed class VenueForm
{
    public string? Name { get; set; }

    public string? Country { get; set; }

    public string? RowVersion { get; set; }
}

internal sealed class VenueFormValidator : AbstractValidator<VenueForm>
{
    public const int MaxNameLength = 100;

    public VenueFormValidator()
    {
        RuleFor(x => x.Name)
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("Name is required.")
            .Must(v => v is null || v.Trim().Length <= MaxNameLength)
            .WithMessage($"Name can have at most {MaxNameLength} characters.");
        RuleFor(x => x.Country).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Select a country.");
    }
}
