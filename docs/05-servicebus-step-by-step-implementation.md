# Step-by-Step MassTransit Implementation in ServiceBus (English & বাংলা)

This guide walks through the complete, working MassTransit architecture implemented across the `ServiceBus` solution on **.NET 6 (`net6.0`)**:
* `Contracts` (Shared message definitions)
* `Order.Service` (Publisher & Request Client & Consumer)
* `Inventory.Service` (Consumer for events & commands)

---

## Architecture Overview (সিস্টেম আর্কিটেকচার)

```
                       +--------------------------------------------------------+
                       |                  RabbitMQ Message Broker               |
                       |                     (localhost:5672)                   |
                       +--------------------------------------------------------+
                               ▲                            ▲                 ▲
            (1) Publish Event  │                            │                 │ (3) Send Command
            "OrderPlaced"      │                            │                 │ "UpdateInventoryStock"
                               │                            │                 │
               +-----------------------------+              │  (2) Request /  │
               |        Order.Service        |              │      Response   │
               | (Port 5001 / HTTP 5003)     |              │                 │
               |                             |              ▼                 │
               | - Publishes OrderPlaced     |  CheckOrderStatus (Req)        │
               | - Consumes OrderPlaced (Log)|  OrderStatusResult (Res)       │
               | - Calls RequestClient       |                                │
               +-----------------------------+                                │
                                                                              ▼
                                             +------------------------------------+
                                             |         Inventory.Service          |
                                             |     (Port 5000 / HTTP 5002)        |
                                             |                                    |
                                             | - Consumes OrderPlaced (Pub/Sub)   |
                                             | - Consumes UpdateInventoryStock    |
                                             |   (Point-to-Point Command)         |
                                             | - Publishes ProductCreationPlaced  |
                                             +------------------------------------+
```

---

## Step 1: Design Message Contracts (`Contracts/`)
### English
MassTransit relies on standard .NET types (preferably `record` types) to define contracts. We placed them in the shared `Contracts` project:
1. **Event**: `OrderPlaced(int OrderId, string UserName)` — Broadcasted when an order is created.
2. **Command**: `UpdateInventoryStock(int OrderId, string ProductName, int Quantity)` — Sent directly to the inventory queue to update stock.
3. **Request**: `CheckOrderStatus(int OrderId)` — Sent by the API to query state.
4. **Response**: `OrderStatusResult(int OrderId, int StatusCode, string StatusText)` — Returned by the consumer.

### বাংলা (Bangla)
MassTransit-এ মেসেজ কন্ট্রাক্ট তৈরি করার জন্য C# `record` সবচেয়ে উপযুক্ত (কারণ এগুলো Immutable বা অপরিবর্তনযোগ্য):
* **Event**: `OrderPlaced` — অর্ডার তৈরি হওয়ার পর ব্রডকাস্ট করা হয়।
* **Command**: `UpdateInventoryStock` — সরাসরি ইনভেন্টরি কিউ-তে পাঠানো হয় স্টক কমানোর জন্য।
* **Request/Response**: `CheckOrderStatus` ও `OrderStatusResult` — RPC স্টাইলে রিকোয়েস্ট পাঠিয়ে রেসপন্স পাওয়ার জন্য।

---

## Step 2: Configure Order.Service (`Startup.cs` & `OrdersController.cs`)

### 1. MassTransit Configuration (`Startup.cs`)
```csharp
services.AddMassTransit(x =>
{
    // 1. Register Consumers
    x.AddConsumer<OrderPlacedConsumer>();
    x.AddConsumer<CheckOrderStatusConsumer>();

    // 2. Register Request Client for RPC
    x.AddRequestClient<CheckOrderStatus>();

    // 3. Configure RabbitMQ Transport
    x.UsingRabbitMq((context, config) =>
    {
        config.Host("localhost", "/", c =>
        {
            c.Username("guest");
            c.Password("guest");
        });

        // 4. Resilience: Retry 3 times with 2-second interval
        config.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));

        // 5. Automatically create queues and bindings
        config.ConfigureEndpoints(context);
    });
});
services.AddMassTransitHostedService();
```

### 2. Dispatches in `OrdersController.cs`
* **Publishing an Event (Pub/Sub)**:
  ```csharp
  await _publisher.Publish(new OrderPlaced(command.OrderId, command.UserName));
  ```
* **Sending a Command (Point-to-Point)**:
  ```csharp
  var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:inventory-stock-update"));
  await sendEndpoint.Send(new UpdateInventoryStock(orderId, product, qty));
  ```
* **Request-Response (RPC over Bus)**:
  ```csharp
  var response = await _client.GetResponse<OrderStatusResult>(new CheckOrderStatus(orderId));
  return Ok(response.Message);
  ```

---

## Step 3: Configure Inventory.Service (`Startup.cs`)

### 1. Register Consumers & Dedicated Endpoints
```csharp
services.AddMassTransit(x =>
{
    // Register Consumers
    x.AddConsumer<InventoryManagementConsumer>();
    x.AddConsumer<InventoryStockUpdateConsumer>();
    x.AddConsumer<OrderPlacedInventoryConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("localhost", "/", c =>
        {
            c.Username("guest");
            c.Password("guest");
        });

        // Retry policy
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));

        // Dedicated receive endpoint for Point-to-Point command
        cfg.ReceiveEndpoint("inventory-stock-update", e =>
        {
            e.ConfigureConsumer<InventoryStockUpdateConsumer>(ctx);
        });

        // Configure Remaining Endpoints (Events)
        cfg.ConfigureEndpoints(ctx);
    });
});
```

---

## Step 4: How to Run and Test (কীভাবে চালাবেন এবং টেস্ট করবেন)

### 1. Start RabbitMQ via Docker
From `/Users/limon/interview/Backend/ServiceBus`:
```bash
docker-compose up -d
```
* RabbitMQ Web UI: `http://localhost:15672` (User: `guest`, Password: `guest`)

### 2. Run Both Services
Open two terminal windows:
* **Terminal 1 (Order.Service)**:
  ```bash
  cd Order.Service
  dotnet run
  ```
  Swagger: `http://localhost:5003/swagger` or `https://localhost:5001/swagger`

* **Terminal 2 (Inventory.Service)**:
  ```bash
  cd Inventory.Service
  dotnet run
  ```
  Swagger: `http://localhost:5002/swagger` or `https://localhost:5000/swagger`

### 3. Test Scenarios (টেস্ট করার দৃশ্যপট)
1. **Pub/Sub Test (1-to-many)**:
   * Call `POST /Orders/CreateOrder` with `{"orderId": 101, "userName": "Limon", "productName": "Laptop"}`.
   * **Result**: Look at both terminal outputs:
     * `Order.Service` logs: `Received order: {"OrderId":101,...}`
     * `Inventory.Service` logs: `Inventory.Service received OrderPlaced event! Reserving inventory...`
2. **Point-to-Point Command Test (1-to-1)**:
   * Call `POST /Orders/SendInventoryUpdate?orderId=101&product=Laptop&qty=2`.
   * **Result**: `Inventory.Service` logs: `Processing command: UpdateInventoryStock for Order ID: 101...`
3. **Request/Response Test (RPC)**:
   * Call `POST /Orders/check-order-status?orderId=101`.
   * **Result**: Returns HTTP 200 with JSON payload `{"orderId": 101, "statusCode": 1, "statusText": "Pending for Shipment"}`.
