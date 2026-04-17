# Licensing Operations Checklist

## Before release

1. Issuer secret stored in secure vault.
2. Token expiration policy defined.
3. Device migration policy defined.
4. Revocation policy defined.
5. Support playbook for license errors ready.

## During onboarding

1. Collect device hash.
2. Verify subscription/order.
3. Issue token.
4. Deliver token securely.
5. Confirm SDK initializes on customer machine.

## Monitoring

1. Track activation count.
2. Track failed validation reasons.
3. Track renewals and expirations.
4. Alert on suspicious repeated activation attempts.

## Support troubleshooting map

- "License key is missing": key not set.
- "License secret is missing": secret not set.
- "License signature is invalid": wrong secret/token tamper.
- "License is expired": renew required.
- "License is not valid for this device": issue new token for this device.
