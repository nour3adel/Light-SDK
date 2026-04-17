# SDK Licensing Guide

This SDK uses a device-bound HMAC license token.

## Token design

Token format:

`base64url(payloadJson).base64url(hmacSha256(payload, secret))`

Payload fields:

- `DeviceHash`
- `ExpiresAtUtc`
- `Plan`
- `Customer`

## Device binding

The SDK computes a deterministic device hash from:

- machine name
- domain
- OS description
- OS architecture
- processor count

## Generate license token (issuer side)

Use your own secure backend service to issue keys.

```csharp
using HivisionIDPhotos.SDK.Licensing;

var secret = "YOUR_PRIVATE_ISSUER_SECRET";
var deviceHash = LicenseTokenService.GetCurrentDeviceHash();

var claims = new LicenseClaims
{
    DeviceHash = deviceHash,
    ExpiresAtUtc = DateTimeOffset.UtcNow.AddMonths(12),
    Plan = "enterprise",
    Customer = "Acme Corp"
};

var licenseKey = LicenseTokenService.Issue(claims, secret);
```

## Consumer-side usage

```csharp
Environment.SetEnvironmentVariable("HIVISION_SDK_LICENSE_SECRET", "YOUR_PRIVATE_ISSUER_SECRET");
Environment.SetEnvironmentVariable("HIVISION_SDK_LICENSE_KEY", "ISSUED_LICENSE_TOKEN");

using var id = new IdCreator();
```

Alternative constructor:

```csharp
using var id = new IdCreator("ISSUED_LICENSE_TOKEN");
```

## Recommended production deployment

1. Do not hardcode your issuer secret in client code.
2. Retrieve license key from your licensing service after user authentication.
3. Rotate secrets and issue short-lived licenses.
4. Revalidate license key periodically in your app lifecycle.
5. Consider adding optional online revocation checks in your host app.
