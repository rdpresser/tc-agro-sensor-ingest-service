using Microsoft.Extensions.DependencyInjection;

namespace TC.Agro.SensorIngest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
