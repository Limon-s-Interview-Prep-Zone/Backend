# MassTransit Interview Questions & Answers (English & বাংলা)

A curated collection of mid-to-senior level interview questions and answers for .NET engineers working with MassTransit and RabbitMQ.

---

### Q1: What is MassTransit, and why should we use it over the raw `RabbitMQ.Client`?
#### English
* **Answer**: MassTransit is a message bus framework that provides a developer-friendly abstraction over message brokers like RabbitMQ, Azure Service Bus, and Amazon SQS.
* **Why use it over raw RabbitMQ.Client**:
  1. **Automatic Topology Management**: Automatically declares exchanges, queues, and routing bindings based on message contracts.
  2. **Built-in Resilience**: Out-of-the-box support for retry policies (immediate, interval, exponential backoff), circuit breakers, and fault queues (`_error`, `_skipped`).
  3. **Enterprise Patterns**: Native support for the **Transactional Outbox**, **Sagas / State Machines**, and delayed message scheduling.
  4. **Production Observability**: Built-in OpenTelemetry tracing, metrics, and health check integrations.
  5. **ASP.NET Core Integration**: Clean dependency injection scoping per consumed message.

#### বাংলা (Bangla)
* **উত্তর**: MassTransit হলো .NET-এর একটি Message Bus Framework, যা RabbitMQ, Azure Service Bus ইত্যাদির ওপর একটি হাই-লেভেল অ্যাবস্ট্রাকশন লেয়ার হিসেবে কাজ করে।
* **সরাসরি RabbitMQ.Client-এর চেয়ে কেন MassTransit ব্যবহার করবেন?**:
  1. **স্বয়ংক্রিয় টপোলজি (Automatic Topology)**: কোডে ক্লাস/রেকর্ড তৈরি করলেই MassTransit নিজে থেকে RabbitMQ-তে Exchange তৈরি করে এবং Queue বাইন্ড করে দেয়।
  2. **বিল্ট-ইন রেজিলিয়েন্স (Resilience)**: এক্সপোনেনশিয়াল রিট্রাই (Exponential Backoff), সার্কিট ব্রেকার এবং এরর হ্যান্ডলিং রেডিমেড পাওয়া যায়।
  3. **অ্যাডভান্সড প্যাটার্নস**: Transactional Outbox Pattern এবং Saga State Machine ইমপ্লিমেন্ট করা অত্যন্ত সহজ।
  4. **অবজার্ভেবিলিটি (Observability)**: OpenTelemetry ডিস্ট্রিবিউটেড ট্রেসিং এবং মেট্রিক্স সরাসরি সাপোর্ট করে।
  5. **DI স্কোপিং**: প্রতিটি মেসেজের জন্য আলাদা Scoped ServiceProvider হ্যান্ডেল করে (যেমন EF Core DbContext)।

---

### Q2: What is the architectural difference between `Publish` and `Send`?
#### English
* **`Publish` (Events)**:
  * Implements **Publish-Subscribe (1-to-many)**.
  * Message contract represents an event that already occurred (e.g., `OrderPlaced`).
  * Published to a `fanout` exchange named after the message type. Any service subscribed gets a copy in its queue.
  * Sender does not know or care who receives it.
* **`Send` (Commands)**:
  * Implements **Point-to-Point (1-to-1)**.
  * Message contract represents an instruction to perform an action (e.g., `ProcessPaymentCommand`).
  * Sent directly to a destination endpoint URI (`queue:process-payment`).
  * Only one consumer handles the command.

#### বাংলা (Bangla)
* **`Publish` (ইভেন্টস)**:
  * এটি **Pub/Sub (১-থেকে-অনেকে)** প্যাটার্ন।
  * এমন ঘটনা বোঝায় যা অতীতে ঘটে গেছে (যেমন: `OrderCreated`)।
  * মেসেজটি Fanout Exchange-এ যায় এবং যতগুলো সার্ভিস সাবস্ক্রাইব করা আছে সবাই মেসেজটির একটি করে কপি পায়।
  * সেন্ডার জানে না কারা এই মেসেজ রিসিভ করবে।
