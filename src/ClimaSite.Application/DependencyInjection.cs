using System.Reflection;
using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Mappings;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Application.Features.Products.Scoring;
using ClimaSite.Application.Features.Wishlist.Services;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Mapster
        var config = MappingConfig.GetConfiguration();
        services.AddSingleton(config);

        // Scoring services
        services.AddScoped<RecommendationScoringService>();
        services.AddScoped<WishlistApplicationService>();

        // Email outbox (ARCH-05): enqueue + drain. The hosted polling shell lives in the API layer.
        services.AddScoped<IEmailOutbox, EmailOutbox>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();

        return services;
    }
}
