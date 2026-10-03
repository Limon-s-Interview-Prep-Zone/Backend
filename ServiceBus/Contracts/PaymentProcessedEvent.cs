using System;

namespace Contracts;

/// <summary>
/// Topic Exchange Contract
/// Used for pattern-matched routing using routing keys like: "payment.card.success", "payment.paypal.failed"
/// </summary>
public record PaymentProcessedEvent(Guid PaymentId, int OrderId, decimal Amount, string PaymentMethod, string Status);