* **`Send` (কমান্ডস)**:
  * এটি **Point-to-Point (১-থেকে-১)** প্যাটার্ন।
  * কোনো নির্দিষ্ট সার্ভিসকে কোনো কাজ করার নির্দেশ দেওয়া হয় (যেমন: `ProcessPaymentCommand`)।
  * মেসেজটি সরাসরি নির্দিষ্ট কিউ-এর অ্যাড্রেসে (`queue:process-payment`) পাঠানো হয়।
  * শুধুমাত্র একটি কনজিউমার এই কমান্ডটি এক্সিকিউট করবে।

---

### Q3: What happens when a consumer throws an unhandled exception in MassTransit?
#### English
* **Answer**:
  1. **In-Memory Retry**: If a retry policy is configured (e.g., exponential backoff), MassTransit retries processing on the current thread without acking the message to the broker.
  2. **Fault Notification**: If all retries fail, MassTransit publishes a `Fault<TMessage>` event that other monitoring or compensation services can subscribe to.
  3. **Dead-Lettering (`_error` queue)**: MassTransit moves the failed message to a dedicated queue named `<queue_name>_error` and acknowledges the original message from the primary queue.
  4. **Diagnostic Headers**: Rich diagnostics (`MT-Fault-Message`, `MT-Fault-StackTrace`, `MT-Fault-Timestamp`) are embedded in the message headers in the error queue.
  5. **`_skipped` queue**: If a message arrives for which no consumer is registered, it is moved to `<queue_name>_skipped` to avoid message loss.

#### বাংলা (Bangla)
* **উত্তর**:
  1. **ইন-মেমোরি রিট্রাই**: রিট্রাই পলিসি সেট করা থাকলে (যেমন ৩ বার চেষ্টা করা), MassTransit মেসেজ একনলেজ না করে পুনরায় এক্সিকিউট করার চেষ্টা করে।
  2. **ফল্ট ইভেন্ট (Fault Event)**: সব রিট্রাই ফেইল করলে MassTransit একটি `Fault<T>` ইভেন্ট পাবলিশ করে, যা অন্য কোনো সার্ভিস শুনতে পারে।
  3. **`_error` কিউ-তে ট্রান্সফার**: মেসেজটি প্রাইমারি কিউ থেকে সরিয়ে `<queue_name>_error` কিউ-তে রেখে দেয়, যাতে অন্য মেসেজ ব্লক না হয়।
  4. **হেডারে এরর বিবরণ**: এরর মেসেজ এবং সম্পূর্ণ স্ট্যাক ট্রেস (Stack Trace) হেডারে যুক্ত করে দেওয়া হয়।
  5. **`_skipped` কিউ**: কিউ-তে কোনো মেসেজ আসলে যার জন্য সিস্টেমে কোনো Consumer কনফিগার করা নেই, মেসেজটি যেন হারিয়ে না যায় সেজন্য `<queue_name>_skipped` কিউ-তে পাঠিয়ে দেওয়া হয়।

---

### Q4: What is the Dual-Write Problem, and how does the Transactional Outbox pattern solve it?
#### English
* **The Problem**: A service needs to update a database (e.g., save an order) and publish a message to a broker (e.g., `OrderPlaced`). If the database commit succeeds but the message broker is temporarily unreachable, the message is lost, causing data inconsistency.
* **MassTransit Solution**:
  * With `AddEntityFrameworkOutbox<TDbContext>`, MassTransit writes outbound messages to an `OutboxMessage` table *inside the same SQL transaction* as the business data change.
  * If the database commit succeeds, both your business entity and the outgoing messages are guaranteed to be saved.
  * A background delivery service (`BusOutboxNotification` or timer) reads the messages from the outbox table and dispatches them to RabbitMQ asynchronously.
  * MassTransit also includes an **Inbox Pattern** that stores processed `MessageId`s to prevent duplicate consumer executions (at-least-once deduplication).

