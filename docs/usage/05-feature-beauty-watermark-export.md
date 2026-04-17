# Feature Guide: Beauty, Watermark, Export, and KB

## Beauty tuning

```csharp
var result = id.CreateFromFile("input.jpg", new IdCreatorRequest
{
    Beauty = new BeautyOptions
    {
        Whitening = 2,
        Brightness = 5,
        Contrast = 5,
        Saturation = 3,
        Sharpen = 2
    }
});
```

## Watermark

```csharp
var bytes = File.ReadAllBytes("input.jpg");
var output = id.AddWatermark(
    bytes,
    text: "CONFIDENTIAL",
    fontSize: 18,
    opacity: 0.15,
    angle: 30,
    colorHex: "999999",
    space: 30);
```

## Export format and DPI

```csharp
var result = id.CreateFromFile("input.jpg", new IdCreatorRequest
{
    StandardExport = new ExportOptions
    {
        Format = OutputImageFormat.Jpeg,
        Dpi = 300,
        IncludeDataUriPrefix = false
    },
    HdExport = new ExportOptions
    {
        Format = OutputImageFormat.Png,
        Dpi = 300
    }
});
```

## Target KB

```csharp
var output = id.SetTargetKb(bytes, targetKb: 80, format: OutputImageFormat.Jpeg);
```
