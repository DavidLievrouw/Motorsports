using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Motorsports.Common;

namespace Motorsports.Teams;

public static class TeamsModule
{
    /// <summary>Assembly holding the routable pages, for the host's router.</summary>
    public static Assembly Assembly => typeof(TeamsModule).Assembly;

    extension(IServiceCollection services)
    {
        public IServiceCollection AddTeamsServices()
        {
            services.AddValidatorsFromAssemblyContaining<TeamFormValidator>(ServiceLifetime.Singleton);
            services.AddScoped<TeamService>();
            return services;
        }
    }

    internal static Error ToError(this ValidationResult result) =>
        Error.Validation(
            result
                .Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray())
        );
}
