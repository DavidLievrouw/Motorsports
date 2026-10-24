using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;
using Motorsports.Participants;
using Motorsports.Sports;
using Motorsports.Teams;
using Motorsports.Venues;
using Shouldly;

namespace Motorsports.UnitTests.ReferenceData;

public class ValidatorTests
{
    private static string Long(int n) => new('x', n);

    [Fact]
    public void Sport_rules_trim_before_measuring_and_enforce_max_lengths()
    {
        var v = new SportFormValidator();
        v.Validate(new SportForm { Name = "  " + Long(50) + "  ", FullName = " " + Long(100) }).IsValid.ShouldBeTrue();
        v.Validate(new SportForm { Name = Long(51) }).IsValid.ShouldBeFalse();
        v.Validate(new SportForm { Name = "a", FullName = Long(101) }).IsValid.ShouldBeFalse();
        v.Validate(new SportForm { Name = "  " }).IsValid.ShouldBeFalse();
        v.Validate(new SportForm { Name = "a", FullName = null }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Venue_team_and_participant_rules_enforce_lengths_and_required_fields()
    {
        new VenueFormValidator().Validate(new VenueForm { Name = Long(100), Country = "GB" }).IsValid.ShouldBeTrue();
        new VenueFormValidator().Validate(new VenueForm { Name = Long(101), Country = "GB" }).IsValid.ShouldBeFalse();
        new VenueFormValidator().Validate(new VenueForm { Name = "a" }).IsValid.ShouldBeFalse();

        var team = new TeamFormValidator();
        team.Validate(
                new TeamForm
                {
                    Name = Long(50),
                    Sport = "F1",
                    Country = "GB",
                }
            )
            .IsValid.ShouldBeTrue();
        team.Validate(
                new TeamForm
                {
                    Name = Long(51),
                    Sport = "F1",
                    Country = "GB",
                }
            )
            .IsValid.ShouldBeFalse();
        team.Validate(new TeamForm { Name = "a", Country = "GB" }).IsValid.ShouldBeFalse();

        var p = new ParticipantFormValidator();
        p.Validate(
                new ParticipantForm
                {
                    Title = Long(10),
                    FirstName = Long(100),
                    LastName = Long(100),
                    Country = "GB",
                }
            )
            .IsValid.ShouldBeTrue();
        p.Validate(
                new ParticipantForm
                {
                    Title = Long(11),
                    FirstName = "a",
                    LastName = "b",
                    Country = "GB",
                }
            )
            .IsValid.ShouldBeFalse();
        p.Validate(
                new ParticipantForm
                {
                    FirstName = "",
                    LastName = "b",
                    Country = "GB",
                }
            )
            .IsValid.ShouldBeFalse();
        p.Validate(
                new ParticipantForm
                {
                    FirstName = "a",
                    LastName = "b",
                    Country = "GB",
                }
            )
            .IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validator_limits_match_the_database_model()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, "Server=localhost;Database=x");
        using var db = new MotorsportsDbContext(options.Options);
        int? Max<T>(string property) => db.Model.FindEntityType(typeof(T))!.FindProperty(property)!.GetMaxLength();

        Max<Sport>(nameof(Sport.Name)).ShouldBe(SportFormValidator.MaxNameLength);
        Max<Sport>(nameof(Sport.FullName)).ShouldBe(SportFormValidator.MaxFullNameLength);
        Max<Venue>(nameof(Venue.Name)).ShouldBe(VenueFormValidator.MaxNameLength);
        Max<Team>(nameof(Team.Name)).ShouldBe(TeamFormValidator.MaxNameLength);
        Max<Participant>(nameof(Participant.Title)).ShouldBe(ParticipantFormValidator.MaxTitleLength);
        Max<Participant>(nameof(Participant.FirstName)).ShouldBe(ParticipantFormValidator.MaxFirstNameLength);
        Max<Participant>(nameof(Participant.LastName)).ShouldBe(ParticipantFormValidator.MaxLastNameLength);
    }
}

public class RowVersionTests
{
    [Fact]
    public void Same_values_give_the_same_token_and_any_change_gives_another()
    {
        var a = RowVersion.Compute("a", "b");
        RowVersion.Matches(a, RowVersion.Compute("a", "b")).ShouldBeTrue();
        RowVersion.Matches(a, RowVersion.Compute("a", "c")).ShouldBeFalse();
        RowVersion.Matches(null, a).ShouldBeFalse();
    }

    [Fact]
    public void Field_boundaries_and_nulls_are_not_ambiguous()
    {
        RowVersion.Compute("ab", "c").ShouldNotBe(RowVersion.Compute("a", "bc"));
        RowVersion.Compute(null, "a").ShouldNotBe(RowVersion.Compute("", "a"));
    }
}

public class FormErrorsTests
{
    [Fact]
    public void Validation_errors_map_to_fields_and_form_level_summary()
    {
        var errors = new FormErrors();
        errors.Set(Error.Validation("Name", "Required."));
        errors.Has("Name").ShouldBeTrue();
        errors.Text("Name").ShouldBe("Required.");

        errors.Clear();
        errors.Set(Error.Conflict("Changed."));
        errors.Summary.ShouldBe("Changed.");
        errors.Has("Name").ShouldBeFalse();
    }
}
