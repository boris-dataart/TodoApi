using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using TodoApi.Application.Common.Interfaces;
using TodoApi.Domain.Constants;
using TodoApi.Infrastructure.Data;
using TodoApi.Infrastructure.Data.Interceptors;
using TodoApi.Infrastructure.Identity;
using Unleash;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var shadowConnectionString = configuration.GetConnectionString("ShadowConnection");

        Guard.Against.Null(connectionString, message: "Connection string 'DefaultConnection' not found.");
        Guard.Against.Null(shadowConnectionString, message: "Connection string 'ShadowConnection' not found.");

        services.AddScoped<IInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<IInterceptor, DispatchDomainEventsInterceptor>();
        services.AddScoped<IInterceptor, ShadowCommandInterceptor>();
        services.AddScoped<IInterceptor, ShadowQueryInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<IInterceptor>());

            options.UseMySQL(connectionString);
        });
        services.AddDbContext<ShadowDbContext>(options => options.UseMySQL(shadowConnectionString));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ApplicationDbContextInitialiser>();

        services.AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme);

        services.AddAuthorizationBuilder();

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IIdentityService, IdentityService>();

        services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));

        var unleashApiUrl = configuration.GetValue<string>("UnleashConfig:ApiUrl");
        Guard.Against.Null(connectionString, message: "Connection string 'UnleashApiUrl' not found.");

        var unleashToken = configuration.GetValue<string>("UnleashConfig:Token");
        Guard.Against.Null(connectionString, message: "Connection string 'UnleashToken' not found.");

        var unleashSettings = new UnleashSettings()
        {
            AppName = configuration.GetValue<string>("UnleashConfig:AppName"),
            UnleashApi = new Uri(unleashApiUrl!),
            CustomHttpHeaders = new Dictionary<string, string>() { {HeaderNames.Authorization, unleashToken! } }
        };

        services.AddSingleton<IUnleash>(c => new DefaultUnleash(unleashSettings));

        return services;
    }
}
