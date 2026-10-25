using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Motorsports.Home;

public static class HomeModule
{
    public static Assembly Assembly => typeof(HomeModule).Assembly;

    extension(IServiceCollection services)
    {
        public IServiceCollection AddHomeServices()
        {
            services.AddScoped<HomeService>();
            return services;
        }
    }
}
