# Device Authorization Overview

This document explains how to ensure not any device can use your SDK.

## Goal

Only authorized devices should be able to instantiate and run SDK processing.

## How this SDK enforces authorization

The SDK validates a device-bound HMAC license token when creating `IdCreator`.

Validation steps:

1. Token format must be valid.
2. HMAC signature must match your secret.
3. Expiration must be in the future.
4. `DeviceHash` in token must match current device hash.

If any check fails, SDK throws and blocks usage.

## Security model

- License token is signed by your licensing backend.
- Device hash ties token to one machine.
- Expiration allows time-limited access.
- Plan/customer claims allow product tier control.

## Important note

If someone copies token from Device A to Device B, validation fails because device hash is different.

## Minimal example

```csharp
Environment.SetEnvironmentVariable("HIVISION_SDK_LICENSE_SECRET", "YOUR_SECRET");
Environment.SetEnvironmentVariable("HIVISION_SDK_LICENSE_KEY", "ISSUED_TOKEN");

using var id = new IdCreator(); // Fails if unauthorized device
```

## Related docs

- `02-license-token-spec.md`
- `03-license-server-issuer-guide.md`
- `04-client-integration-guide.md`
