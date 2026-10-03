# MassTransit Advanced Architecture: Outbox, Sagas & Idempotency (English & বাংলা)

---

## 1. The Dual-Write Problem & Transactional Outbox
### English
* **The Problem**: In a distributed system, modifying a database and publishing a message are two separate operations.
  * If the database commit succeeds, but network fails before publishing the message → message is lost.
  * If the message is published, but the database transaction rolls back → phantom message / data corruption.
  * Distributed 2-Phase Commit (2PC) is slow, brittle, and not supported by modern cloud architectures.
* **The Solution: Transactional Outbox Pattern**:
  * Instead of publishing directly to RabbitMQ, MassTransit writes the outbound message to an `OutboxMessage` table inside the *same SQL database transaction* as your business entity.
  * Once the SQL transaction commits, a background worker publishes the messages to RabbitMQ asynchronously.
  * **Inbox Pattern**: Deduplicates incoming messages using an `InboxState` table to guarantee exactly-once processing semantics at application level.

```csharp
services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UsePostgres(); // or UseSqlServer()
        o.UseBusOutbox();
    });
});
```

### বাংলা (Bangla)
* **Dual-Write সমস্যা কী?**:
  * যখন আপনাকে একই সাথে ডাটাবেজে ডাটা সেভ করতে হয় এবং RabbitMQ-তে মেসেজ পাঠাতে হয়।
  * ডাটাবেজে সেভ হলো কিন্তু নেটওয়ার্কের কারণে মেসেজ পাবলিশ হলো না → মেসেজ ড্রপ এবং ইনকনসিস্টেন্সি।
  * মেসেজ চলে গেল কিন্তু ডাটাবেজ ট্রানজেকশন ফেইল করে রোলব্যাক করলো → ভুল তথ্যের মেসেজ ছড়িয়ে পড়লো।
* **সমাধান: Transactional Outbox Pattern**:
  * MassTransit সরাসরি RabbitMQ-তে মেসেজ না পাঠিয়ে, আপনার বিজনেস টেবিলের সাথে একই ডাটাবেজ ট্রানজেকশনে একটি `OutboxMessage` টেবিলে মেসেজটি সেভ করে।
  * ডাটাবেজ কমিট সফল হলে ব্যাকগ্রাউন্ড ওয়ার্কার ওই টেবিল থেকে মেসেজ নিয়ে ব্রোকারে পৌঁছে দেয়।
  * **Inbox Pattern**: কোনো কারণে ডুপ্লিকেট মেসেজ আসলে ডাটাবেজের `InboxState` চেক করে দ্বিতীয়বার একই মেসেজ প্রসেস করা বন্ধ করে (Idempotency নিশ্চিত করে)।

---

## 2. Sagas & State Machines (Automatonymous)
### English
* **What is a Saga?**: A pattern to manage long-running distributed transactions across microservices without distributed locks.
* **Choreography vs Orchestration**:
  * **Choreography**: Services react to each other's events directly without a central coordinator. (Becomes unmaintainable as workflows grow).
  * **Orchestration (MassTransit State Machine)**: A central Saga State Machine coordinates the entire workflow, manages state transitions, and triggers compensating actions if a step fails.
* **Saga Implementation**:
  ```csharp
  public class OrderState : SagaStateMachineInstance
  {
      public Guid CorrelationId { get; set; }
      public string CurrentState { get; set; }
      public Guid OrderId { get; set; }
      public decimal Amount { get; set; }
  }

  public class OrderStateMachine : MassTransitStateMachine<OrderState>
  {
      public State Submitted { get; private set; }
      public State Accepted { get; private set; }
      public State Canceled { get; private set; }

      public Event<OrderSubmitted> OrderSubmittedEvent { get; private set; }
      public Event<PaymentFailed> PaymentFailedEvent { get; private set; }

      public OrderStateMachine()
      {
          InstanceState(x => x.CurrentState);

          Initially(
              When(OrderSubmittedEvent)
                  .Then(context => context.Saga.OrderId = context.Message.OrderId)
                  .TransitionTo(Submitted)
                  .Publish(context => new ProcessPayment(context.Saga.OrderId))
          );

          During(Submitted,
              When(PaymentFailedEvent)
                  .TransitionTo(Canceled)
                  .Publish(context => new CancelOrder(context.Saga.OrderId))
          );
      }
  }
  ```

### বাংলা (Bangla)
* **Saga কী?**: ডিস্ট্রিবিউটেড মাইক্রোসার্ভিসে দীর্ঘমেয়াদী ট্রানজেকশন (Long-running process) ম্যানেজ করার ডিজাইন প্যাটার্ন। যেমন: অর্ডার প্লেস → পেমেন্ট কাটা → ইনভেন্টরি স্টক আপডেট → ডেলিভারি বুকিং।
* **Orchestration বনাম Choreography**:
  * **Choreography**: প্রতিটি সার্ভিস ইভেন্ট শুনে নিজেই সিদ্ধান্ত নেয়। সিস্টেম জটিল হলে কে কার উপর নির্ভর করছে তা ট্র্যাক করা অসম্ভব হয়ে যায়।
  * **Orchestration (State Machine)**: একজন সেন্ট্রাল ডিরেক্টর বা স্টেট মেশিন থাকে, যে পুরো প্রসেসের অবস্থা (State) ট্র্যাক করে এবং কোনো স্টেপ ফেইল করলে পূর্বের কাজগুলো রোলব্যাক বা ক্ষতিপূরণ (Compensate) করে।
* **MassTransit State Machine**:
  * এটি C#-এর ফ্লুয়েন্ট সিনট্যাক্স দিয়ে ডিস্ট্রিবিউটেড ওয়ার্কফ্লো তৈরি করতে দেয়।
  * স্টেট মেশিনের ডেটা ডাটাবেজে (EF Core / Redis / Mongo) স্বয়ংক্রিয়ভাবে পারসিস্ট হয়।

---

## 3. Idempotency (আইডেমপোটেন্সি)
### English
* Distributed messaging follows **At-Least-Once Delivery**. A consumer may receive the exact same message twice due to network retries or broker rebalances.
* **Best Practices**:
  1. Use unique `CorrelationId` or `MessageId` on every message.
  2. Implement idempotent operations in the database (e.g., `UPSERT`, conditional state check: `WHERE Status = 'Pending'`).
  3. Enable MassTransit Inbox outbox filter to reject duplicate `MessageId`s.

### বাংলা (Bangla)
* ডিস্ট্রিবিউটেড মেসেজিংয়ের মূল নিয়ম: মেসেজ অন্তত একবার পৌঁছাবে (**At-Least-Once Delivery**)। নেটওয়ার্ক রিট্রাইয়ের কারণে একই মেসেজ ২ বার বা ততোধিক বার কনজিউমারে আসতে পারে।
* আপনার কনজিউমার এমনভাবে ডিজাইন করতে হবে যাতে একই মেসেজ ১০ বার আসলেও ডাটাবেজে ডুপ্লিকেট ডাটা তৈরি না হয় বা ইউজারের অ্যাকাউন্ট থেকে দুইবার টাকা না কাটে।
* সমাধান: প্রতিটি মেসেজের সাথে ইউনিক `CorrelationId` বা `MessageId` ব্যবহার করা এবং MassTransit Inbox প্যাটার্ন বা ডাটাবেজে ইউনিক কনস্ট্রেইন্ট ব্যবহার করা।
