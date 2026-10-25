using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Motorsports.Common;

namespace Motorsports.Participants;

public static class ParticipantsModule
{
    /// <summary>Assembly holding the routable pages, for the host's router.</summary>
    public static Assembly Assembly => typeof(ParticipantsModule).Assembly;

    extension(IServiceCollection services)
    {
        public IServiceCollection AddParticipantsServices()
        {
            services.AddValidatorsFromAssemblyContaining<ParticipantFormValidator>(
                ServiceLifetime.Singleton,
                includeInternalTypes: true
            );
            services.AddScoped<ParticipantService>();
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
