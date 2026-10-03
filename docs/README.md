# MassTransit Documentation Guide (English & বাংলা)

Welcome to the comprehensive MassTransit theoretical and architectural documentation.

## Documentation Index

1. [01. MassTransit Fundamentals & RabbitMQ Comparison](file:///Users/limon/interview/Backend/docs/01-masstransit-fundamentals.md)
   - RabbitMQ vs MassTransit
   - Events vs Commands (`Publish` vs `Send`)
   - Consumers, Contracts, and Endpoints
   - Broker Topology & Naming Conventions

2. [02. Resilience & Error Handling](file:///Users/limon/interview/Backend/docs/02-resilience-and-error-handling.md)
   - `_error` and `_skipped` Queues
   - Retry Strategies (Immediate, Fixed, Exponential Backoff)
   - Redelivery vs Retry (Delayed processing)
   - High-throughput tuning: `PrefetchCount` & `ConcurrentMessageLimit`

3. [03. Advanced Patterns, Saga & Outbox](file:///Users/limon/interview/Backend/docs/03-advanced-patterns-and-saga.md)
   - The Dual-Write Problem & Transactional Outbox Pattern
   - Saga & State Machines (Automatonymous)
   - Orchestration vs Choreography
   - Idempotency & Message Deduplication

4. [04. Common Interview Questions & Answers](file:///Users/limon/interview/Backend/docs/04-interview-questions-and-answers.md)
   - RabbitMQ vs MassTransit benefits
   - Publish vs Send mechanics
   - Fault handling, retries, `_error` & `_skipped`
   - Dual-Write problem & Transactional Outbox
   - Sagas & Orchestration
   - Consumer Idempotency & Deduping
   - PrefetchCount & ConcurrentMessageLimit tuning
   - Testing with `ITestHarness`

5. [05. ServiceBus Step-by-Step Implementation Guide](file:///Users/limon/interview/Backend/docs/05-servicebus-step-by-step-implementation.md)
   - Architectural flow diagram
   - Contracts (`Contracts/`)
   - Order.Service configuration (`Startup.cs` & `OrdersController.cs`)
   - Inventory.Service configuration (`Startup.cs` & consumers)
   - Testing Pub/Sub, Commands, and RPC step-by-step

6. [06. All RabbitMQ Exchange Types in MassTransit Guide](file:///Users/limon/interview/Backend/docs/06-all-rabbitmq-exchange-types-in-masstransit.md)
   - Architectural matrix of all 4 exchange types
   - Fanout Exchange (Global Broadcast)
   - Direct Exchange (Exact RoutingKey matching for SMS/Email)
   - Topic Exchange (Pattern matching with `*` and `#` wildcards for Payment/Fraud)
   - Headers Exchange (AMQP Header attribute routing for Enterprise tiers)
