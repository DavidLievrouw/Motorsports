using FluentValidation;
using Motorsports.Common;

namespace Motorsports.Seasons;

internal sealed record SeasonEntryRow(
    SeasonId SeasonId,
    TeamId TeamId,
    string Name,
    string TeamName,
    CountryIso Country,
    string CountryName
);

internal sealed record SeasonEntryInfo(
    SeasonId SeasonId,
    TeamId TeamId,
    string? SeasonLabel,
    SportName Sport,
    string Name,
    string TeamName,
    CountryIso Country,
    string CountryName,
    bool IsSeasonWinner,
    bool IsRoundWinner
);

public sealed record SeasonEntryContext(SeasonId Id, string? Label, SportName Sport);

internal sealed record SeasonDeleteInfo(SeasonId Id, string? Label, int RoundCount, int EntryCount, int WinnerCount);

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
