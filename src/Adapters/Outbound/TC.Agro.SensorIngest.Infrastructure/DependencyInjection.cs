using TC.Agro.SharedKernel.Infrastructure;

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
            services.AddScoped<ISensorAggregateRepository, SensorAggregateRepository>();
            services.AddScoped<ISensorReadStore, SensorReadStore>();
            services.AddScoped<IAlertAggregateRepository, AlertAggregateRepository>();
            services.AddScoped<IAlertReadStore, AlertReadStore>();

            // Owner snapshot store
            services.AddScoped<IOwnerSnapshotStore, OwnerSnapshotStore>();

            // Sensor snapshot store
            services.AddScoped<ISensorSnapshotStore, SensorSnapshotStore>();

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

            // IApplicationDbContext (required for SharedKernel ApplyMigrations)
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

            // Unit of Work (for simple handlers that don't need outbox)
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

            // Transactional Outbox
            services.AddScoped<ITransactionalOutbox, SensorIngestOutbox>();

            services.AddAgroInfrastructure(configuration);

            return services;
        }
    }
}
