using TC.Agro.SharedKernel.Infrastructure;
using TC.Agro.SensorIngest.Infrastructure.Options.Jobs;
using TC.Agro.SensorIngest.Infrastructure.Options.Wheater;

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

            // ============================================
            // Sensor Readings Job Configuration with Validation
            // ============================================
            services.AddOptions<SensorReadingsJobOptions>()
                .Bind(configuration.GetSection("Jobs:SensorReadings"))
                .Validate(o => o.IntervalSeconds > 0,
                    "Jobs:SensorReadings:IntervalSeconds must be greater than 0")
                .Validate(o => o.IntervalSeconds <= 3600,
                    "Jobs:SensorReadings:IntervalSeconds must not exceed 3600 seconds (1 hour)")
                .ValidateOnStart();

            // ============================================
            // Weather Provider Configuration with Validation
            // ============================================
            services.AddOptions<WeatherProviderOptions>()
                .Bind(configuration.GetSection("WeatherProvider"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl),
                    "WeatherProvider:BaseUrl is required")
                .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _),
                    "WeatherProvider:BaseUrl must be a valid absolute URI")
                .Validate(o => o.Latitude >= -90 && o.Latitude <= 90,
                    "WeatherProvider:Latitude must be between -90 and 90")
                .Validate(o => o.Longitude >= -180 && o.Longitude <= 180,
                    "WeatherProvider:Longitude must be between -180 and 180")
                .ValidateOnStart();

            // Register factory for easy access to options
            services.AddSingleton<SensorReadingsJobOptionsFactory>();
            services.AddSingleton<WeatherProviderOptionsFactory>();

            services.AddAgroInfrastructure(configuration);

            return services;
        }
    }
}
