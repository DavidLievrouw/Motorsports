using FluentValidation;

namespace Motorsports.Seasons;

internal sealed record SeasonEntryRow(
    int SeasonId,
    int TeamId,
    string Name,
    string TeamName,
    string Country,
    string CountryName
);

internal sealed record SeasonEntryInfo(
    int SeasonId,
    int TeamId,
    string? SeasonLabel,
    string Sport,
    string Name,
    string TeamName,
    string Country,
    string CountryName,
    bool IsSeasonWinner,
    bool IsRoundWinner
);

public sealed record SeasonEntryContext(int Id, string? Label, string Sport);

internal sealed record SeasonDeleteInfo(int Id, string? Label, int RoundCount, int EntryCount, int WinnerCount);

public sealed class SeasonEntryForm
{
    public int? TeamId { get; set; }

    public string? Name { get; set; }

    public string? RowVersion { get; set; }
}

internal sealed class SeasonEntryFormValidator : AbstractValidator<SeasonEntryForm>
{
    public const int MaxNameLength = 50;

    public SeasonEntryFormValidator()
    {
        RuleFor(form => form.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name is required.")
            .Must(value => value is null || value.Trim().Length <= MaxNameLength)
            .WithMessage($"Name can have at most {MaxNameLength} characters.");
    }
}