#### বাংলা (Bangla)
* **সমস্যা (Dual-Write Problem)**: একটি এপিআই-তে একই সাথে ডাটাবেজে ডাটা সেভ করতে হবে এবং ব্রোকারে মেসেজ পাঠাতে হবে। ডাটাবেজে সেভ হওয়ার পর নেটওয়ার্কের কারণে ব্রোকারে মেসেজ যেতে ব্যর্থ হলে পুরো সিস্টেমে ইনকনসিস্টেন্সি তৈরি হয়।
* **MassTransit Outbox-এর সমাধান**:
  * MassTransit সরাসরি ব্রোকারে মেসেজ না পাঠিয়ে আপনার ডাটাবেজের একই SQL ট্রানজেকশনে `OutboxMessage` টেবিলে মেসেজটি লিখে রাখে।
  * ডাটাবেজ সফলভাবে কমিট হলে মেসেজটি হারানো অসম্ভব।
  * এরপর একটি ব্যাকগ্রাউন্ড সার্ভিস ওই টেবিল থেকে মেসেজ নিয়ে RabbitMQ-তে নিরাপদে পৌঁছে দেয়।
  * একইভাবে **Inbox Pattern** ব্যবহারের মাধ্যমে পূর্বে প্রসেস করা মেসেজ আইডি ট্র্যাক করে ডুপ্লিকেট মেসেজ দ্বিতীয়বার এক্সিকিউট হওয়া ঠেকায়।

---

### Q5: What is a Saga in MassTransit, and how do Orchestration and Choreography compare?
#### English
* **Saga**: A design pattern for managing long-running, distributed transactions across multiple microservices without using distributed 2PC locks.
* **Choreography**: Each service listens to events from other services and decides its next action independently. Hard to track and debug when workflows grow beyond 2–3 services.
* **Orchestration (MassTransit State Machine)**: A central state machine (`MassTransitStateMachine<TSagaState>`) coordinates the entire workflow:
  * Tracks the current saga state in a database (EF Core, Redis, Mongo).
  * Listens to events, transitions between states, and dispatches commands.
  * Coordinates compensation (e.g., if payment fails, publishes a command to release reserved stock).

#### বাংলা (Bangla)
* **Saga**: মাইক্রোসার্ভিসে একাধিক সার্ভিসের মধ্যে জটিল ও দীর্ঘমেয়াদী ট্রানজেকশন সমন্বয় করার ডিজাইন প্যাটার্ন (যেমন: ই-কমার্সে অর্ডার → স্টক বুকিং → পেমেন্ট → শিপিং)।
* **Choreography বনাম Orchestration**:
  * **Choreography**: প্রতিটি সার্ভিস একে অপরের ইভেন্ট শুনে নিজেই সিদ্ধান্ত নেয়। সিস্টেম বড় হলে কে কখন কী করছে ট্র্যাক করা অসম্ভব হয়ে যায়।
  * **Orchestration (State Machine)**: একটি সেন্ট্রাল স্টেট মেশিন পুরো ওয়ার্কফ্লো নিয়ন্ত্রণ করে।
    * MassTransit-এর `MassTransitStateMachine` দিয়ে এটি কোড করা হয়।
    * বর্তমান অবস্থা (State) ডাটাবেজে সেভ থাকে।
    * কোনো স্টেপ ফেইল করলে (যেমন পেমেন্ট ফেইল) স্বয়ংক্রিয়ভাবে আগের কাজগুলো রোলব্যাক বা ক্ষতিপূরণ (Compensate) করে।

---

### Q6: How do you guarantee Idempotency in a MassTransit consumer?
#### English
* **Context**: Distributed brokers guarantee **At-Least-Once Delivery**. Network disconnects or retries can deliver the same message multiple times.
* **Techniques**:
  1. **MassTransit Consumer Outbox / Inbox**: Tracks processed `MessageId` in the database within the consumer's execution transaction. If the same `MessageId` is seen again, processing is skipped.
  2. **Database-level Idempotency**: Use unique business keys (e.g., `OrderId` or `PaymentTransactionId`) with database unique constraints or `UPSERT` queries.
  3. **State Verification**: Check current state before modifying (e.g., `UPDATE Orders SET Status = 'Paid' WHERE Id = @Id AND Status = 'Pending'`).

