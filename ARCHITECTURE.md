# SDK Architecture

The SDK is organized by responsibility so each file stays small and maintainable.

## Folder layout

- `Public/`
  - Public entry points consumed by SDK users.
  - `IdCreator` is the facade for end-to-end generation.

- `Contracts/`
  - DTOs for SDK input/output (`IdCreatorRequest`, `IdCreatorResult`).

- `Configuration/`
  - Initialization configuration (`IdCreatorOptions`).

- `Internal/`
  - Internal helper components for mapping, model path setup, and decoding.

- `Licensing/`
  - Device fingerprinting and HMAC token issuance/validation.

## Design goals

1. Keep public API simple.
2. Keep internal concerns isolated and testable.
3. Minimize side effects in constructors.
4. Keep model environment setup centralized.
5. Keep mapping logic away from orchestration.

## Extension guidance

- New request fields: add to `Contracts/IdCreatorRequest` and map in `Internal/IdCreatorRequestMapper`.
- New export formats: extend `Internal/OutputFormatHelper` and core export policy.
- New model discovery strategy: update `Internal/ModelEnvironmentConfigurator`.
- New licensing checks: add into `Licensing/LicenseTokenService`.
