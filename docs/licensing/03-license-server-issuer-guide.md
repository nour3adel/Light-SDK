# License Server Issuer Guide

This guide is for your backend service that issues device licenses.

## Backend flow

1. Client requests activation.
2. Client sends device hash and customer identity.
3. Backend validates purchase/subscription.
4. Backend issues signed token bound to device hash.
5. Client stores token and uses SDK.

## Collect device hash

Client helper:

```csharp
using HivisionIDPhotos.SDK.Licensing;

var deviceHash = LicenseTokenService.GetCurrentDeviceHash();
```

## Issue token on backend

```csharp
using HivisionIDPhotos.SDK.Licensing;

var claims = new LicenseClaims
{
    DeviceHash = request.DeviceHash,
    ExpiresAtUtc = DateTimeOffset.UtcNow.AddMonths(6),
    Plan = "enterprise",
    Customer = "Acme Corp"
};

var token = LicenseTokenService.Issue(claims, issuerSecret);
```

## Rotation strategy

1. Old secret + new secret overlap window.
2. Reissue tokens with new secret.
3. Remove old secret after migration.

## Revocation strategy

SDK validates signature/device locally. For immediate revoke:

1. Keep a revocation list in backend.
2. App performs periodic online check.
3. App disables SDK if token revoked.

## API endpoint suggestion

- `POST /license/activate`
- `POST /license/refresh`
- `POST /license/revoke`
- `POST /license/validate-online`
