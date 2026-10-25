using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Motorsports.Common;
using Motorsports.Common.Images;
using Motorsports.Infrastructure;

namespace Motorsports.Sports;

public static class SportsModule
{
    /// <summary>Assembly holding the routable pages, for the host's router.</summary>
    public static Assembly Assembly => typeof(SportsModule).Assembly;

    extension(IServiceCollection services)
    {
        public IServiceCollection AddSportsServices()
        {
            services.AddValidatorsFromAssemblyContaining<SportFormValidator>(
                ServiceLifetime.Singleton,
                includeInternalTypes: true
            );
            services.AddScoped<SportService>();
            return services;
        }
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Serves stored sport images so pages reference a cacheable URL instead of inlining bytes.</summary>
        public IEndpointRouteBuilder MapSportImages()
        {
            endpoints.MapGet(
                "/sports/image/{name}",
                async (string name, HttpContext http, IDbContextFactory<MotorsportsDbContext> factory) =>
                {
                    await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
                    var image = await db
                        .Sports.AsNoTracking()
                        .Where(s => s.Name == name)
                        .Select(s => s.Image)
                        .FirstOrDefaultAsync(http.RequestAborted);
                    if (image is null || SportImage.ContentType(image) is not { } contentType)
                    {
                        return Results.Redirect("/img/notfound.png");
                    }

                    var etag =
                        $"\"{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(image.AsSpan()))[..16]}\"";
                    http.Response.Headers.CacheControl = "private, max-age=300";
                    http.Response.Headers.ETag = etag;
                    if (http.Request.Headers.IfNoneMatch == etag)
                    {
                        return Results.StatusCode(StatusCodes.Status304NotModified);
                    }

                    return Results.Bytes(image, contentType);
                }
            );
            return endpoints;
        }
    }

    internal static Error ToError(this ValidationResult result) =>
        Error.Validation(
            result
                .Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray())
        );
}
