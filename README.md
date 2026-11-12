# Qasimha.Sdk

New to Qasimha? Read [Getting started](../../docs/getting-started.md) first for the overall flow; this page covers the .NET SDK only.

Server-side .NET SDK for the Qasimha merchant API: checkout sessions, plan quotes, orders,
payment links and QR, settlements, webhook endpoints, and webhook verification. Targets .NET 6 and .NET 8, uses `HttpClient`
and `System.Text.Json`, and depends on nothing else beyond `Microsoft.Extensions.Http` for the
optional dependency-injection registration.

The SDK is a thin, honest wrapper. It does not invent behaviour the API does not have, it never
hides an error, and it never holds anything you did not give it.

## Install

Version 0.1.0 is not yet on NuGet.org, so `dotnet add package Qasimha.Sdk` cannot find it yet.
Build the package from your copy of this repository (.NET 8 SDK or later) into a local folder:

```sh
cd /path/to/repo/sdks/dotnet          # your copy of this repository
dotnet pack src/Qasimha.Sdk -c Release -o /path/to/local-packages
```

Then add that folder as a package source in your project's `nuget.config` and add the package:

```sh
cd /path/to/your-app
dotnet new nugetconfig                # only if the project has no nuget.config yet
dotnet nuget add source /path/to/local-packages --name qasimha-local --configfile nuget.config
dotnet add package Qasimha.Sdk --version 0.1.0
```

`--configfile nuget.config` keeps the source with your project rather than in your user-wide NuGet
settings. If your build machines do not have that folder, commit `Qasimha.Sdk.0.1.0.nupkg` to a
folder in your repository (for example `packages/`) and point the source at it with a relative
path.

Once the package is published:

```sh
dotnet add package Qasimha.Sdk
```

More on installing every package: [installation.md](../../docs/installation.md).

## Configuration

### Environments

| | Sandbox | Production |
|---|---|---|
| API host | `https://api-uat.qasimha.com` | `https://api.qasimha.com` |
| Secret key | `sk_test_…` | `sk_live_…` |
| Publishable key | `pk_test_…` | `pk_live_…` |

Create keys in the Merchant Portal under Settings → API Keys. The SDK picks the environment from
the key prefix: a `sk_test_` key talks to the sandbox, a `sk_live_` key to production. You do not
set a URL. The `BaseUrl` option exists only for a test server you run yourself; leave it unset in
sandbox and production.

### Where the values live

Keep keys and secrets out of source control (including `appsettings.json`) and out of the browser
or mobile app. In ASP.NET Core, read them through `IConfiguration` from a `Qasimha` section and set
them as environment variables in each deployment (`__` is the section separator), or with
`dotnet user-secrets` in development:

| Configuration key | Environment variable | Value |
|---|---|---|
| `Qasimha:ApiKey` | `Qasimha__ApiKey` | `sk_test_…` in sandbox, `sk_live_…` in production. Required. |
| `Qasimha:WebhookSecret` | `Qasimha__WebhookSecret` | `whsec_…` of the webhook endpoint registered in that environment. |
| `Qasimha:PublishableKey` | `Qasimha__PublishableKey` | `pk_test_…` / `pk_live_…`. Only if you quote plans with `Checkout.GetPlansAsync`. |
| `Qasimha:StoreBaseUrl` (your own name) | `Qasimha__StoreBaseUrl` | The public base URL of this deployment, used to build return URLs. |

These are the .NET spelling of the `QASIMHA_API_KEY`, `QASIMHA_WEBHOOK_SECRET` and
`QASIMHA_PUBLISHABLE_KEY` variables the other SDKs use. `appsettings.json` holds only the
non-secret value:

```json
{
  "Qasimha": {
    "StoreBaseUrl": "https://localhost:5001"
  }
}
```

