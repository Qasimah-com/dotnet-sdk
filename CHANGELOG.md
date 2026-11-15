# Changelog

All notable changes to `Qasimha.Sdk` are documented in this file. The format follows
Keep a Changelog, and the package follows Semantic Versioning.

## [Unreleased]

### Added

- `Checkout.GetPlansAsync(amount, GetPlansOptions?)`: `GET /widget/plans?amount=&currency=SAR&lang=`,
  the instalment plans the policy offers for an amount, as `Plans` (`MerchantName`, `Currency`,
  `MinAmount`, `MaxAmount`, `Items`) of `Plan`. The call authenticates with the new optional
  `QasimhaClientOptions.PublishableKey` (`pk_test_…` / `pk_live_…`, sent as `X-Public-Key`; the
  API key is not sent) or a per-call `GetPlansOptions.PublishableKey`; `GetPlansOptions.Lang`
  overrides the language. A publishable key from the other environment is a
  `ConfigurationException` at construction, a missing one at the call, before any request. An
  empty plan list is returned, not thrown. `QasimhaClient.MaskedPublishableKey` is safe to log.
- `Orders.GetByReferenceAsync(orderReferenceId)`: `GET /merchant/orders/by-reference/{orderReferenceId}`,
  the same `Order` as `GetAsync`, for a webhook or a return route that only holds your reference.
  A `/` in the reference is kept as a path separator; every other character is percent-encoded.
- `WebhookEndpoints` resource (scope `webhooks:manage`): `CreateAsync`, `ListAsync`, `GetAsync`,
  `UpdateAsync`, `DeleteAsync`, `RotateSecretAsync`, `PingAsync`, `ListDeliveriesAsync` and
  `ReplayDeliveryAsync`, with `WebhookEndpoint`, `WebhookEndpointWithSecret`, `WebhookDelivery`,
  `WebhookPingResult`, `CreateWebhookEndpointRequest`, `UpdateWebhookEndpointRequest`,
  `ListWebhookDeliveriesRequest`, `WebhookDeliveryStatus` and `WebhookEventType`. The URL must be
  `https://` and the event names must be known before a request is sent; the secret is never
  logged and is not part of `ToString()`.

- `CreateSessionRequest.Language` and `CreateQrSessionRequest.Language` (`"ar"` or `"en"`): the
  language of the hosted checkout for one order, sent in the body only when set. Any other value is
  a `ValidationException` before a request is sent. The client-level `Language` is unchanged.

### Changed

- `CreateCheckoutLinkRequest.Amount` is optional (`decimal?`) and no longer a `required` member.
  With `Items` and no amount it is left out of the body and the API uses the sum of the lines; with
  neither, a `ValidationException` on `Amount` is raised before a request is sent.
- `CreateCheckoutLinkRequest.Channel` is nullable; `null` is the `Link` channel and is left out of
  the body. The default is still `Link`.
- `CreateCheckoutLinkRequest.Language` must be `"ar"` or `"en"` and `EjarContractNumber` at most
  50 characters; both are checked before a request is sent.
- `CheckoutItem.Type` is optional (`string?`) and no longer a `required` member. It is free text of
  at most 50 characters, left out of the body when not set; an empty value is a `ValidationException`.
- `MerchantUrl.Cancel` is optional (`string?`) and no longer a `required` member; it is left out of
  the body when not set and the API then uses `Failure`. `Success` and `Failure` must both be
  non-blank whenever a `MerchantUrl` is given.
- `CreateSessionRequest.PaymentType` no longer defaults to `pay_by_instalments` and is sent only
  when set. It is still validated when set.
- `Order.Raw` follows the typed order responses of the API: enum names instead of integers, and
  internal fields that are not part of the contract are no longer included.

### Deprecated

- `CreateSessionRequest.PaymentType` is `[Obsolete]`: the API ignores it.
- `MerchantUrl.Notification` is `[Obsolete]`: the field is reserved. It is never sent, and a
  non-blank value is a `ValidationException` on `MerchantUrl.Notification` before a request is sent.

## [0.1.0] - 2026-09-25

### Added

- `QasimhaClient` with `Checkout`, `Orders`, `CheckoutLinks`, `Settlements` and `Webhooks`
  resources against merchant API v1.
- Client construction rules: key-prefix environment inference, `https://`-only base URLs,
  per-attempt timeout, bounded retries.
- Typed error model: `QasimhaException` and its subtypes (`ConfigurationException`,
  `ValidationException`, `AuthenticationException`, `PermissionException`, `NotFoundException`,
  `DuplicateOrderReferenceException`, `ConflictException`, `OrderDeclinedException`,
  `RateLimitException`, `ApiException`, `ServerException`, `ConnectionException`,
  `WebhookSignatureException`).
- Client-side basket validation (required fields, two-decimal amounts, halala identities).
- Enum normalisation: integer or string status/channel/payment-type values are exposed by name.
- Curated `Order` model with the full decoded response reachable through `Raw`.
- Retries with exponential backoff and jitter on connection errors, 429, 502, 503, 504 and
  `409 REQUEST_IN_PROGRESS`; a UUID v4 `Idempotency-Key` on every POST.
- Webhook verification for both the `sha256=` and the `t=,v1=` signature formats with
  constant-time comparison and a replay tolerance.
- `IServiceCollection.AddQasimha(...)` registration built on `IHttpClientFactory`.
