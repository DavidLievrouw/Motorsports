using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Motorsports.Common;

namespace Motorsports.Rounds;

public static class RoundsModule
{
    public static Assembly Assembly => typeof(RoundsModule).Assembly;

    extension(IServiceCollection services)
    {
        public IServiceCollection AddRoundsServices()
        {
            services.AddValidatorsFromAssemblyContaining<RoundFormValidator>(
                ServiceLifetime.Singleton,
                includeInternalTypes: true
            );
            services.AddScoped<RoundService>();
            return services;
        }
    }

    internal static Error ToError(this ValidationResult result) =>
        Error.Validation(
            result
                .Errors.GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).Distinct().ToArray()
                )
        );
}
