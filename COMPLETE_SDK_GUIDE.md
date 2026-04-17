# Complete SDK Guide

This is the final operational guide for shipping Light.SDK with a dual-artifact distribution model:

1. NuGet package for SDK binaries and API.
2. GitHub Releases assets for model bundles (and optional template bundles).

## 1. Release Strategy

For each public version, publish:

1. Light.SDK.<version>.nupkg to NuGet.
2. light-sdk-models-v<version>.zip to GitHub Releases.
3. Optional light-sdk-templates-v<version>.zip to GitHub Releases.

This keeps NuGet small and lets users download models only when needed.

## 2. Pre-Release Checklist

1. Update package version in HivisionIDPhotos-CSharp/Light.SDK/Light.SDK.csproj.
2. Confirm package metadata:
   PackageId, Version, Description, RepositoryUrl, PackageTags, PackageLicenseExpression.
3. Validate docs:
   README.md, docs/usage/01-quick-start.md, docs/usage/06-models-and-presets.md.
4. Build and validate sample:
   dotnet build HivisionIDPhotos-CSharp/Light.SDK/Light.SDK.csproj
   dotnet run --project HivisionIDPhotos-CSharp/Samples/LightSdkConsoleDemo/LightSdkConsoleDemo.csproj

## 3. Pack the SDK

```bash
dotnet pack HivisionIDPhotos-CSharp/Light.SDK/Light.SDK.csproj -c Release
```

Output:

1. HivisionIDPhotos-CSharp/Light.SDK/bin/Release/Light.SDK.<version>.nupkg

## 4. Publish to NuGet

Create API key at nuget.org, then push:

```bash
dotnet nuget push HivisionIDPhotos-CSharp/Light.SDK/bin/Release/Light.SDK.<version>.nupkg --api-key <YOUR_NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
```

CI-safe command:

```bash
dotnet nuget push HivisionIDPhotos-CSharp/Light.SDK/bin/Release/Light.SDK.<version>.nupkg --api-key <YOUR_NUGET_API_KEY> --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## 5. Publish Model Bundles to GitHub Releases

In your GitHub repository release page, upload these artifacts:

1. light-sdk-models-v<version>.zip
2. light-sdk-templates-v<version>.zip (optional)

Recommended extracted structure for models:

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

Recommended extracted structure for templates:

```text
templates/
  template_config.json
  template_1.png
  template_2.png
  ...
```

## 6. Consumer Installation Guide

### 6.1 Install package

```bash
dotnet add package Light.SDK --version <version>
```

### 6.2 Download assets

1. Download model zip from your GitHub Releases page.
2. Extract to host-managed location, for example D:\light-sdk\models.
3. Optionally download template zip and extract to D:\light-sdk\templates.

### 6.3 Configure runtime

```csharp
using var creator = new IdCreator(new IdCreatorOptions
{
    ModelsRootPath = @"D:\light-sdk\models"
});

var result = creator.CreateFromFile("input.jpg", cfg => cfg
    .WithModels(FaceDetectionModelPreset.LightFaceDetect, MattingModelPreset.LightLite)
    .TryTemplate("template_1", @"D:\light-sdk\templates")
    .WithStandardExport(OutputImageFormat.Jpeg, dpi: 300));
```

## 7. Dynamic Path Rules

### Models

Resolution order:

1. IdCreatorOptions.RetinaFaceModelPath and IdCreatorOptions.MattingModelsDirectory
2. IdCreatorOptions.ModelsRootPath
3. RETINAFACE_MODEL_PATH and MATTING_MODEL_DIR
4. Auto-discovery from runtime base directory

### Templates

Resolution order:

1. TryTemplate(templateName, assetsDirectory)
2. LIGHT_TEMPLATE_ASSETS_DIR
3. AppContext.BaseDirectory

## 8. Versioning and Compatibility

1. Keep NuGet version and GitHub model bundle version aligned.
2. If model compatibility changes, bump minor or major version.
3. Add release notes listing required model bundle version.

## 9. CI/CD Baseline

1. Build and run tests on each PR.
2. Publish NuGet and GitHub release assets on tag builds.
3. Keep API key in CI secrets.
4. Use --skip-duplicate for repeatable release pipelines.

## 10. Troubleshooting

1. Package push denied: verify NuGet API key scope and ownership.
2. Model not found: check ModelsRootPath and extracted folder names.
3. Template missing: ensure template_config.json and template image exist in resolved directory.
4. Wrong release asset picked: align app version with model bundle version.

## 11. Step-by-Step Publish Runbook

Use these exact steps to publish a new version.

1. Update metadata and version in HivisionIDPhotos-CSharp/Light.SDK/Light.SDK.csproj.
2. Build SDK:

```bash
dotnet build HivisionIDPhotos-CSharp/Light.SDK/Light.SDK.csproj -c Release
```

3. Run sample validation:

```bash
dotnet run --project HivisionIDPhotos-CSharp/Samples/LightSdkConsoleDemo/LightSdkConsoleDemo.csproj
```

4. Pack NuGet:

```bash
dotnet pack HivisionIDPhotos-CSharp/Light.SDK/Light.SDK.csproj -c Release
```

5. Verify nupkg exists:

```bash
dir HivisionIDPhotos-CSharp/Light.SDK/bin/Release/Light.SDK.*.nupkg
```

6. Push package:

```bash
dotnet nuget push HivisionIDPhotos-CSharp/Light.SDK/bin/Release/Light.SDK.<version>.nupkg --api-key <YOUR_NUGET_API_KEY> --source https://api.nuget.org/v3/index.json --skip-duplicate
```

7. Create GitHub Release with the same version tag.
8. Upload model/template release assets.
9. Update release notes with required model bundle version.

## 12. Legal and Copyright Readiness

Before public release, verify:

1. LICENSE file exists and matches your intended distribution terms.
2. csproj includes copyright metadata.
3. PackageLicenseExpression or PackageLicenseFile is configured.
4. RepositoryUrl and PackageProjectUrl point to your real GitHub repo.
5. Third-party dependencies and model redistribution rights are reviewed.
