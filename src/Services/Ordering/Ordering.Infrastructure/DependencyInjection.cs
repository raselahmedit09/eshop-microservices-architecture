using BuildingBlocks.Messaging.MassTransit;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Data;
using Ordering.Infrastructure.Data;
using Ordering.Infrastructure.Data.Interceptors;

namespace Ordering.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Database");

            // Add services to the container.
            services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
            services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();


            //// Add services to the container.
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseSqlServer(connectionString);
            });

            // Same instance as ApplicationDbContext, so handlers and the outbox save in one transaction
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

            // Transactional outbox: published messages are saved to OrderDb together with the order,
            // then delivered to RabbitMQ in the background, so an order is never saved without its event
            services.AddMessageBroker(configuration, typeof(Application.DependencyInjection).Assembly, config =>
            {
                config.AddEntityFrameworkOutbox<ApplicationDbContext>(outbox =>
                {
                    outbox.UseSqlServer();
                    outbox.UseBusOutbox();
                    outbox.QueryDelay = TimeSpan.FromSeconds(1);
                });

                config.AddConfigureEndpointsCallback((context, name, endpoint) =>
                {
                    // Each retry runs in a new transaction, so a duplicate that lost a race
                    // with the same order sees it on retry and is skipped by the consumer
                    endpoint.UseMessageRetry(retry => retry.Intervals(100, 500, 1000));
                    endpoint.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
                });
            });

            return services;
        }
    }
}
