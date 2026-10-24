using FluentValidation;

namespace Motorsports.Seasons;

public sealed record SeasonRow(
    int Id,
    string Sport,
    string? SportFullName,
    bool SportHasImage,
    string? Label,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int SeasonEntryCount,
    int RoundCount
);

public sealed record SeasonInfo(
    int Id,
    string Sport,
    string? SportFullName,
    string? Label,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? WinningTeam,
    string? WinningTeamName,
    string[] WinningParticipants,
    int SeasonEntryCount,
    int RoundCount
);

public sealed class SeasonForm
{
    public string? Sport { get; set; }

    public string? Label { get; set; }

    public int? WinningTeam { get; set; }

    public List<int> Winners { get; set; } = [];

    public string? RowVersion { get; set; }
}

public sealed class SeasonFormValidator : AbstractValidator<SeasonForm>
{
    public const int MaxLabelLength = 100;

    public SeasonFormValidator()
    {
        RuleFor(form => form.Sport).Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Select a sport.");
        RuleFor(form => form.Label)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Label is required.")
            .Must(value => value is null || value.Trim().Length <= MaxLabelLength)
            .WithMessage($"Label can have at most {MaxLabelLength} characters.");
    }
}
