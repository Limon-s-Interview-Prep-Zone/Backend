# MassTransit Fundamentals: RabbitMQ vs MassTransit (English & বাংলা)

---

## 1. RabbitMQ vs MassTransit: Why do we need MassTransit?
### English
* **RabbitMQ** is a **Message Broker**. It manages physical queues, exchanges, bindings, and delivery protocols (AMQP). When you write code using the raw `RabbitMQ.Client`, you are responsible for:
  * Manually serializing/deserializing payloads (JSON, byte arrays).
  * Manually declaring exchanges, queues, and routing keys.
  * Handling connection lifecycle, reconnects, thread safety, and channel pools.
  * Writing custom retry policies, dead-letter logic, and correlation tracking.
* **MassTransit** is a **Message Bus Framework / Distributed Application Framework** for .NET.
  * It acts as an abstraction layer on top of message brokers (RabbitMQ, Azure Service Bus, Amazon SQS, etc.).
  * It automates broker topology (creates exchanges and queues based on message types).
  * Provides enterprise patterns out-of-the-box: Retries, Redelivery, Transactional Outbox, Sagas/State Machines, OpenTelemetry distributed tracing, and dependency injection integration.

### বাংলা (Bangla)
* **RabbitMQ** হলো একটি **Message Broker**। এটি মেসেজ জমা রাখা, কিউ (Queue), এক্সচেঞ্জ (Exchange), এবং রাউটিং পরিচালনা করে। সরাসরি `RabbitMQ.Client` লাইব্রেরি ব্যবহার করলে আপনাকে নিজে হাতে:
  * JSON বা বাইট অ্যারেতে ডেটা Serialize/Deserialize করতে হয়।
  * কোডের মধ্যে Exchange ও Queue ডিক্লেয়ার এবং বাইন্ডিং করতে হয়।
  * কানেকশন ড্রপ হলে রিকানেকশন এবং চ্যানেল পুল ম্যানেজ করতে হয়।
  * ফেইল করলে রিট্রাই (Retry), ডেড-লেটার কিউ এবং ট্র্যাক করার কোড নিজ দায়িত্বে লিখতে হয়।
