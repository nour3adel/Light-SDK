# Client Integration Guide (Authorization)

## Required runtime settings

- `HIVISION_SDK_LICENSE_KEY`
- `HIVISION_SDK_LICENSE_SECRET`

Optional:

- `ModelsRootPath` via `IdCreatorOptions` if models are outside output folder.

## Integration pattern A (env-based)

```csharp
Environment.SetEnvironmentVariable("HIVISION_SDK_LICENSE_SECRET", secret);
Environment.SetEnvironmentVariable("HIVISION_SDK_LICENSE_KEY", token);

using var id = new IdCreator();
```

## Integration pattern B (options-based)

```csharp
using var id = new IdCreator(new IdCreatorOptions
{
    LicenseKey = token,
    LicenseSecret = secret,
    ModelsRootPath = "D:/hivision-models"
});
```

## Handle authorization failures

```csharp
try
{
    using var id = new IdCreator(token);
}
catch (InvalidOperationException ex)
{
    // Return activation required / unauthorized device message.
    Console.WriteLine(ex.Message);
}
```

## Device transfer policy

If customer moves to a new device:

1. Generate new device hash on new machine.
2. Send hash to license backend.
3. Issue new token.
4. Replace old token.
