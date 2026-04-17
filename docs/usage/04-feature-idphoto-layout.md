# Feature Guide: ID Photo, Face Layout, and Paper Layout

## Generate full ID photo

```csharp
var result = id.CreateFromFile("input.jpg", new IdCreatorRequest
{
    SizePreset = "american_visa",
    FaceLayout = new FaceLayoutOptions
    {
        FaceAlign = true,
        HeadRatio = 0.2,
        TopDistance = 0.12,
        TopDistanceMin = 0.10,
        HeadHeightRatio = 0.45
    }
});
```

## Custom size (width/height)

```csharp
var result = id.CreateFromFile("input.jpg", new IdCreatorRequest
{
    Width = 295,
    Height = 413
});
```

## Crop-only flow

```csharp
var bytes = File.ReadAllBytes("input.jpg");
var result = id.CropIdPhoto(bytes, 413, 295, 0.2, 0.12, faceAlign: true);
```

## Generate paper layout sheet

```csharp
var sheet = id.GenerateLayoutSheet(
    bytes,
    photoHeight: 413,
    photoWidth: 295,
    layoutOptions: new LayoutOptions
    {
        PaperKind = LayoutPaperKind.A4,
        Dpi = 300,
        MarginPx = 40,
        GapPx = 20,
        DrawCutLines = true
    });
```

## Paper presets

- `FiveInch`
- `SixInch`
- `A4`
- `ThreeR`
- `FourR`
- `Custom`