* **MassTransit** হলো একটি হাই-লেভেল **Distributed Application Framework (Message Bus)**।
  * এটি RabbitMQ বা Azure Service Bus-এর ওপর একটি আধুনিক অ্যাবস্ট্রাকশন লেয়ার।
  * মেসেজ টাইপ (C# class/record) দেখেই এটি স্বয়ংক্রিয়ভাবে RabbitMQ-তে Exchange তৈরি ও Queue বাইন্ড করে দেয়।
  * প্রোডাকশন লেভেলের ফিচার যেমন: Retry Mechanism, Circuit Breaker, Outbox Pattern, Saga/State Machine, OpenTelemetry Tracing ইত্যাদি একদম রেডিমেড পাওয়া যায়।

---

## 2. Core Concepts: Messages, Events vs Commands
### English
In MassTransit, messages are represented by standard .NET types (preferably immutable `record` or `interface`).

#### A. Events (`Publish`)
* **Definition**: An event indicates that something has already happened in the system (past tense).
* **Semantics**: 1-to-many (Publish / Subscribe). Broadcast notification.
* **Method**: `await publishEndpoint.Publish<OrderSubmitted>(new OrderSubmitted(...));`
* **RabbitMQ Topology**: MassTransit creates a `Fanout` exchange named after the message type's full name. Any consumer service subscribed to this event gets its own queue bound to this exchange.

#### B. Commands (`Send`)
* **Definition**: A command tells a specific service to perform an action (imperative tense).
* **Semantics**: 1-to-1 (Point-to-Point). Only one consumer handles the command.
* **Method**:
  ```csharp
  var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:process-payment"));
  await sendEndpoint.Send<ProcessPaymentCommand>(new ProcessPaymentCommand(...));
  ```
* **RabbitMQ Topology**: The message goes directly to the destination queue via direct exchange routing.

### বাংলা (Bangla)
MassTransit-এ মেসেজ তৈরি করা হয় C# `record` বা `interface` দিয়ে। মেসেজ সাধারণত দুই প্রকার:

#### ক. Events (`Publish`)
* **সংজ্ঞা**: অতীতে কোনো ঘটনা ঘটে গেছে তা বোঝাতে Event ব্যবহার করা হয় (যেমন: `OrderCreated`, `PaymentCompleted`)।
* **ধরন**: 1-to-many (Pub/Sub)। অর্থাৎ একটি ইভেন্ট একাধিক সার্ভিস বা কনজিউমার গ্রহণ করতে পারে।
* **কীভাবে পাঠানো হয়**: `IPublishEndpoint.Publish()` দিয়ে।
* **RabbitMQ-তে কী ঘটে**: MassTransit স্বয়ংক্রিয়ভাবে মেসেজের নামের একটি Fanout Exchange তৈরি করে। যে যে সার্ভিস এই মেসেজ শুনবে, তাদের Queue এই Exchange-এর সাথে কানেক্ট হয়ে যায়।

#### খ. Commands (`Send`)
* **সংজ্ঞা**: কোনো নির্দিষ্ট সার্ভিসকে কোনো নির্দিষ্ট কাজ করার নির্দেশ দেওয়া (যেমন: `SendEmailCommand`, `ProcessPaymentCommand`)।
* **ধরন**: 1-to-1 (Point-to-Point)। শুধুমাত্র একটি নির্দিষ্ট সার্ভিস এই কমান্ডটি এক্সিকিউট করবে।
* **কীভাবে পাঠানো হয়**: `ISendEndpoint.Send()` দিয়ে এবং নির্দিষ্ট কিউ-এর URI উল্লেখ করতে হয় (`queue:queue-name`)।
* **RabbitMQ-তে কী ঘটে**: মেসেজটি সরাসরি নির্দিষ্ট কিউ-তে চলে যায়।

---

## 3. Consumers & Endpoints
### English
* **Consumer**: A class implementing `IConsumer<TMessage>`. MassTransit creates an instance of the consumer per message within a DI scope (`IServiceProvider`).
  ```csharp
  public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
  {
      public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
      {
          var message = context.Message;
          // Process message
      }
  }
  ```
* **Receive Endpoint**: The physical queue receiver configuration. MassTransit allows fine-grained configuration for concurrency, prefetch count, and retries per endpoint.

### বাংলা (Bangla)
* **Consumer**: মেসেজ প্রসেস করার ক্লাস। এটি `IConsumer<T>` ইন্টারফেস ইমপ্লিমেন্ট করে।
  * MassTransit প্রতিটি মেসেজ প্রসেস করার সময় Dependency Injection (DI) স্কোপ তৈরি করে, ফলে scoped সার্ভিস (যেমন EF Core DbContext) খুব সহজে ইনজেক্ট করা যায়।
* **Receive Endpoint**: এটি মূলত একটি নির্দিষ্ট কিউ-এর কনফিগারেশন। কিউ থেকে কয়টি মেসেজ একসাথে আনা হবে (Prefetch), কনকারেন্টলি কয়টি প্রসেস হবে, তা এখানে ঠিক করা হয়।

---

## 4. Message Topology & Convention
### English
* MassTransit uses conventions based on .NET namespaces and class names.
* E.g., message `Ecommerce.Contracts.OrderSubmitted`:
  * Creates exchange: `Ecommerce.Contracts:OrderSubmitted` (Fanout).
  * Consumer queue: `order-service` or configured queue name.
  * Binds exchange $\rightarrow$ consumer exchange $\rightarrow$ consumer queue.
* Benefits: You never have to manually run `channel.QueueDeclare()` or `channel.ExchangeDeclare()`.

### বাংলা (Bangla)
* MassTransit ক্লাসের নাম এবং নেইমস্পেস অনুযায়ী RabbitMQ তে টপোলজি বানায়।
* উদাহরণস্বরূপ, `Ecommerce.Contracts.OrderSubmitted` ক্লাসের জন্য এটি `Ecommerce.Contracts:OrderSubmitted` নামের Exchange বানাবে এবং সাবস্ক্রাইবার কিউ-এর সাথে বাইন্ড করবে।
* সুবিধা: ডেভেলপারকে ম্যানুয়ালি RabbitMQ ড্যাশবোর্ডে গিয়ে বা কোডে এক্সচেঞ্জ বা কিউ ডিক্লেয়ার করার ঝামেলা পোহাতে হয় না।
