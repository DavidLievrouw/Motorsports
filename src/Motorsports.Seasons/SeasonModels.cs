using FluentValidation;
using Motorsports.Common;

namespace Motorsports.Seasons;

internal sealed record SeasonRow(
    SeasonId Id,
    SportName Sport,
    string? SportFullName,
    bool SportHasImage,
    string? Label,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int SeasonEntryCount,
    int RoundCount
);

internal sealed record SeasonInfo(
    SeasonId Id,
    SportName Sport,
    string? SportFullName,
    string? Label,
    DateOnly? StartDate,
    DateOnly? EndDate,
    TeamId? WinningTeam,
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

internal sealed class SeasonFormValidator : AbstractValidator<SeasonForm>
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
