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

namespace Order.Service
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

            // STEP 1: Add and configure MassTransit
            services.AddMassTransit(x =>
            {
                // Register Consumers
                x.AddConsumer<OrderPlacedConsumer>();
                x.AddConsumer<CheckOrderStatusConsumer>();

                // Register Request Client for Request-Response RPC pattern
                x.AddRequestClient<CheckOrderStatus>();

                // Configure RabbitMQ Transport
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

                    // STEP 2: Configure Retry Policy (Resilience)
                    config.UseMessageRetry(r =>
                    {
                        // Retry 3 times with 2 seconds interval between retries
                        r.Interval(3, TimeSpan.FromSeconds(2));
                    });

                    // Automatically configure receive endpoints for all registered consumers
                    config.ConfigureEndpoints(context);
                });
            });

            // Hosted service to start/stop the bus with ASP.NET Core lifecycle
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
}
