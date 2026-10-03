using System;
using Contracts;
using GreenPipes;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Order.Service.Consumers;

namespace Order.Service;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddMassTransit(x =>
        {
            // 1. Set global endpoint naming convention
            x.SetKebabCaseEndpointNameFormatter();

            // 2. Discover consumers scoped to the Consumers namespace
            x.AddConsumersFromNamespaceContaining<OrderPlacedConsumer>();

            // 3. Register Request Client for Request-Response RPC pattern
            x.AddRequestClient<CheckOrderStatus>();

            // 4. Configure RabbitMQ Transport
            x.UsingRabbitMq((context, config) =>
            {
                var rabbitHost = Configuration.GetValue<string>("RabbitMq:Host") ?? "localhost";
                var rabbitUser = Configuration.GetValue<string>("RabbitMq:Username") ?? "guest";
                var rabbitPass = Configuration.GetValue<string>("RabbitMq:Password") ?? "guest";

                config.Host(rabbitHost, "/", c =>
                {
                    c.Username(rabbitUser);
                    c.Password(rabbitPass);
                });

                // Resilience: Retry policy
                config.UseMessageRetry(r =>
                {
                    r.Ignore<ArgumentException>();
                    r.Interval(3, TimeSpan.FromSeconds(2));
                });

                // Automatically configure receive endpoints for all registered consumers
                config.ConfigureEndpoints(context);
            });
        });

        services.AddMassTransitHostedService();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order.Service", Version = "v1" });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order.Service v1"));
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
