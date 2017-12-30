using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Motorsports.Scaffolding.Core.Dapper;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.Validators;
using Motorsports.Scaffolding.Core.Models.Validators.Create;
using Motorsports.Scaffolding.Core.Models.Validators.Update;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core {
  public class Startup {
    public Startup(IHostingEnvironment env) {
      var builder = new ConfigurationBuilder()
        .SetBasePath(env.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile("appsettings-connectionstrings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();
      Configuration = builder.Build();
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services) {
      services.AddMvc();

      // Lowest level data access
      var connectionString = Configuration.GetConnectionString("Motorsports");
      services.AddDbContext<MotorsportsContext>(options => options.UseSqlServer(connectionString).EnableSensitiveDataLogging(CurrentEnvironment.IsDevelopment()));
      services.TryAddSingleton<IQueryExecutor>(new QueryExecutor(new SqlDbConnectionFactory(connectionString)));

      // Services
      services.TryAddSingleton<ISportService>(provider => new SportService(provider.GetRequiredService<IQueryExecutor>()));
      services.TryAddSingleton<IVenueService>(provider => new VenueService(provider.GetRequiredService<IQueryExecutor>()));
      services.TryAddScoped<ISeasonService>(provider => new SeasonService(provider.GetService<MotorsportsContext>()));
      services.TryAddScoped<IRoundService>(provider => new RoundService(provider.GetService<MotorsportsContext>()));

      // Validators
      services.TryAddScoped<IModelStatePopulator<Sport>>(provider => new ModelStatePopulator<Sport>(
        new CreateSportValidator(provider.GetService<MotorsportsContext>()),
        new UpdateSportValidator(provider.GetService<MotorsportsContext>())));
      services.TryAddScoped<IModelStatePopulator<Venue>>(provider => new ModelStatePopulator<Venue>(
        new CreateVenueValidator(provider.GetService<MotorsportsContext>()),
        new UpdateVenueValidator(provider.GetService<MotorsportsContext>())));
      services.TryAddScoped<IModelStatePopulator<Team>>(provider => new ModelStatePopulator<Team>(
        new CreateTeamValidator(provider.GetService<MotorsportsContext>()),
        new UpdateTeamValidator(provider.GetService<MotorsportsContext>())));
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
      CurrentEnvironment = env;

      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
        app.UseBrowserLink();
      }
      else app.UseExceptionHandler("/Home/Error");

      app.UseStaticFiles();

      app.UseMvc(
        routes => {
          routes.MapRoute(
            name: "default",
            template: "{controller=Home}/{action=Index}/{id?}");
        });
    }

    public IHostingEnvironment CurrentEnvironment { get; private set; }
  }
}