# All RabbitMQ Exchange Types in MassTransit (English & বাংলা)

A complete architectural guide and real-world reference implementation for all RabbitMQ Exchange types using MassTransit in .NET 6:
1. **Fanout Exchange** (Global Broadcast)
2. **Direct Exchange** (Exact Routing Key Matching)
3. **Topic Exchange** (Pattern Matching with `*` and `#` Wildcards)
4. **Headers Exchange** (Attribute/Metadata-based Routing)

---

## Architecture Matrix (এক্সচেঞ্জ প্রকারভেদ ও তুলনা)

| Exchange Type | Routing Mechanism | MassTransit API | Real-World Use Case |
| :--- | :--- | :--- | :--- |
| **Fanout** | Ignores routing keys; copies message to **all** bound queues. | Default `Publish<T>()` | `OrderPlaced` event notify Order, Inventory, Notification services. |
| **Direct** | Exact string match on `RoutingKey`. | `context.SetRoutingKey("email")` + `b.RoutingKey = "email"` | Notification channels: routing between `email` vs `sms` workers. |
| **Topic** | Pattern match on dot-separated keys using `*` (single word) and `#` (zero or more words). | `context.SetRoutingKey("payment.card.failed")` + `b.RoutingKey = "payment.*.failed"` | Payment processing, fraud alerts (`payment.*.failed`), and audit analytics (`payment.#`). |
| **Headers** | Matches key-value pairs in AMQP message headers (e.g. `x-match = all`). | `context.Headers.Set("tier", "enterprise")` + `b.SetBindingArgument(...)` | Priority processing, enterprise SLAs, compliance or document types. |

---

## 1. Fanout Exchange (ফ্যানআউট এক্সচেঞ্জ)
### English
* **How it works**: The exchange duplicates incoming messages to every bound queue unconditionally. Routing keys are completely ignored.
* **MassTransit Implementation**:
  * Default behavior for `Publish<T>()`.
  * Publisher:
    ```csharp
    await _publisher.Publish(new OrderPlaced(orderId, customer));
    ```
  * Subscribers: `Order.Service`, `Inventory.Service`, and `Notification.Service` all receive their own copy.

### বাংলা (Bangla)
* **কীভাবে কাজ করে**: এই এক্সচেঞ্জ রাউটিং কী (Routing Key) গ্রাহ্য করে না। যতগুলো কিউ এই এক্সচেঞ্জের সাথে যুক্ত থাকে, সবাইকে হুবহু এক কপি মেসেজ পাঠিয়ে দেয় (ব্রডকাস্ট)।
* **বাস্তব উদাহরণ**: যখন কোনো অর্ডার তৈরি হয় (`OrderPlaced`), তখন একই সাথে ইনভেন্টরি স্টক রিজার্ভ করতে হয় এবং কাস্টমারকে ওয়েলকাম ইমেইল পাঠাতে হয়।

---

## 2. Direct Exchange (ডিরেক্ট এক্সচেঞ্জ)
### English
* **How it works**: Messages are routed only to queues whose binding key matches the message's `RoutingKey` **exactly**.
* **MassTransit Implementation**:
  1. Configure Publish Topology on Publisher:
     ```csharp
     config.Publish<SendNotificationEvent>(p => p.ExchangeType = ExchangeType.Direct);
     ```
  2. Bind Consumer Endpoints with Specific Routing Keys:
     ```csharp
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
     ```
  3. Publish with Routing Key:
     ```csharp
     await _publisher.Publish(new SendNotificationEvent(...), ctx => ctx.SetRoutingKey("email"));
     ```

### বাংলা (Bangla)
* **কীভাবে কাজ করে**: মেসেজের সাথে পাঠানো `RoutingKey` এবং কিউ-এর `BindingKey` হুবহু (Exact Match) মিললে শুধুমাত্র সেই কিউ-তে মেসেজ যায়।
* **বাস্তব উদাহরণ**: নোটিফিকেশন সিস্টেম। মেসেজের রাউটিং কী `email` হলে শুধু ইমেইল কিউ-তে যাবে, `sms` হলে শুধু SMS কিউ-তে যাবে।

---

## 3. Topic Exchange (টপিক এক্সচেঞ্জ)
### English
* **How it works**: Routes messages based on wildcard pattern matching on dot-separated routing keys:
  * `*` (asterisk): Replaces **exactly one** word (e.g., `payment.*.failed` matches `payment.card.failed` and `payment.paypal.failed`).
  * `#` (hash): Replaces **zero or more** words (e.g., `payment.#` matches any payment event).