```csharp
using Qasimha.Sdk;
using Qasimha.Sdk.Extensions.DependencyInjection;

builder.Services.AddQasimha(options =>
{
    options.ApiKey = builder.Configuration["Qasimha:ApiKey"];
    options.WebhookSecret = builder.Configuration["Qasimha:WebhookSecret"];
    options.PublishableKey = builder.Configuration["Qasimha:PublishableKey"];   // optional
});

var storeBaseUrl = builder.Configuration["Qasimha:StoreBaseUrl"]!;   // e.g. https://shop.example
var merchantUrl = new MerchantUrl
{
    Success = $"{storeBaseUrl}/checkout/qasimha/return?order={Uri.EscapeDataString(orderReferenceId)}",
    Failure = $"{storeBaseUrl}/checkout/qasimha/return?order={Uri.EscapeDataString(orderReferenceId)}",
    Cancel = $"{storeBaseUrl}/cart",
};
```

```sh
dotnet user-secrets init
dotnet user-secrets set "Qasimha:ApiKey" "sk_test_…"     # development only
```

### Return URLs

`MerchantUrl` (`Success`, `Failure`, optional `Cancel`) tells the hosted checkout where to send
the shopper afterwards. Use `https` URLs on your own site (plain `http` only for `localhost`), and
build them from the deployment's base URL so sandbox orders return to your test site and
production orders to your live site. Qasimha appends `orderId`, `orderReferenceId` and `status`
to the URL. The return is not proof of payment: always confirm with `Orders.GetAsync(orderId)`
(see [Handle the return](#handle-the-return-verify-with-ordersgetasync)). Details:
[return-url-contract.md](../../docs/return-url-contract.md); every setting in one place:
[configuration.md](../../docs/configuration.md).

### Switching to production

1. Replace `Qasimha__ApiKey` (and `Qasimha__PublishableKey`) with the `sk_live_…` / `pk_live_…`
   keys from the production Merchant Portal. No code change.
2. Point `Qasimha__StoreBaseUrl` at the live site, so the return URLs are production URLs.
3. Register the production webhook URL and set its `whsec_…` secret as `Qasimha__WebhookSecret`.
4. Remove any `BaseUrl` override.

### Client options

Create one `QasimhaClient` per API key and reuse it; it is thread-safe. Without dependency
injection, construct it directly:

```csharp
using Qasimha.Sdk;

var client = new QasimhaClient(new QasimhaClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("QASIMHA_API_KEY"),   // sk_test_… or sk_live_…
    WebhookSecret = Environment.GetEnvironmentVariable("QASIMHA_WEBHOOK_SECRET"),
    PublishableKey = Environment.GetEnvironmentVariable("QASIMHA_PUBLISHABLE_KEY"),   // pk_test_… or pk_live_…, for plan quotes
    Language = "en",                                                   // or "ar": the language of API messages
    UserAgentSuffix = "my-store/1.4.0",
});
```

The environment is inferred from the key prefix: `sk_test_` talks to the sandbox at
`https://api-uat.qasimha.com`, `sk_live_` to production at `https://api.qasimha.com`.
Setting `Environment` to the other one throws `ConfigurationException`. `BaseUrl` overrides
both (it must be `https://`, except for `localhost` / `127.0.0.1` test servers).

Other options: `Timeout` (per attempt, default 30 s), `MaxRetries` (0–5, default 2), `Logger`
(see below), and `Delay` (replace it in tests to make retries instant). `PublishableKey` is
optional and only used by `Checkout.GetPlansAsync`; it must be from the same environment as
`ApiKey` (`sk_test_` with `pk_live_` throws `ConfigurationException`).

### ASP.NET Core / dependency injection

`AddQasimha` (shown under [Where the values live](#where-the-values-live)) registers
`QasimhaClient` as a typed client on `IHttpClientFactory` with redirects disabled; inject
`QasimhaClient` wherever you need it. `AddQasimha` returns the `IHttpClientBuilder`, so you can
chain your own handlers.

### Bring your own `HttpClient`

`new QasimhaClient(options, httpClient)` uses a client you manage (the SDK never disposes it);
`new QasimhaClient(options, handler)` uses your `HttpMessageHandler`. In both cases set
`AllowAutoRedirect = false` on the primary handler: the API never redirects, and the SDK treats a
3xx as an `ApiException`. Certificate validation is the runtime's default and cannot be turned off
through the SDK. The runtime's proxy settings (`HTTPS_PROXY`) apply.

## Quote instalment plans for an amount

The quote the on-site widget shows under a price, for a server that renders its own product
or basket page, or wants to decide whether to offer Qasimha at all. It is quoted from your
policy's price list with no customer data; the hosted checkout quotes the customer's own terms.

```csharp
var quote = await client.Checkout.GetPlansAsync(1200.00m);

foreach (var plan in quote.Items)
{
    Console.WriteLine($"{plan.Instalments} x {plan.InstalmentAmount} {quote.Currency}, total {plan.TotalPayable}");
}
```

The call authenticates with the **publishable** key (`X-Public-Key`); the API key is not sent.
Set `PublishableKey` on the client or pass it per call, together with the language of the
quote:

```csharp
var arabic = await client.Checkout.GetPlansAsync(1200.00m, new GetPlansOptions { Lang = "ar", PublishableKey = "pk_test_…" });
```

`Plans` carries `MerchantName`, `Currency` (`SAR`), `MinAmount`, `MaxAmount` and `Items`, one
`Plan` per tenor with `Instalments`, `Frequency`, `InstalmentAmount`, `FirstInstalmentAmount`,
`LastInstalmentAmount`, `FeesIncludingVat`, `TotalPayable` and `ShariaCompliant`. `Items` is
empty, not an error, when the amount is outside `[MinAmount, MaxAmount]` or the account has no
active policy. Without a publishable key the call throws `ConfigurationException` before any
request; an amount with more than two decimals throws `ValidationException` locally; a rejected
key is `AuthenticationException` (401) and a refused amount `ValidationException` (400). The
call is a GET and is retried like every other read.

## Create a checkout session and redirect

```csharp
using Qasimha.Sdk;

var session = await client.Checkout.CreateSessionAsync(new CreateSessionRequest
{
    OrderReferenceId = "ORDER-12345",
    TotalAmount = 1302.10m,
    TaxAmount = 173.10m,
    ShippingAmount = 25.00m,
    DiscountAmount = 50.00m,
    Description = "Order #12345",
    Items = new[]
    {
        new CheckoutItem { ReferenceId = "SKU-1", Name = "Wireless Headphones", Type = ItemType.Physical, Sku = "SKU-1", Quantity = 1, UnitPrice = 899.00m, TotalAmount = 899.00m },
        new CheckoutItem { ReferenceId = "SKU-2", Name = "USB-C Cable", Type = ItemType.Physical, Sku = "SKU-2", Quantity = 3, UnitPrice = 45.00m, TotalAmount = 135.00m },
        new CheckoutItem { ReferenceId = "SKU-3", Name = "Extended Warranty", Type = ItemType.Digital, Sku = "SKU-3", Quantity = 1, UnitPrice = 120.00m, TotalAmount = 120.00m },
    },
    Consumer = new Consumer { FirstName = "Noura", LastName = "Al-Shammari", PhoneNumber = "+9665XXXXXXXX", Email = "noura@example.com" },
    ShippingAddress = new Address { FirstName = "Noura", LastName = "Al-Shammari", Line1 = "King Fahd Rd", City = "Riyadh", CountryCode = "SA" },
    MerchantUrl = new MerchantUrl
    {
        Success = "https://store.example/checkout/return?order=12345",
        Failure = "https://store.example/checkout/failed?order=12345",
        Cancel = "https://store.example/cart",
    },
});

// Store session.OrderId against your order, then redirect the shopper.
return Results.Redirect(session.PaymentUrl!);
```

Amounts are `decimal` with at most two decimals. The SDK checks the basket before sending it and
throws `ValidationException` naming the line, without any request being made, when it does not
add up:

- `item.TotalAmount == Quantity × UnitPrice − item.DiscountAmount`
- `TotalAmount == Σ item.TotalAmount + TaxAmount + ShippingAmount − DiscountAmount`

Both to the halala. `ShippingAddress` is required by the API's validator even for digital
baskets. The API remains the authority: whatever passes locally is still validated server-side.

Outcomes of `CreateSessionAsync`:

- a `Session` with `PaymentUrl` (always present on a 201);
- `DuplicateOrderReferenceException` when `OrderReferenceId` is already in use: it carries
  `ExistingOrderId` and `ExistingPaymentUrl`, so send the shopper to the checkout already open;
- `OrderDeclinedException` when the order was created but not approved: `OrderId` identifies it.

After the hosted checkout the shopper is sent to `MerchantUrl.Success`, `Failure` or `Cancel`.
A return is a hint, not proof:
read the order back (next section) or wait for the webhook before marking anything paid.

POS and QR orders use the same basket shapes:

```csharp
var pos = await client.Checkout.CreatePosSessionAsync(new CreatePosSessionRequest { /* Consumer.Email required */ });
// pos.CheckoutUrl: the hosted page the customer opens; pos.Status == OrderStatus.AwaitingCheckout

var qr = await client.Checkout.CreateQrSessionAsync(new CreateQrSessionRequest { /* … */ });
// qr.QrCodeUrl: render it as a QR code. A declined QR order throws OrderDeclinedException.
```

## Handle the return (verify with `Orders.GetAsync`)

Never trust a return URL or a webhook alone. Read the order back and act on its status:

```csharp
var order = await client.Orders.GetAsync(orderId);

if (order.Status == OrderStatus.Confirmed)
{
    // The shopper finished the hosted checkout and paid the first instalment. Mark your order paid.
}
```

An order that completes the hosted checkout is already `Confirmed`. `Orders.CaptureAsync` applies
to `New` / `Approved` orders only and answers `400 INVALID_STATUS` on a confirmed one. A store
plugin marks the order paid on `Confirmed`, calls `FulfillAsync` on shipment and `RefundAsync`
after that. `order.RefundableAmount` is the customer's paid instalments minus refunds so far.

`Order` is a curated model: status, amounts, dates, `Customer`, `Items`, `Installments`,
`Fulfillments`, `Reversals`, `Voids`, `StatusHistory`. Everything else the API returned is in
`order.Raw` (a `JsonElement`). Status, channel and payment type are exposed by name
(`OrderStatus.Declined`, `OrderChannel.Online`, `PaymentType.PayByInstalments`) whether the API
sent the integer code or the name; an unknown value is passed through as the string received.

## Look an order up by your reference

When all you hold is your own `OrderReferenceId` (a return route, a webhook, a store that never
kept the Qasimha id), read the order directly:

```csharp
var order = await client.Orders.GetByReferenceAsync("ORDER-12345");
```

It returns the same `Order` as `GetAsync`. The match is exact and case-sensitive; a reference
you never used, or one used by another merchant, throws `NotFoundException`. A `/` inside the
reference is kept as a path separator (`ORDER/2026/1` is read at `/by-reference/ORDER/2026/1`);
every other character is percent-encoded. `Orders.ListAsync(new ListOrdersRequest { Search = … })`
remains the fuzzy search across reference, customer name, email and phone.

## Webhooks (raw body, `ConstructEvent`)

Verify every delivery with the **raw** bytes as received, never a re-serialised object, then
confirm the order with `Orders.GetAsync` before acting.

ASP.NET Core minimal API:

```csharp
app.MapPost("/webhooks/qasimha", async (HttpRequest request, QasimhaClient client) =>
{
    using var reader = new StreamReader(request.Body);
    var rawBody = await reader.ReadToEndAsync();

    WebhookEvent evt;
    try
    {
        evt = client.Webhooks.ConstructEvent(rawBody, name => request.Headers[name]);
    }
    catch (WebhookSignatureException ex)
    {
        return Results.BadRequest(ex.ReasonCode);
    }

    // De-duplicate on evt.Id, enqueue evt for processing, and answer quickly.
    return Results.Ok();
});
```

If something earlier in the pipeline (model binding, a logging middleware) has already read the
body, call `request.EnableBuffering()` in middleware that runs before it and rewind with
`request.Body.Position = 0` before the `StreamReader`. Do not bind the body to a model and
serialise it again: any change to whitespace or number formatting breaks the signature.

`ConstructEvent` also takes the two headers directly, and a `byte[]` body:

```csharp
var evt = client.Webhooks.ConstructEvent(rawBytes, signatureHeader, timestampHeader);
```

`WebhookEvent` exposes `Id`, `Type`, `OrderId`, `OrderReferenceId`, `Status`, `Amount`,
`Currency`, `OccurredAt`, `DeliveryAttempt` and the whole body as `Raw`. Event types, as
`WebhookEventType` constants: `order.confirmed` (the order is `Confirmed`), `order.declined`,
`order.fulfilled`, `order.partially_fulfilled`, `order.refunded`, `order.partially_refunded`
(`Amount` is that refund), `order.voided`, `order.cancelled`, `order.expired`,
`checkout_link.expired`, and `ping` from `WebhookEndpoints.PingAsync`. `order.completed` stays
accepted as an alias of `order.confirmed` for one release.

Verification rules:

- Format `sha256=<hex>`: HMAC-SHA256 of the raw body with your secret. When an
  `X-Qasimha-Timestamp` header is present it is also checked against the tolerance (300 s by
  default) as a replay guard.
- Format `t=<unix>,v1=<hex>[,v1=<hex>]`: HMAC-SHA256 of `"<t>." + body`; `t` must be within the
  tolerance; any `v1` may match (several are sent during a secret rotation).
- Every comparison is constant-time. Any failure throws `WebhookSignatureException` with a
  `Reason` (`MissingHeader`, `MalformedHeader`, `TimestampOutsideTolerance`,
  `SignatureMismatch`). There is no unsigned path.

A delivery to an endpoint you registered (next section) is signed with that
endpoint's secret in the `t=,v1=` format, carries `X-Qasimha-Delivery-Attempt`, and its body
has `orderId` and `data.order`, so `evt.OrderId` is set and `Orders.GetAsync(evt.OrderId)` is the
confirmation. A delivery that fails is retried until your server answers 2xx
(`WebhookDelivery.MaxAttempts` attempts over about a day). An account with no registered
endpoint keeps the account-level URL set up by Qasimha: `sha256=` format, the body without an
order id, and the event names `order.completed`, `order.declined`, `order.voided`,
`order.reversed`; there, confirm with `Orders.GetByReferenceAsync(evt.OrderReferenceId)`.

## Manage webhook endpoints

Endpoints are per merchant and managed with an API key that has the `webhooks:manage` scope (a
key without it gets `PermissionException`). Up to 5 per account; the URL must be `https://` and
reachable from the internet (the API refuses private-network targets).

```csharp
var created = await client.WebhookEndpoints.CreateAsync(new CreateWebhookEndpointRequest
{
    Url = "https://store.example/webhooks/qasimha",
    Description = "Production store",
    Events = new[] { WebhookEventType.OrderConfirmed, WebhookEventType.OrderRefunded },   // omit to receive every event
});
// created.Secret is returned once: store it as QASIMHA_WEBHOOK_SECRET. created.SecretHint is its last characters.

var ping = await client.WebhookEndpoints.PingAsync(created.Id);        // queues a signed "ping" event
var failed = await client.WebhookEndpoints.ListDeliveriesAsync(created.Id, new ListWebhookDeliveriesRequest { Status = WebhookDeliveryStatus.Failed });
foreach (var d in failed.Items)
{
    Console.WriteLine($"{d.EventType} {d.Status} {d.Attempts}/{d.MaxAttempts} last={d.LastStatusCode} {d.LastError}");
}

await client.WebhookEndpoints.ReplayDeliveryAsync(created.Id, failed.Items[0].Id);       // after fixing your receiver
await client.WebhookEndpoints.UpdateAsync(created.Id, new UpdateWebhookEndpointRequest { IsActive = false }); // pause
var rotated = await client.WebhookEndpoints.RotateSecretAsync(created.Id);              // rotated.Secret, once
var all = await client.WebhookEndpoints.ListAsync();
var one = await client.WebhookEndpoints.GetAsync(created.Id);
await client.WebhookEndpoints.DeleteAsync(created.Id);
```

The SDK checks `Url` (https) and `Events` (known `WebhookEventType` names) before sending and
throws `ValidationException` without a request otherwise; the API answers `400` with
`URL_INVALID`, `URL_PRIVATE`, `URL_TOO_LONG`, `INVALID_EVENT` or `LIMIT_EXCEEDED`. The secret is
never logged by the SDK and is not part of `WebhookEndpoint.ToString()`. After a rotation,
deliveries carry a `v1=` for the new and the previous secret until
`rotated.PreviousSecretExpiresAt` (24 hours), so a server holding either one verifies; switch
`WebhookSecret` to the new value within that window. `UpdateAsync` (PUT) and `DeleteAsync`
(DELETE) are sent once, without retries; the POSTs carry an `Idempotency-Key` like every other write.

## Capture / fulfil / refund / cancel

```csharp
await client.Orders.FulfillAsync(orderId, new FulfillRequest
{
    ShippedAmount = 1200.00m,                 // omit to fulfil everything outstanding; 0 is rejected
    ShippingCompany = "Aramex",
    TrackingNumber = "ARX123",
    TrackingUrl = "https://tracking.example/ARX123",
});

await client.Orders.RefundAsync(orderId, new RefundRequest { Amount = 300.00m, Reason = "Customer return" });

await client.Orders.CancelAsync(orderId);     // voids an order that has not been fulfilled
await client.Orders.CaptureAsync(orderId);    // New / Approved orders only

var counts = await client.Orders.CountsAsync();          // All, Approved, Authorised, Captured, Refunded, Cancelled
var contract = await client.Orders.ContractAsync(orderId); // contract.Content (PDF bytes), contract.ContentType
```

`Orders.ListAsync` filters by `Status` (an `OrderStatus` name or an `OrderStatusFilter` alias
such as `captured`), `Search`, `Page` and `PageSize`, and returns `Page<OrderSummary>` with
`Items` and `Pagination`.

## Payment links & QR

For merchants without a website. The order is created up-front; the customer completes the
same hosted journey.

```csharp
var link = await client.CheckoutLinks.CreateAsync(new CreateCheckoutLinkRequest
{
    Amount = 250.00m,
    Channel = CheckoutLinkChannel.Link,       // SMS to Phone (required); Qr returns a URL you render
    Phone = "+9665XXXXXXXX",
    Language = "ar",
    OrderReferenceId = "ORDER-12345",
});

var status = await client.CheckoutLinks.GetAsync(link.Id);   // status.Completed, status.Declined, status.Status
var recent = await client.CheckoutLinks.ListAsync();          // the latest 50, each with Waiting
await client.CheckoutLinks.CancelAsync(link.Id);              // 409 NOT_OPEN once it is no longer open
```

Links expire after two hours. Create, get and list return different subsets of `CheckoutLink`
today, so every member except `Id` is optional and the full payload is in `Raw`.

## Errors

Every failure is a `QasimhaException` with `Code` (the envelope code), `HttpStatus`,
`RequestId`, `Errors` (field errors, names in the API's PascalCase such as
`Items[0].Quantity`) and `RawEnvelope`. `Message` is the API's own text, localised by
`Language`. Subtypes, by HTTP status then code:

| Exception | When |
|---|---|
| `ConfigurationException` | the client is misconfigured, or no webhook secret or publishable key is available |
| `ValidationException` | any 400, or a request refused before sending (`Errors` names the fields) |
| `AuthenticationException` | 401: bad or revoked key (the body is empty; the message is fixed) |
| `PermissionException` | 403: the key lacks the scope |
| `NotFoundException` | 404 |
| `DuplicateOrderReferenceException` | 409 `DUPLICATE_ORDER_REFERENCE`, with `ExistingOrderId` / `ExistingPaymentUrl` |
| `ConflictException` | any other 409 (`MERCHANT_SUSPENDED`, `NOT_OPEN`, `INVALID_STATE`) |
| `OrderDeclinedException` | 422 `ORDER_DECLINED`, with `OrderId` |
| `RateLimitException` | 429 after the retries, with `RetryAfter` when sent |
| `ServerException` | 5xx (extends `ApiException`) |
| `ApiException` | anything else, including a 2xx whose envelope says `success: false` |
| `ConnectionException` | DNS, TLS, socket or timeout failure; `InnerException` is the transport's |
| `WebhookSignatureException` | a delivery failed verification, with `Reason` |

```csharp
try
{
    var session = await client.Checkout.CreateSessionAsync(request);
}
catch (DuplicateOrderReferenceException ex) when (ex.ExistingPaymentUrl is not null)
{
    return Results.Redirect(ex.ExistingPaymentUrl);
}
catch (OrderDeclinedException ex)
{
    // ex.OrderId: read it back for the decision
}
catch (ValidationException ex)
{
    foreach (var error in ex.Errors) Console.WriteLine($"{error.Field}: {error.Message}");
}
```

If your project also imports `System.ComponentModel.DataAnnotations`, alias the SDK's
`ValidationException` (`using QasimhaValidationException = Qasimha.Sdk.ValidationException;`).

## Retries & idempotency

- `Timeout` applies per attempt (connect and read).
- The SDK retries `ConnectionException`, 429, 502, 503, 504 and `409 REQUEST_IN_PROGRESS`, up
  to `MaxRetries` times, with `min(2^attempt × 500 ms, 8 s)` plus 0–250 ms jitter; a
  `Retry-After` on a 429 is honoured (capped at 30 s). Other 4xx and a 500 are never retried: a
  500 on a write may have committed.
- Every POST carries an `Idempotency-Key`: a UUID v4 generated per logical call and reused across
  that call's retries. The API stores the first response and replays it for later requests with
  the same key (`Idempotent-Replay: true`, exposed as `result.Replayed`), or answers
  `409 REQUEST_IN_PROGRESS` while the first request is still running. Override the key with
  `RequestOptions { IdempotencyKey = "…" }`; turn it off with `RequestOptions { Idempotent = false }`,
  which also turns off POST retries.
- `WebhookEndpoints.UpdateAsync` (PUT) and `DeleteAsync` (DELETE) are sent once and never
  retried.
- Creates are additionally protected by your `OrderReferenceId`: a duplicate met on a retry
  (the first attempt timed out after succeeding) is returned as the session already open and
  logged at `Info`; on the first attempt it is `DuplicateOrderReferenceException`.
- Every result exposes `RequestId` (the envelope `uuid`, else `X-Request-Id`) and `Replayed`;
  every exception exposes `RequestId`.

## Logging

No logging by default. Set `Logger` to receive `(level, message, fields)`:

```csharp
options.Logger = (level, message, fields) =>
    logger.Log(level switch
    {
        QasimhaLogLevel.Debug => LogLevel.Debug,
        QasimhaLogLevel.Info => LogLevel.Information,
        QasimhaLogLevel.Warn => LogLevel.Warning,
        _ => LogLevel.Error,
    }, "{Message} {@Fields}", message, fields);
```

Fields are `method`, `path` (without the query string), `status`, `durationMs`, `requestId`,
`attempt` and `code`. The API key, the publishable key, the webhook secret, query strings and
consumer data are never logged, at any level. `client.MaskedKey` (`sk_live_…AbCd`),
`client.MaskedPublishableKey` and `client.ToString()` are safe to log; no key ever appears in
an exception message.

## Security notes

- TLS only. `BaseUrl` must be `https://` (except `localhost` / `127.0.0.1`); certificate
  validation is the runtime's and cannot be disabled through the SDK; no certificate pinning.
- The SDK never follows redirects when it owns the handler. With your own `HttpClient`, set
  `AllowAutoRedirect = false` yourself.
- Keep the API key and the webhook secret in a secrets manager, never in source. Keys are shown
  once at creation in the Merchant Portal and can be revoked there; a webhook secret is shown
  once by `WebhookEndpoints.CreateAsync` and replaced by `RotateSecretAsync`. The publishable
  key is the only one safe to embed in a page; it quotes plans and nothing else.
- Verify every webhook with the raw body, de-duplicate on `WebhookEvent.Id`, and confirm the
  order with `Orders.GetAsync` before fulfilling.

## Demo store backend

[`examples/DemoMerchantServer`](examples/DemoMerchantServer), in the repository, is a complete merchant backend for the
web demo stores, built on this SDK as an ASP.NET Core minimal API: it prices the basket on the
server, creates the session with `Checkout.CreateSessionAsync`, confirms the outcome with
`Orders.GetAsync` and verifies webhooks on the raw body with `Webhooks.ConstructEvent`. It runs
against the sandbox with your test keys, or offline against the shared mock Qasimha:

```sh
node sdks/node/examples/demo-merchant-server/mock-qasimha.mjs
dotnet run --project sdks/dotnet/examples/DemoMerchantServer
```

## Support

Qasimha Integration Support — customercare@qasimha.com. For account, key and settlement questions
contact your Qasimha integration contact. Include the `RequestId` from the result or exception
when reporting an API problem.
