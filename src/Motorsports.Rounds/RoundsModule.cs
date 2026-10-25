using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapRoundsEndpoints()
        {
            endpoints
                .MapPost(
                    "/rounds/{id:int}/ready-to-watch",
                    async (
                        int id,
                        RoundService service,
                        IAntiforgery antiforgery,
                        HttpContext context,
                        CancellationToken cancellationToken
                    ) =>
                    {
                        if (!await antiforgery.IsRequestValidAsync(context))
                        {
                            return Results.BadRequest();
                        }

                        var result = await service.MarkReadyToWatchAsync(id, cancellationToken);
                        if (result.IsSuccess)
                        {
                            return Results.Redirect("/");
                        }

                        var statusCode = result.Error.Kind switch
                        {
                            ErrorKind.NotFound => StatusCodes.Status404NotFound,
                            ErrorKind.Conflict => StatusCodes.Status409Conflict,
                            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
                            _ => StatusCodes.Status400BadRequest,
                        };
                        return Results.Problem(result.Error.Message, statusCode: statusCode);
                    }
                )
                .RequireAuthorization();
            return endpoints;
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