* **MassTransit Implementation**:
  1. Configure Publish Topology:
     ```csharp
     config.Publish<PaymentProcessedEvent>(p => p.ExchangeType = ExchangeType.Topic);
     ```
  2. Bind Queues with Wildcards:
     * **Fraud Detection Queue**: Binds `payment.*.failed` (only alerts on failures).
     * **Analytics Queue**: Binds `payment.#` (captures everything).
  3. Publish with Dynamic Routing Key:
     ```csharp
     await _publisher.Publish(new PaymentProcessedEvent(...), ctx =>
     {
         ctx.SetRoutingKey($"payment.{paymentMethod}.{status}");
     });
     ```

### বাংলা (Bangla)
* **কীভাবে কাজ করে**: ডট (.) দিয়ে বিভক্ত রাউটিং কী-তে ওয়াইল্ডকার্ড প্যাটার্ন ম্যাচ করে মেসেজ পাঠায়:
  * `*` (স্টার): ঠিক একটি শব্দ নির্দেশ করে।
  * `#` (হ্যাশ): শূন্য বা একাধিক যেকোনো শব্দ নির্দেশ করে।
* **বাস্তব উদাহরণ**: পেমেন্ট সিস্টেম ও ফ্রড ডিটেকশন। পেমেন্ট ফেইল করলে (`payment.card.failed`) ফ্রড অ্যালার্ট এবং অ্যানালিটিক্স উভয় কিউ-তে যাবে, কিন্তু সফল পেমেন্ট (`payment.card.success`) শুধু অ্যানালিটিক্স কিউ (`payment.#`)-তে যাবে।

---

## 4. Headers Exchange (হেডার্স এক্সচেঞ্জ)
### English
* **How it works**: Routes messages based on AMQP message header key-value attributes instead of routing keys.
  * `x-match = all`: All specified headers must match.
  * `x-match = any`: At least one specified header must match.
* **MassTransit Implementation**:
  1. Configure Publish Topology:
     ```csharp
     config.Publish<DocumentProcessedEvent>(p => p.ExchangeType = ExchangeType.Headers);
     ```
  2. Bind Queue with Header Arguments:
     ```csharp
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
     ```
  3. Publish with Headers:
     ```csharp
     await _publisher.Publish(new DocumentProcessedEvent(...), ctx =>
     {
         ctx.Headers.Set("tier", "enterprise");
     });
     ```

### বাংলা (Bangla)
* **কীভাবে কাজ করে**: রাউটিং কী বাদ দিয়ে মেসেজের মেটাডেটা বা হেডার অ্যাট্রিবিউট (Headers) দেখে মেসেজ রাউট করে।
* **বাস্তব উদাহরণ**: এন্টারপ্রাইজ প্রায়োরিটি প্রসেসিং। যেসব ডকুমেন্টের হেডারে `tier = enterprise` থাকবে, সেগুলো সাধারণ কিউ-তে না গিয়ে সরাসরি হাই-প্রায়োরিটি `enterprise-document-queue`-তে যাবে।

---

## How to Test via Swagger / HTTP Endpoints

Open `Order.Service` Swagger at [http://localhost:5003/swagger](http://localhost:5003/swagger):

1. **Test Fanout**:
   * `POST /Orders/publish-fanout` $\rightarrow$ Look at `Order.Service`, `Inventory.Service`, and `Notification.Service` consoles (all 3 receive it).
2. **Test Direct**:
   * `POST /Orders/publish-direct?recipient=user@example.com&content=Welcome&channel=email` $\rightarrow$ Consumed by `EmailNotificationConsumer`.
   * `POST /Orders/publish-direct?recipient=+123456789&content=OTP:1234&channel=sms` $\rightarrow$ Consumed by `SmsNotificationConsumer`.
3. **Test Topic**:
   * `POST /Orders/publish-topic?orderId=10&amount=99&method=card&status=failed` $\rightarrow$ Consumed by **both** Fraud Detection and Analytics!
   * `POST /Orders/publish-topic?orderId=11&amount=99&method=card&status=success` $\rightarrow$ Consumed **only** by Analytics!
4. **Test Headers**:
   * `POST /Orders/publish-headers?fileName=report.pdf&tier=enterprise` $\rightarrow$ Consumed by `EnterpriseDocumentConsumer`.
   * `POST /Orders/publish-headers?fileName=report.pdf&tier=standard` $\rightarrow$ Skipped by `EnterpriseDocumentConsumer`.
