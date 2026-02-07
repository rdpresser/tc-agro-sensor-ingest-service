namespace TC.Agro.SensorIngest.Application
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // FluentValidation validators
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            // Handlers
            services.AddScoped<CreateBatchReadingsCommandHandler>();
            services.AddScoped<GetLatestReadingsQueryHandler>();
            services.AddScoped<GetSensorListQueryHandler>();
            services.AddScoped<GetReadingsHistoryQueryHandler>();
            services.AddScoped<GetAlertListQueryHandler>();
            services.AddScoped<ResolveAlertCommandHandler>();
            services.AddScoped<GetDashboardStatsQueryHandler>();

            return services;
        }
    }
}
