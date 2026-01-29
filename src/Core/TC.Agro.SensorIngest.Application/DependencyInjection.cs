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

            return services;
        }
    }
}
