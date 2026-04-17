# Usage Best Practices

## Performance

1. Reuse one `IdCreator` instance per process scope.
2. Avoid repeatedly reading/writing huge intermediate images.
3. Use JPEG for smaller output, PNG when alpha is needed.

## Reliability

1. Validate license before exposing public endpoints.
2. Return clear API error messages for auth/model failures.
3. Keep output folders writable and monitored.

## Maintainability

1. Centralize your default `IdCreatorRequest` profiles.
2. Version-lock SDK package in production.
3. Keep model and licensing config in environment or secure config.

## Security

1. Never hardcode real issuer secret in source control.
2. Use secret manager or protected environment variables.
3. Rotate license tokens regularly.
