# SDK Quick Start

## 1. Install

```bash
dotnet add package Light.SDK --version 1.0.3
```

## 2. Download runtime assets

1. Download model bundle from your GitHub Releases page.
2. Extract to a host path, for example D:\light-sdk\models.
3. Optionally download template assets and extract to D:\light-sdk\templates.

## 3. Configure SDK

```csharp
var options = new IdCreatorOptions
{
    ModelsRootPath = @"D:\light-sdk\models"
};
```

## 4. Generate photo

```csharp
using Light.SDK;
using HivisionIDPhotos.Core.Models.Sdk;

using var id = new IdCreator(options);

var result = id.CreateFromFile("input.jpg", cfg => cfg
    .WithSizePreset(IdPhotoSizePreset.OneInch)
    .WithBackgroundColor("FFFFFF")
    .WithModels(FaceDetectionModelPreset.LightFaceDetect, MattingModelPreset.LightLite)
    .WithFaceLayout(headRatio: 0.2, topDistance: 0.12, faceAlign: true)
    .WithBeauty(whitening: 2, brightness: 3, contrast: 3, saturation: 2, sharpen: 1)
    .TryTemplate("template_1", @"D:\light-sdk\templates")
    .WithStandardExport(OutputImageFormat.Jpeg, dpi: 300)
    .WithHdExport(OutputImageFormat.Png, dpi: 300)
);

File.WriteAllBytes("standard.jpg", result.StandardImageBytes);
File.WriteAllBytes("hd.png", result.HdImageBytes);
if (result.TemplateImageBytes is not null) File.WriteAllBytes("template.jpg", result.TemplateImageBytes);
```

## 5. Dynamic discovery summary

1. Models: explicit options, then ModelsRootPath, then environment, then runtime auto-discovery.
2. Templates: TryTemplate path, then LIGHT_TEMPLATE_ASSETS_DIR, then AppContext.BaseDirectory.
