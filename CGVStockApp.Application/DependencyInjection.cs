using CGVStockApp.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CGVStockApp.Application;

public static class DependendyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient( typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient( typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient( typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}