using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Motorsports.Common;

namespace Motorsports.Venues;

public static class VenuesModule
{
    /// <summary>Assembly holding the routable pages, for the host's router.</summary>
    public static Assembly Assembly => typeof(VenuesModule).Assembly;

    extension(IServiceCollection services)
    {
        public IServiceCollection AddVenuesServices()
        {
            services.AddValidatorsFromAssemblyContaining<VenueFormValidator>(ServiceLifetime.Singleton);
            services.AddScoped<VenueService>();
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
