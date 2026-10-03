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

namespace Inventory.Service
{
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

            // STEP 1: Add MassTransit and register consumers
            services.AddMassTransit(x =>
            {
                // Register Consumers
                x.AddConsumer<InventoryManagementConsumer>();
                x.AddConsumer<InventoryStockUpdateConsumer>();
                x.AddConsumer<OrderPlacedInventoryConsumer>();

                // Configure RabbitMQ Transport
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    var rabbitHost = Configuration.GetValue<string>("RabbitMq:Host") ?? "localhost";
                    var rabbitUser = Configuration.GetValue<string>("RabbitMq:Username") ?? "guest";
                    var rabbitPass = Configuration.GetValue<string>("RabbitMq:Password") ?? "guest";

                    cfg.Host(rabbitHost, "/", c =>
                    {
                        c.Username(rabbitUser);
                        c.Password(rabbitPass);
                    });

                    // STEP 2: Configure Retry Policy (Resilience)
                    cfg.UseMessageRetry(r =>
                    {
                        r.Interval(3, TimeSpan.FromSeconds(2));
                    });

                    // Explicit receive endpoint for the point-to-point command queue
                    cfg.ReceiveEndpoint("inventory-stock-update", e =>
                    {
                        e.ConfigureConsumer<InventoryStockUpdateConsumer>(ctx);
                    });

                    // Automatically configure remaining endpoints (events)
                    cfg.ConfigureEndpoints(ctx);
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
}
