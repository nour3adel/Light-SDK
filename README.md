# Light.SDK

Light.SDK is a production-grade .NET SDK for ID photo processing and generation.
It provides a clean high-level API for photo matting, background composition, crop layout rules, beauty enhancement, watermarking, printable sheets, template overlays, and export policies.

## Why Light.SDK

1. Production-focused API surface with fluent request configuration.
2. Flexible runtime model loading (external models, no heavyweight NuGet payload).
3. Multiple output targets: standard, HD, layout sheet, template render.
4. Dynamic template workflow for built-in and user-uploaded templates.
5. Designed for app teams shipping desktop, server, and cloud workflows.

## Core Features

1. ID photo generation with face-aware framing.
2. Background matting and color replacement (solid and gradients).
3. Face alignment and top-distance/head-ratio controls.
4. Beauty pipeline (whitening, brightness, contrast, saturation, sharpen).
5. Watermark rendering with rotation, opacity, spacing, and color controls.
6. Printable layout sheet generation with cut lines.
7. Template rendering similar to Python template plugin behavior.
8. Export options for JPEG/PNG, DPI scaling, and target-size compression.
9. Strongly-typed presets for size and model selection.

## Install from NuGet

```bash
dotnet add package Light.SDK
```

## Download Models from GitHub Releases

Light.SDK NuGet package is intentionally lightweight and does not include model binaries.

Create a release workflow like this in your GitHub repository:

1. Publish Light.SDK to NuGet.
2. Upload model bundle zip files to GitHub Releases.
3. Ask users to download the matching model bundle version.

Recommended release asset naming:

1. light-sdk-models-v2.0.0.zip
2. light-sdk-templates-v2.0.0.zip (optional)

Recommended extracted model folder structure:

```text
models/
  detector models/
    Light_faceDetect.lsdkm
  matting models/
    Light.lite.lsdkm
    Light.Huma.lsdkm
    Light.M01.lsdkm
    ...
```

## Quick Start

```csharp
using Light.SDK;
using HivisionIDPhotos.Core.Models.Sdk;

var options = new IdCreatorOptions
{
    // Host-managed model root (downloaded from your GitHub Releases).
    ModelsRootPath = @"D:\light-sdk\models"
};

using var creator = new IdCreator(options);

var result = creator.CreateFromFile("input.jpg", cfg => cfg
    .WithSizePreset(IdPhotoSizePreset.AmericanVisa)
    .WithBackgroundColor("FFFFFF")
    .WithModels(FaceDetectionModelPreset.LightFaceDetect, MattingModelPreset.LightLite)
    .WithFaceLayout(headRatio: 0.2, topDistance: 0.12, faceAlign: true)
    .WithBeauty(whitening: 2, brightness: 3, contrast: 3, saturation: 2, sharpen: 1)
    .WithWatermark(text: "Light SDK", fontSize: 18, opacity: 0.15, angle: 30, colorHex: "8A8A8A", space: 30)
    .TryTemplate("template_1", "templates")
    .WithLayoutSheet(LayoutPaperKind.FiveInch, dpi: 300)
    .WithStandardExport(OutputImageFormat.Jpeg, dpi: 300)
    .WithHdExport(OutputImageFormat.Jpeg, dpi: 300)
    .WithLayoutExport(OutputImageFormat.Jpeg, dpi: 300));

File.WriteAllBytes("output_standard.jpg", result.StandardImageBytes);
File.WriteAllBytes("output_hd.jpg", result.HdImageBytes);
if (result.LayoutImageBytes is not null) File.WriteAllBytes("output_layout.jpg", result.LayoutImageBytes);
if (result.TemplateImageBytes is not null) File.WriteAllBytes("output_template.jpg", result.TemplateImageBytes);
```

## Dynamic Path Resolution

### Models

Model resolution order:

1. IdCreatorOptions.RetinaFaceModelPath and IdCreatorOptions.MattingModelsDirectory
2. IdCreatorOptions.ModelsRootPath
3. Environment variables RETINAFACE_MODEL_PATH and MATTING_MODEL_DIR
4. Runtime auto-discovery near application base directory

### Templates

Template assets resolution order:

1. TryTemplate(templateName, assetsDirectory)
2. Environment variable LIGHT_TEMPLATE_ASSETS_DIR
3. Default AppContext.BaseDirectory

Expected template assets in selected directory:

1. template_config.json
2. template_1.png, template_2.png, ...

## Pack and Publish

Pack SDK:

```bash
dotnet pack HivisionIDPhotos-CSharp/Light.SDK/Light.SDK.csproj -c Release
```

Push package:

```bash
dotnet nuget push HivisionIDPhotos-CSharp/Light.SDK/bin/Release/Light.SDK.2.0.0.nupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
```

Publish model bundle to GitHub Releases as separate artifact zip.

## Final Release Checklist

1. Set real repository URLs in project metadata before publishing.
2. Bump package version in csproj.
3. Build and run sample.
4. Pack and push NuGet package.
5. Publish GitHub release assets for models and templates.
6. Confirm LICENSE and copyright metadata are present.

See full operational flow in [COMPLETE_SDK_GUIDE.md](COMPLETE_SDK_GUIDE.md).

## Documentation Map

1. Architecture: [ARCHITECTURE.md](ARCHITECTURE.md)
2. End-to-end release and distribution: [COMPLETE_SDK_GUIDE.md](COMPLETE_SDK_GUIDE.md)
3. Full docs index: [docs/README.md](docs/README.md)
