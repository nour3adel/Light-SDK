# Models and Size Presets

Light.SDK uses external model files distributed separately from NuGet.
Recommended distribution is GitHub Releases model bundles versioned alongside the SDK package.

## Face detection model

Set via request:

```csharp
Models = new ModelOptions
{
    FaceDetectionModel = "retinaface"
}
```

Accepted values include aliases:

- `retinaface`
- `retina`
- `haar`
- `auto`

Strongly typed preset options:

- `FaceDetectionModelPreset.LightFaceDetect`
- `FaceDetectionModelPreset.LightFaceFallback`
- `FaceDetectionModelPreset.Auto`

## Matting model

```csharp
Models = new ModelOptions
{
    MattingModel = "modnet_photographic_portrait_matting"
}
```

Common model keys:

- `modnet_photographic_portrait_matting`
- `hivision_modnet`
- `rmbg-1.4`
- `bria-rmbg`
- `birefnet-v1-lite`
- `birefnet-general`
- `birefnet-general-lite`
- `birefnet-portrait`

Strongly typed Light presets:

- `MattingModelPreset.LightLite`
- `MattingModelPreset.LightHuma`
- `MattingModelPreset.LightM01` through `MattingModelPreset.LightM18`

## Model bundle source

Download model assets from your GitHub Releases page.

Recommended extracted structure:

```text
models/
    detector models/
    matting models/
```

Use IdCreatorOptions.ModelsRootPath to point to this root.

## Size presets

Examples:

- `one_inch`
- `two_inches`
- `american_visa`
- `japanese_visa`
- `korean_visa`
- `social_security_card`
- `graduate_entrance_exam`

If preset is not found, SDK falls back to one-inch size.
