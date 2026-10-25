using FluentValidation;

namespace Motorsports.Teams;

internal sealed record TeamRow(int Id, string Name, string Sport, string Country, string CountryName);

internal sealed record TeamInfo(
    int Id,
    string Name,
    string Sport,
    string Country,
    string CountryName,
    int SeasonEntryCount
);

/// <summary>Form model for create and edit. <see cref="RowVersion"/> is the optimistic concurrency token of an edit.</summary>
public sealed class TeamForm
{
    public string? Name { get; set; }

    public string? Sport { get; set; }

    public string? Country { get; set; }

    public string? RowVersion { get; set; }
}

internal sealed class TeamFormValidator : AbstractValidator<TeamForm>
{
    public const int MaxNameLength = 50;

    public TeamFormValidator()
    {
        RuleFor(x => x.Name)
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("Name is required.")
            .Must(v => v is null || v.Trim().Length <= MaxNameLength)
            .WithMessage($"Name can have at most {MaxNameLength} characters.");
        RuleFor(x => x.Sport).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Select a sport.");
        RuleFor(x => x.Country).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Select a country.");
    }
}
