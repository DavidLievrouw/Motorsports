using Motorsports.Common;

namespace Motorsports.Infrastructure.Entities;

public sealed class Country
{
    public CountryIso Iso { get; set; }
    public string? Name { get; set; }
    public string? NiceName { get; set; }
    public string? Iso3 { get; set; }
    public short? NumCode { get; set; }
    public short? PhoneCode { get; set; }
}

public sealed class Sport
{
    public SportName Name { get; set; }
    public string? FullName { get; set; }
    public byte[]? Image { get; set; }
}

public sealed class Status
{
    public StatusName Name { get; set; }
    public sbyte Step { get; set; }
}

public sealed class Venue
{
    public VenueName Name { get; set; }
    public CountryIso Country { get; set; }
    public Country? CountryRef { get; set; }
}

public sealed class Participant
{
    public ParticipantId Id { get; set; }
    public string Title { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public CountryIso Country { get; set; }
    public Country? CountryRef { get; set; }
}

public sealed class Team
{
    public TeamId Id { get; set; }
    public string Name { get; set; } = "";
    public SportName Sport { get; set; }
    public CountryIso Country { get; set; }
    public Sport? SportRef { get; set; }
    public Country? CountryRef { get; set; }
}

public sealed class Season
{
    public SeasonId Id { get; set; }
    public SportName Sport { get; set; }
    public string? Label { get; set; }
    public TeamId? WinningTeam { get; set; }
    public Sport? SportRef { get; set; }
}

public sealed class SeasonEntry
{
    public SeasonId Season { get; set; }
    public TeamId Team { get; set; }
    public string Name { get; set; } = "";
    public Season? SeasonRef { get; set; }
    public Team? TeamRef { get; set; }
}

public sealed class SeasonWinner
{
    public SeasonWinnerId Id { get; set; }
    public SeasonId Season { get; set; }
    public ParticipantId Participant { get; set; }
}

public sealed class Round
{
    public RoundId Id { get; set; }
    public DateOnly Date { get; set; }
    public short Number { get; set; }
    public string? Name { get; set; }
    public SeasonId Season { get; set; }
    public VenueName Venue { get; set; }
    public StatusName Status { get; set; }
    public decimal? Rating { get; set; }
    public decimal? Rain { get; set; }
    public TeamId? WinningTeam { get; set; }
    public string? Note { get; set; }
    public Season? SeasonRef { get; set; }
    public Status? StatusRef { get; set; }
}

public sealed class RoundWinner
{
    public RoundWinnerId Id { get; set; }
    public RoundId Round { get; set; }
    public ParticipantId Participant { get; set; }
}

public sealed class User
{
    public UserId Id { get; set; }
    public string Username { get; set; } = "";
    public byte[] PasswordHash { get; set; } = [];
    public byte[] Salt { get; set; } = [];
    public int Iterations { get; set; }
    public string Prf { get; set; } = "HMACSHA512";
    public bool ForceChangePassword { get; set; }
    public string? Title { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? EmailAddress { get; set; }
    public bool IsDeleted { get; set; }
    public string? PasswordHashV3 { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
}
