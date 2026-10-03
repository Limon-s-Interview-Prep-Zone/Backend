using System;

namespace Contracts;

/// <summary>
/// Headers Exchange Contract
/// Used for attribute-based routing using AMQP message headers (e.g., tier: "enterprise", department: "finance")
/// </summary>
public record DocumentProcessedEvent(Guid DocumentId, string FileName, string FileType, string Department);