#### বাংলা (Bangla)
* **প্রেক্ষাপট**: ডিস্ট্রিবিউটেড মেসেজিংয়ে **At-Least-Once Delivery** প্রযোজ্য। নেটওয়ার্ক গ্লিচের কারণে একই মেসেজ একাধিকবার কনজিউমারে পৌঁছাতে পারে।
* **আইডেমপোটেন্সি নিশ্চিত করার উপায়**:
  1. **MassTransit Inbox Filter**: মেসেজের ইউনিক `MessageId` ডাটাবেজের ইনবক্স টেবিলে সেভ রাখে। একই আইডি দ্বিতীয়বার আসলে কাজ না করে স্কিপ করে দেয়।
  2. **ডাটাবেজ ইউনিক কনস্ট্রেইন্ট**: বিজনেজ কী (যেমন `PaymentReferenceId`) ডাটাবেজে Unique Constraint দিয়ে রাখা।
  3. **স্টেট ভ্যালিডেশন**: আপডেট করার আগে চেক করা (যেমন: অর্ডার স্ট্যাটাস যদি আগেই 'Paid' থাকে, তবে পুনরায় টাকা না কেটে স্কিপ করা)।

---

### Q7: How do you tune `PrefetchCount` and `ConcurrentMessageLimit` for high throughput vs resource safety?
#### English
* **`PrefetchCount`**: How many unacknowledged messages RabbitMQ pushes to the consumer's memory buffer in advance.
* **`ConcurrentMessageLimit`**: How many messages the consumer executes in parallel on worker threads.
* **Tuning Rules**:
  * **Rule of Thumb**: `PrefetchCount >= ConcurrentMessageLimit`. (Typically 2x `ConcurrentMessageLimit`).
  * **Fast CPU-bound or lightweight I/O tasks**: Higher values (e.g., Prefetch: 100, Concurrency: 50) for maximum throughput.
  * **Heavy / Slow tasks (e.g., generating PDFs, complex video rendering)**: Low values (e.g., Prefetch: 1–5, Concurrency: 1–2) to prevent one consumer instance from hoarding all messages while other nodes sit idle.

#### বাংলা (Bangla)
* **`PrefetchCount`**: RabbitMQ থেকে একবারে কতগুলো মেসেজ ক্লায়েন্টের মেমোরি বাফারে এনে রাখা হবে।
* **`ConcurrentMessageLimit`**: লোকাল মেশিনে একই সাথে সমান্তরালভাবে (Parallel) কয়টি মেসেজ এক্সিকিউট হবে।
* **টিউনিং নিয়ম**:
  * সাধারণ নিয়ম: `PrefetchCount` সবসময় `ConcurrentMessageLimit`-এর চেয়ে বেশি বা দ্বিগুণ রাখা উচিত।
  * **দ্রুত ও হালকা কাজের জন্য**: বেশি মান দেওয়া যায় (যেমন Prefetch: 50, Concurrency: 25), এতে হাই-থ্রুপুট পাওয়া যায়।
  * **ভারী ও সময়সাপেক্ষ কাজের জন্য (যেমন রিপোর্ট জেনারেশন)**: মান খুব কম রাখা উচিত (যেমন Prefetch: 2, Concurrency: 1), যাতে একটি সার্ভার একা সব মেসেজ আটকে রেখে অন্য সার্ভারগুলোকে অলস বসিয়ে না রাখে।

---

### Q8: How do you unit test MassTransit consumers and Sagas without running Docker or a real RabbitMQ instance?
#### English
* **Answer**: MassTransit provides `MassTransit.Testing` containing the **`ITestHarness`**.
* **Key capabilities**:
  * Runs completely in-memory inside the test runner process.
  * Supports consumer verification: `Assert.True(await harness.Consumed.Any<OrderSubmitted>());`
  * Supports published message assertions: `Assert.True(await harness.Published.Any<OrderCompleted>());`
  * Tests retry configurations and Sagas deterministically in milliseconds without network overhead.

#### বাংলা (Bangla)
* **উত্তর**: MassTransit-এর অফিসিয়াল টেস্টিং প্যাকেজ `MassTransit.Testing`-এ থাকা **`ITestHarness`** ব্যবহার করে।
* **সুবিধা**:
  * কোনো ডকার (Docker) বা রিয়েল RabbitMQ ইনস্ট্যান্স ছাড়াই মেমোরিতে টেস্ট চলে।
  * মেসেজ ঠিকমতো কনজিউম হয়েছে কিনা (`harness.Consumed.Any<T>()`) বা নতুন ইভেন্ট পাবলিশ হয়েছে কিনা তা কয়েক মিলিসেকেন্ডের মধ্যে অ্যাসার্ট (Assert) করা যায়।
