﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Motorsports.Scaffolding.Core.Dapper;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.Validators;
using Motorsports.Scaffolding.Core.Models.Validators.Create;
using Motorsports.Scaffolding.Core.Models.Validators.Delete;
using Motorsports.Scaffolding.Core.Models.Validators.Update;
using Motorsports.Scaffolding.Core.Security;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core;

public class Startup {
  public Startup(IWebHostEnvironment env) {
    var builder = new ConfigurationBuilder()
      .SetBasePath(env.ContentRootPath)
      .AddJsonFile("appsettings.json", true, true)
      .AddJsonFile("appsettings-connectionstrings.json", true, true);

    if (env.IsDevelopment()) {
      builder = builder
        .AddJsonFile("appsettings.Development.json", true, true);
    }

    builder = builder
      .AddEnvironmentVariables();

    Configuration = builder.Build();
  }

  public IConfiguration Configuration { get; }

  public IWebHostEnvironment CurrentEnvironment { get; private set; }

  public void ConfigureServices(IServiceCollection services) {
    services.Configure<RequestLocalizationOptions>(options => {
      options.DefaultRequestCulture = new RequestCulture("en-US");
      options.SupportedCultures = new List<CultureInfo> { new("en-US") };
    });
    services.AddMvc();

    // Lowest level data access
    var connectionString = Configuration.GetConnectionString("Motorsports");
    var serverVersion = ServerVersion.AutoDetect(connectionString);
    services.AddDbContext<MotorsportsContext>(options => options
      .UseMySql(connectionString, serverVersion)
      .LogTo(Console.WriteLine, LogLevel.Information)
      .EnableSensitiveDataLogging(
        CurrentEnvironment.IsDevelopment()
      )
    );
    services.TryAddSingleton<IQueryExecutor>(new QueryExecutor(new MariaDbConnectionFactory(connectionString)));

    // Services
    services.TryAddSingleton<ISportService>(provider => new SportService(provider.GetRequiredService<IQueryExecutor>()));
    services.TryAddSingleton<IVenueService>(provider => new VenueService(provider.GetRequiredService<IQueryExecutor>()));
    services.TryAddScoped<IHomeService>(provider => new HomeService(
      provider.GetService<MotorsportsContext>(),
      provider.GetRequiredService<IQueryExecutor>(),
      provider.GetRequiredService<IRoundService>()));
    services.TryAddScoped<ISeasonService>(provider => new SeasonService(provider.GetService<MotorsportsContext>(), provider.GetRequiredService<IQueryExecutor>()));
    services.TryAddScoped<IRoundService>(provider => new RoundService(provider.GetService<MotorsportsContext>(), provider.GetRequiredService<IQueryExecutor>()));
    services.TryAddScoped<ISeasonEntryService>(provider => new SeasonEntryService(provider.GetService<MotorsportsContext>(), provider.GetRequiredService<IQueryExecutor>()));

    // Helpers for requests
    services.TryAddSingleton<IActionContextAccessor>(provider => new ActionContextAccessor());
    services.TryAddScoped<IUrlHelper>(provider => new UrlHelper(provider.GetService<IActionContextAccessor>().ActionContext));
    services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider);
    services.TryAddScoped<IImageService, ImageService>();

    // Validators
    services.TryAddScoped<IModelStatePopulator<Sport, string>>(provider => new ModelStatePopulator<Sport, string>(
      new CreateSportValidator(provider.GetService<MotorsportsContext>()),
      new UpdateSportValidator(provider.GetService<MotorsportsContext>()),
      new DisallowDeleteInGeneralValidator()));
    services.TryAddScoped<IModelStatePopulator<Venue, string>>(provider => new ModelStatePopulator<Venue, string>(
      new CreateVenueValidator(provider.GetService<MotorsportsContext>()),
      new UpdateVenueValidator(provider.GetService<MotorsportsContext>()),
      new DisallowDeleteInGeneralValidator()));
    services.TryAddScoped<IModelStatePopulator<Team, int>>(provider => new ModelStatePopulator<Team, int>(
      new CreateTeamValidator(provider.GetService<MotorsportsContext>()),
      new UpdateTeamValidator(provider.GetService<MotorsportsContext>()),
      new DisallowDeleteInGeneralValidator()));
    services.TryAddScoped<IModelStatePopulator<Participant, int>>(provider => new ModelStatePopulator<Participant, int>(
      new CreateParticipantValidator(provider.GetService<MotorsportsContext>()),
      new UpdateParticipantValidator(provider.GetService<MotorsportsContext>()),
      new DisallowDeleteInGeneralValidator()));
    services.TryAddScoped<IModelStatePopulator<Season, int>>(provider => new ModelStatePopulator<Season, int>(
      new CreateSeasonValidator(provider.GetService<MotorsportsContext>()),
      new UpdateSeasonValidator(provider.GetService<MotorsportsContext>()),
      new DeleteSeasonValidator(provider.GetService<MotorsportsContext>())));
    services.TryAddScoped<IModelStatePopulator<Round, int>>(provider => new ModelStatePopulator<Round, int>(
      new CreateRoundValidator(provider.GetService<MotorsportsContext>()),
      new UpdateRoundValidator(provider.GetService<MotorsportsContext>()),
      new DeleteRoundValidator(provider.GetService<MotorsportsContext>())));
    services.TryAddScoped<IModelStatePopulator<SeasonEntry, SeasonEntry.SeasonEntryKey>>(provider => new ModelStatePopulator<SeasonEntry, SeasonEntry.SeasonEntryKey>(
      new CreateSeasonEntryValidator(provider.GetService<MotorsportsContext>()),
      new UpdateSeasonEntryValidator(provider.GetService<MotorsportsContext>()),
      new DeleteSeasonEntryValidator(provider.GetService<MotorsportsContext>())));

    // Security
    services.TryAddSingleton(provider => new PasswordHashingConfig());
    services.TryAddSingleton<IHashPasswordService>(provider => new HashPasswordService(provider.GetRequiredService<PasswordHashingConfig>()));
    services.TryAddSingleton<IAuthenticateUserService<UsernamePasswordCredentials>>(provider => new UsernamePasswordAuthenticateUserService(
      new UserDataService(provider.GetRequiredService<IQueryExecutor>()),
      new RandomHashedPasswordProvider(provider.GetRequiredService<IHashPasswordService>()),
      provider.GetRequiredService<IHashPasswordService>(),
      new UsernamePasswordCredentialsValidator(),
      provider.GetRequiredService<IAuthenticationSchemeProvider>()));

    // Authentication
    services
      .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
      .AddCookie(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options => {
          options.ReturnUrlParameter = "returnUrl";
          options.LoginPath = "/login";
          options.LogoutPath = "/logout";
          options.AccessDeniedPath = "/accessdenied";
        });
  }

  public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
    CurrentEnvironment = env;

    if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
    else app.UseExceptionHandler("/error");

    app
      .UseRequestLocalization()
      .UseStaticFiles()
      .UseRouting()
      .UseAuthentication()
      .UseAuthorization()
      .UseEndpoints(endpoints => { endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}"); }
      );
  }
}