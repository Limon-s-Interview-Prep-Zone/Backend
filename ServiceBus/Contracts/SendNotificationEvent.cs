using System;

namespace Contracts;

/// <summary>
/// Direct Exchange Contract
/// Used for targeted routing using exact routing keys: "email" or "sms"
/// </summary>
public record SendNotificationEvent(Guid NotificationId, string Recipient, string Content, string Channel);
