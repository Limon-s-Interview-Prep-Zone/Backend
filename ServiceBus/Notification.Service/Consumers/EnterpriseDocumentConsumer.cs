using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Service.Consumers;

/// <summary>
/// Headers Exchange Consumer
/// Bound with AMQP header criteria: "tier" = "enterprise"
/// Routes priority processing based on message attributes rather than routing keys.
/// </summary>
public class EnterpriseDocumentConsumer : IConsumer<DocumentProcessedEvent>
{
    private readonly ILogger<EnterpriseDocumentConsumer> _logger;

    public EnterpriseDocumentConsumer(ILogger<EnterpriseDocumentConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DocumentProcessedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[HEADERS EXCHANGE -> ENTERPRISE TIER] Processing priority document: {FileName} ({FileType}) for Department: {Dept}",
            msg.FileName, msg.FileType, msg.Department);

        await Task.CompletedTask;
    }
}
