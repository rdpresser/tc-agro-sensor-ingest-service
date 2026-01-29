namespace TC.Agro.SensorIngest.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Repositories
            services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();
            services.AddScoped<ISensorReadingReadStore, SensorReadingReadStore>();

            // EF Core with Wolverine Integration
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

            // Transactional Outbox
            services.AddScoped<ITransactionalOutbox, SensorIngestOutbox>();

            SharedKernel.Infrastructure.DependencyInjection.AddAgroInfrastructure(services, configuration);

            return services;
        }
    }
}
