# Neurotechnology-Style Activation Flow

This SDK now supports a professional activation model:

1. SDK is standalone package.
2. Customer cannot use SDK without valid device license file.
3. Customer uses Activation Wizard to generate request file from purchased key.
4. Vendor signs request and returns license file.
5. SDK verifies signed file + device binding at runtime.
6. Vendor can deactivate activation and reuse key seat.

## Runtime enforcement

`IdCreator` requires signed license file by default.

Required runtime config:

- `HIVISION_SDK_LICENSE_FILE` (path to signed file)
- `HIVISION_SDK_PUBLIC_KEY_PEM` or `HIVISION_SDK_PUBLIC_KEY_FILE`

If missing/invalid, SDK blocks initialization.

## Seat-based key model

License key metadata:

- plan
- customer
- max activations
- optional key expiration

Activation records:

- activation id
- license key
- device hash
- active/inactive status
- issued/expiry timestamps

## Deactivation behavior

When deactivated:

1. Activation record set to inactive.
2. Seat is released.
3. License key can be reused for another device.

## Security guidance

1. Keep private key only on vendor authority side.
2. Never ship private key in SDK or client app.
3. Rotate keys with migration plan.
4. Add optional online revocation checks in app layer.
