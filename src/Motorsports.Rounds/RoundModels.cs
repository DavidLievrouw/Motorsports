using FluentValidation;
using Motorsports.Common;

namespace Motorsports.Rounds;

internal sealed record RoundSeasonContext(int Id, string Sport, string? Label);

internal sealed record RoundRow(
    int Id,
    DateOnly Date,
    short Number,
    string? Name,
    string Venue,
    string VenueCountry,
    string Status,
    sbyte StatusStep,
    decimal? Rating,
    string? WinningTeamName,
    string? WinningTeamCountry,
    string[] Winners
);

internal sealed record RoundInfo(
    int Id,
    int SeasonId,
    string Sport,
    string? SeasonLabel,
    DateOnly Date,
    short Number,
    string? Name,
    string Venue,
    string VenueCountry,
    string Status,
    sbyte StatusStep,
    decimal? Rating,
    decimal? Rain,
    string? Note,
    int? WinningTeam,
    string? WinningTeamName,
    string? WinningTeamCountry,
    string[] WinningParticipants
);

internal sealed record EventHistoryItem(
    int Id,
    DateOnly Date,
    short Number,
    string? Name,
    string Venue,
    string VenueCountry,
    string Status,
    decimal? Rating,
    decimal? Rain,
    string? WinningTeamName,
    string? WinningTeamCountry,
    string[] Winners
);

internal sealed record RoundDeleteInfo(int Id, int SeasonId, string Label, bool HasWinners, bool HasWinningTeam);

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
    }
}
