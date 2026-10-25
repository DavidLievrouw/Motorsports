using FluentValidation;

namespace Motorsports.Participants;

internal sealed record ParticipantRow(
    int Id,
    string Title,
    string FirstName,
    string LastName,
    string Country,
    string CountryName
);

internal sealed record ParticipantInfo(
    int Id,
    string Title,
    string FirstName,
    string LastName,
    string Country,
    string CountryName,
    string[] Sports,
    ParticipantRoundWin[] RoundWins,
    ParticipantSeasonWin[] SeasonWins
);

internal sealed record ParticipantRoundWin(
    int RoundId,
    DateOnly Date,
    string Sport,
    string? SeasonLabel,
    short Number,
    string? Name,
    string Venue,
    string VenueCountry,
    decimal? Rating
);

internal sealed record ParticipantSeasonWin(
    int SeasonId,
    string Sport,
    string? Label,
    DateOnly? StartDate,
    DateOnly? EndDate
);

/// <summary>Form model for create and edit. <see cref="RowVersion"/> is the optimistic concurrency token of an edit.</summary>
public sealed class ParticipantForm
{
    public string? Title { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Country { get; set; }

    public string? RowVersion { get; set; }
}

internal sealed class ParticipantFormValidator : AbstractValidator<ParticipantForm>
{
    public const int MaxTitleLength = 10;
    public const int MaxFirstNameLength = 100;
    public const int MaxLastNameLength = 100;

    public ParticipantFormValidator()
    {
        RuleFor(x => x.Title)
            .Must(v => v is null || v.Trim().Length <= MaxTitleLength)
            .WithMessage($"Title can have at most {MaxTitleLength} characters.");
        RuleFor(x => x.FirstName)
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("First name is required.")
            .Must(v => v is null || v.Trim().Length <= MaxFirstNameLength)
            .WithMessage($"First name can have at most {MaxFirstNameLength} characters.");
        RuleFor(x => x.LastName)
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("Last name is required.")
            .Must(v => v is null || v.Trim().Length <= MaxLastNameLength)
            .WithMessage($"Last name can have at most {MaxLastNameLength} characters.");
        RuleFor(x => x.Country).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Select a country.");
    }
}
