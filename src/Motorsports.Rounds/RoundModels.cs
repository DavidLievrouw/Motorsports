using FluentValidation;
using Motorsports.Common;

namespace Motorsports.Rounds;

internal sealed record RoundSeasonContext(SeasonId Id, SportName Sport, string? Label);

internal sealed record RoundRow(
    RoundId Id,
    DateOnly Date,
    short Number,
    string? Name,
    VenueName Venue,
    CountryIso VenueCountry,
    StatusName Status,
    sbyte StatusStep,
    decimal? Rating,
    string? WinningTeamName,
    string? WinningTeamCountry,
    string[] Winners
);

internal sealed record RoundInfo(
    RoundId Id,
    SeasonId SeasonId,
    SportName Sport,
    string? SeasonLabel,
    DateOnly Date,
    short Number,
    string? Name,
    VenueName Venue,
    CountryIso VenueCountry,
    StatusName Status,
    sbyte StatusStep,
    decimal? Rating,
    decimal? Rain,
    string? Note,
    TeamId? WinningTeam,
    string? WinningTeamName,
    string? WinningTeamCountry,
    string[] WinningParticipants
);

internal sealed record EventHistoryItem(
    RoundId Id,
    DateOnly Date,
    short Number,
    string? Name,
    VenueName Venue,
    CountryIso VenueCountry,
    StatusName Status,
    decimal? Rating,
    decimal? Rain,
    string? WinningTeamName,
    string? WinningTeamCountry,
    string[] Winners
);

internal sealed record RoundDeleteInfo(
    RoundId Id,
    SeasonId SeasonId,
    string Label,
    bool HasWinners,
    bool HasWinningTeam
);

internal sealed record RoundFinishForm(RoundForm Values, CountryIso VenueCountry);

public sealed class RoundForm
{
    public int SeasonId { get; set; }

    public DateOnly? Date { get; set; }

    public DateOnly? OriginalDate { get; set; }

    public short Number { get; set; }

    public string? Name { get; set; }

    public string? Venue { get; set; }

    public string? Status { get; set; }

    public string? Note { get; set; }

    public int? Rating { get; set; }

    public RainLevel? Rain { get; set; }

    public int? WinningTeam { get; set; }

    public List<int> Winners { get; set; } = [];

    public string? RowVersion { get; set; }
}

internal sealed class RoundFormValidator : AbstractValidator<RoundForm>
{
    public static readonly DateOnly MinDate = new(2026, 1, 1);
    public const int MaxNameLength = 100;
    public const int MaxNoteLength = 1024;
    public const int MaxWinners = 4;

    public RoundFormValidator()
    {
        RuleFor(form => form.Date).NotNull().WithMessage("Date is required.");
        RuleFor(form => form.Date)
            .Must((form, date) => date is null || form.OriginalDate == date || date >= MinDate)
            .WithMessage($"Date must be on or after {MinDate:yyyy-MM-dd}.");
        RuleFor(form => form.Number)
            .InclusiveBetween((short)0, (short)99)
            .WithMessage("Number must be between 0 and 99.");
        RuleFor(form => form.Name)
            .Must(value => value is null || value.Trim().Length <= MaxNameLength)
            .WithMessage($"Name can have at most {MaxNameLength} characters.");
        RuleFor(form => form.Venue).Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Select a venue.");
        RuleFor(form => form.Status).Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Select a status.");
        RuleFor(form => form.Note)
            .Must(value => value is null || value.Trim().Length <= MaxNoteLength)
            .WithMessage($"Note can have at most {MaxNoteLength} characters.");
        RuleFor(form => form.Rating)
            .Must(value => value is null or >= 0 and <= 10)
            .WithMessage("Rating must be a whole number from 0 to 10.");
        RuleFor(form => form.Rain)
            .Must(value => value is null || Enum.IsDefined(value.Value))
            .WithMessage("Select a valid rain level.");
        RuleFor(form => form.Winners)
            .Must(winners => winners.Distinct().Count() == winners.Count)
            .WithMessage("Winning participants cannot contain duplicates.")
            .Must(winners => winners.Count <= MaxWinners)
            .WithMessage($"Select at most {MaxWinners} winning participants.");

        RuleSet(
            StatusName.Finished.Value,
            () =>
            {
                RuleFor(form => form.Rating).NotNull().WithMessage("Rating is required.");
                RuleFor(form => form.Rain).NotNull().WithMessage("Rain is required.");
                RuleFor(form => form.WinningTeam).NotNull().WithMessage("Select a winning team.");
                RuleFor(form => form.Winners).NotEmpty().WithMessage("Select at least one winning participant.");
            }
        );
        RuleSet(
            StatusName.Stopped.Value,
            () =>
            {
                RuleFor(form => form.Rating).NotNull().WithMessage("Rating is required.");
                RuleFor(form => form.Rain).NotNull().WithMessage("Rain is required.");
            }
        );
        RuleSet(
            StatusName.Postponed.Value,
            () => RuleFor(form => form.Note).NotEmpty().WithMessage("Note is required when postponing a round.")
        );
        RuleSet(
            StatusName.Cancelled.Value,
            () => RuleFor(form => form.Note).NotEmpty().WithMessage("Note is required when cancelling a round.")
        );
    }
}
