using System;
using System.Collections.Generic;
using Contracts;
using GreenPipes;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Notification.Service.Consumers;
using RabbitMQ.Client;

namespace Notification.Service;

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
            x.SetKebabCaseEndpointNameFormatter();

            // Register consumers
            x.AddConsumer<OrderPlacedNotificationConsumer>();
            x.AddConsumer<EmailNotificationConsumer>();
            x.AddConsumer<SmsNotificationConsumer>();
            x.AddConsumer<FraudDetectionConsumer>();
            x.AddConsumer<AnalyticsPaymentConsumer>();
            x.AddConsumer<EnterpriseDocumentConsumer>();

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

                cfg.UseMessageRetry(r =>
                {
                    r.Ignore<ArgumentException>();
                    r.Interval(3, TimeSpan.FromSeconds(2));
                });

                // ====================================================================
                // 1. DIRECT EXCHANGE: Exact routing key matching ("email" or "sms")
                // ====================================================================
                cfg.ReceiveEndpoint("notification-email-queue", e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind<SendNotificationEvent>(b =>
                    {
                        b.ExchangeType = ExchangeType.Direct;
                        b.RoutingKey = "email";
                    });
                    e.ConfigureConsumer<EmailNotificationConsumer>(ctx);
                });

                cfg.ReceiveEndpoint("notification-sms-queue", e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind<SendNotificationEvent>(b =>
                    {
                        b.ExchangeType = ExchangeType.Direct;
                        b.RoutingKey = "sms";
                    });
                    e.ConfigureConsumer<SmsNotificationConsumer>(ctx);
                });

                // ====================================================================
                // 2. TOPIC EXCHANGE: Pattern matching with wildcards (* and #)
                // ====================================================================
                cfg.ReceiveEndpoint("fraud-detection-queue", e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind<PaymentProcessedEvent>(b =>
                    {
                        b.ExchangeType = ExchangeType.Topic;
                        b.RoutingKey = "payment.*.failed";
                    });
                    e.ConfigureConsumer<FraudDetectionConsumer>(ctx);
                });

                cfg.ReceiveEndpoint("payment-analytics-queue", e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind<PaymentProcessedEvent>(b =>
                    {
                        b.ExchangeType = ExchangeType.Topic;
                        b.RoutingKey = "payment.#";
                    });
                    e.ConfigureConsumer<AnalyticsPaymentConsumer>(ctx);
                });

                // ====================================================================
                // 3. HEADERS EXCHANGE: Header-attribute matching (tier = enterprise)
                // ====================================================================
                cfg.ReceiveEndpoint("enterprise-document-queue", e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind<DocumentProcessedEvent>(b =>
                    {
                        b.ExchangeType = ExchangeType.Headers;
                        b.SetBindingArgument("tier", "enterprise");
                        b.SetBindingArgument("x-match", "all");
                    });
                    e.ConfigureConsumer<EnterpriseDocumentConsumer>(ctx);
                });

                // ====================================================================
                // 4. FANOUT EXCHANGE: Automatically configures remaining consumers (OrderPlaced)
                // ====================================================================
                cfg.ConfigureEndpoints(ctx);
            });
        });

        services.AddMassTransitHostedService();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notification.Service", Version = "v1" });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification.Service v1"));
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
