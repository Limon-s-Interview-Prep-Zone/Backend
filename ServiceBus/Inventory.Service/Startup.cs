using System;
using GreenPipes;
using Inventory.Service.Consumers;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Inventory.Service;

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
            // 1. Set global endpoint naming formatter (kebab-case convention)
            x.SetKebabCaseEndpointNameFormatter();

            // 2. Discover consumers and their definitions scoped to the Consumers namespace
            x.AddConsumersFromNamespaceContaining<InventoryStockUpdateConsumer>();

            // 3. Configure RabbitMQ Transport
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitHost = Configuration.GetValue<string>("RabbitMq:Host") ?? "localhost";
                var rabbitUser = Configuration.GetValue<string>("RabbitMq:Username") ?? "guest";
                var rabbitPass = Configuration.GetValue<string>("RabbitMq:Password") ?? "guest";

                cfg.Host(rabbitHost, "/", c =>
                {
                    c.Username(rabbitUser);
                    c.Password(rabbitPass);
                });

                // Global fallback retry policy
                cfg.UseMessageRetry(r =>
                {
                    r.Ignore<ArgumentException>();
                    r.Interval(3, TimeSpan.FromSeconds(2));
                });

                // Automatically configures all endpoints using their respective ConsumerDefinitions
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddMassTransitHostedService();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Inventory.Service", Version = "v1" });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory.Service v1"));
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
