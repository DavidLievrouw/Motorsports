using FluentValidation;

namespace Motorsports.Venues;

public sealed record VenueRow(string Name, string Country, string CountryName);

public sealed record VenueRoundInfo(
    int Id,
    DateOnly Date,
    short Number,
    string? Name,
    string Label,
    string Sport,
    bool SportHasImage,
    string Status,
    decimal? Rating,
    string? Rain,
    string? WinningTeamName,
    IReadOnlyList<string> WinningParticipants
);

public sealed record VenueInfo(
    string Name,
    string Country,
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

public sealed class VenueFormValidator : AbstractValidator<VenueForm>
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
