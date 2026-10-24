using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Motorsports.Common;

namespace Motorsports.Seasons;

public static class SeasonsModule
{
    public static Assembly Assembly => typeof(SeasonsModule).Assembly;

    extension(IServiceCollection services)
    {
        public IServiceCollection AddSeasonsServices()
        {
            services.AddValidatorsFromAssemblyContaining<SeasonFormValidator>(ServiceLifetime.Singleton);
            services.AddScoped<SeasonService>();
            services.AddScoped<SeasonEntryService>();
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
