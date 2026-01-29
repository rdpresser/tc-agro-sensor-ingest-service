using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using TC.Agro.SensorIngest.Infrastructure.Persistence;
using TC.Agro.SharedKernel.Infrastructure.Database;
using Wolverine.EntityFrameworkCore;

namespace TC.Agro.SensorIngest.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextWithWolverineIntegration<ApplicationDbContext>((sp, opts) =>
        {
            var dbFactory = sp.GetRequiredService<DbConnectionFactory>();

            opts.UseNpgsql(dbFactory.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, DefaultSchemas.Default);
            });

            opts.UseSnakeCaseNamingConvention();
            opts.LogTo(Log.Logger.Information, LogLevel.Information);

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                opts.EnableSensitiveDataLogging(true);
                opts.EnableDetailedErrors();
            }
        });

        SharedKernel.Infrastructure.DependencyInjection.AddAgroInfrastructure(services, configuration);

        return services;
    }
}
