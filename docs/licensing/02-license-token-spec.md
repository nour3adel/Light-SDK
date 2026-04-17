# License Token Specification

## Token format

`base64url(payloadJson).base64url(hmacSha256(payload, secret))`

## Payload schema

```json
{
  "DeviceHash": "string",
  "ExpiresAtUtc": "2027-12-31T23:59:59+00:00",
  "Plan": "enterprise",
  "Customer": "Acme Corp"
}
```

## Field meanings

- `DeviceHash`: hash of the target device identity.
- `ExpiresAtUtc`: UTC expiration timestamp.
- `Plan`: license plan name.
- `Customer`: customer account label.

## Signature

- Algorithm: HMAC SHA-256.
- Input: payload segment (not raw JSON).
- Key: your private issuer secret.

## Validation failure cases

- Invalid segment count.
- Invalid Base64Url encoding.
- Signature mismatch.
- Expired token.
- Device hash mismatch.

## Best practices

1. Keep secret in secure backend only.
2. Rotate secret periodically.
3. Issue short-lived tokens.
4. Add server-side revocation checks in your app layer.
