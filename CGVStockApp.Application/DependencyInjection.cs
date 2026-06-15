using Microsoft.Extensions.DependencyInjection;

namespace CGVStockApp.Application;

public static class DependendyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}