# MassTransit Resilience & Error Handling (English & বাংলা)

---

## 1. Error Queues: `_error` & `_skipped`
### English
* **`_error` Queue**:
  * When a consumer throws an unhandled exception and all configured retry attempts are exhausted, MassTransit automatically moves the message into a fault queue named `<queue_name>_error`.
  * Along with the payload, MassTransit adds rich diagnostic headers:
    * `MT-Fault-Message`: The exception message.
    * `MT-Fault-StackTrace`: Full stack trace.
    * `MT-Fault-Timestamp`: When the failure occurred.
* **`_skipped` Queue**:
  * If a message arrives in a queue but no registered consumer knows how to handle that message contract/type, MassTransit moves it to `<queue_name>_skipped` to avoid message loss.

### বাংলা (Bangla)
* **`_error` কিউ**:
  * যদি কনজিউমার কোডে কোনো এক্সেপশন (Exception) ঘটে এবং সব রিট্রাই ফেইল করে, MassTransit মেসেজটিকে ফেলে দেয় না; স্বয়ংক্রিয়ভাবে `<queue_name>_error` কিউ-তে ট্রান্সফার করে।
  * এর সাথে মেসেজের হেডার হিসেবে এরর মেসেজ, স্ট্যাকট্রেস (Stack trace) এবং কোন সময় ফেইল হয়েছে তা যুক্ত করে দেয়।
* **`_skipped` কিউ**:
  * কিউ-তে এমন কোনো মেসেজ আসলে যার জন্য কোনো Consumer কনফিগার করা নেই, মেসেজটি হারিয়ে যাওয়ার হাত থেকে বাঁচাতে MassTransit এটিকে `<queue_name>_skipped` কিউ-তে পাঠায়।

---

## 2. Retry Policies
### English
MassTransit provides in-memory retry policies that execute before a message is acknowledged or rejected.

#### Types of Retries:
1. **Immediate Retry**: Retries right away without waiting (useful for thread race conditions).
   ```csharp
   r.Immediate(3);
   ```
2. **Interval Retry**: Retries after a fixed delay.
   ```csharp
   r.Interval(3, TimeSpan.FromSeconds(5));
   ```
3. **Exponential Backoff**: Ideal for transient network or database failures (e.g., waiting 1s, 2s, 4s, 8s).
   ```csharp
   r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
   ```
4. **Exception Filters**: Ignore non-transient errors (e.g., do not retry if input validation fails):
   ```csharp
   r.Ignore<ValidationException>();
   r.Handle<HttpRequestException>();
   ```

### বাংলা (Bangla)
মেসেজ প্রসেসিং ফেইল করলে সাথে সাথে এরর কিউ-তে না পাঠিয়ে কয়েকবার পুনরায় চেষ্টা (Retry) করার ক্ষমতা:

1. **Immediate Retry**: কোনো বিলম্ব ছাড়াই সাথে সাথে আবার চেষ্টা করে।
2. **Interval Retry**: নির্দিষ্ট সময় পরপর (যেমন: প্রতি ৫ সেকেন্ড পর) চেষ্টা করে।
3. **Exponential Backoff**: প্রোডাকশনে সবচেয়ে বেশি ব্যবহৃত হয়। প্রতিবার রিট্রাইয়ের সময় গুণোত্তর হারে বাড়ে (যেমন: ১ম বার ১ সেকেন্ড, ২য় বার ৩ সেকেন্ড, ৩য় বার ৯ সেকেন্ড)। থার্ড পার্টি API বা ডাটাবেজ সাময়িকভাবে ডাউন থাকলে এটি খুব কার্যকর।
4. **Exception Filtering**: যেসব এরর বারবার চেষ্টা করলেও ঠিক হবে না (যেমন `ValidationException` বা ভুল ইনপুট), সেগুলোকে `Ignore()` করে দেওয়া যায় যাতে অহেতুক সিস্টেম রিসোর্স নষ্ট না হয়।

---

## 3. Redelivery (Delayed Retry / Second Chance)
### English
* **Difference between Retry and Redelivery**:
  * **Retry**: Happens in-memory on the active consumer worker thread. The message remains unacknowledged in RabbitMQ.
  * **Redelivery**: MassTransit acknowledges the message, schedules it for future delivery (using RabbitMQ Delayed Exchange or Quartz), and re-queues it later (e.g., after 5 minutes, 30 minutes, 2 hours).
  * This is critical when an external dependency (like a third-party payment gateway) is down for maintenance.

```csharp
cfg.UseDelayedRedelivery(r => r.Intervals(
    TimeSpan.FromMinutes(5), 
    TimeSpan.FromMinutes(15), 
    TimeSpan.FromMinutes(60)
));
```

### বাংলা (Bangla)
* **Retry বনাম Redelivery-এর পার্থক্য**:
  * **Retry**: মেমোরির ভেতরে তৎক্ষণাৎ বা কয়েক সেকেন্ডের ব্যবধানে ঘটে। এই সময় মেসেজটি RabbitMQ-এর চ্যানেলে লক থাকে।
  * **Redelivery**: যদি কোনো বাহ্যিক সার্ভিস দীর্ঘ সময় ডাউন থাকে (যেমন পেমেন্ট গেটওয়ে ৩০ মিনিট ধরে ডাউন), তখন মেমোরি আটকে না রেখে মেসেজটি একনলেজ করে শিডিউল করা হয় এবং দীর্ঘ বিরতির পর (যেমন ৫ মিনিট, ১৫ মিনিট, ১ ঘণ্টা পর) পুনরায় কিউ-তে পাঠানো হয়।

---

## 4. Concurrency & Prefetch Tuning (Senior Level Optimization)
### English
* **`PrefetchCount`**: The number of messages RabbitMQ pushes to the client buffer in advance.
  * High prefetch = high throughput for fast, lightweight consumers.
  * Low prefetch (e.g., 1 to 16) = prevents worker starvation when processing heavy/slow tasks.
* **`ConcurrentMessageLimit`**: The number of messages the consumer processes in parallel on the local machine.
  * Typical rule of thumb: `PrefetchCount >= ConcurrentMessageLimit`.

```csharp
cfg.ReceiveEndpoint("order-processing", e =>
{
    e.PrefetchCount = 32;
    e.ConcurrentMessageLimit = 16;
});
```

### বাংলা (Bangla)
* **`PrefetchCount`**: ব্রোকার থেকে ক্লায়েন্ট একবারে কতগুলো মেসেজ লোকাল মেমোরি বাফারে নিয়ে রাখবে।
  * মেসেজ প্রসেস হতে বেশি সময় লাগলে Prefetch কম রাখা ভালো (যাতে একটি কনজিউমার একা সব মেসেজ আটকে না রাখে)।
* **`ConcurrentMessageLimit`**: লোকাল মেশিনে একই সাথে সমান্তরালভাবে (Parallel) কয়টি মেসেজ এক্সিকিউট হবে।
  * গোল্ডেন রুল: `PrefetchCount` সবসময় `ConcurrentMessageLimit`-এর সমান বা বেশি হওয়া উচিত।
