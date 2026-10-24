using FluentValidation;

namespace Motorsports.Sports;

public sealed record SportRow(string Name, string? FullName, bool HasImage);

public sealed record SportInfo(
    string Name,
    string? FullName,
    int TeamCount,
    int SeasonCount,
    bool HasImage,
    string? LatestSeasonLabel,
    string? WinningTeamName,
    IReadOnlyList<string> WinningParticipants,
    DateOnly? NextRoundDate
);

/// <summary>Form model for create and edit. <see cref="RowVersion"/> is the optimistic concurrency token of an edit.</summary>
public sealed class SportForm
{
    public string? Name { get; set; }

    public string? FullName { get; set; }

    public string? RowVersion { get; set; }

    public byte[]? Image { get; set; }

    /// <summary>Name under which the stored image is served; null when the sport has none.</summary>
    public string? StoredImageName { get; set; }

    public bool ImageReplaced { get; set; }

    public string? ImageUploadError { get; set; }
}

public sealed class SportFormValidator : AbstractValidator<SportForm>
{
    public const int MaxNameLength = 50;
    public const int MaxFullNameLength = 100;

    public SportFormValidator()
    {
        RuleFor(x => x.Name)
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("Name is required.")
            .Must(v => v is null || v.Trim().Length <= MaxNameLength)
            .WithMessage($"Name can have at most {MaxNameLength} characters.");
        RuleFor(x => x.FullName)
            .Must(v => v is null || v.Trim().Length <= MaxFullNameLength)
            .WithMessage($"Full name can have at most {MaxFullNameLength} characters.");
        RuleFor(x => x.Image)
            .Must(image => image is null || Motorsports.Common.Images.SportImage.IsValid(image))
            .WithMessage("Choose a PNG, JPEG, or WebP image no larger than 2 MB.");
        RuleFor(x => x.ImageUploadError).Must(string.IsNullOrEmpty).WithMessage(x => x.ImageUploadError!);
    }
}
