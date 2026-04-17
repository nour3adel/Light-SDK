# Feature Guide: Background and Matting

## Remove background

```csharp
var bytes = File.ReadAllBytes("input.jpg");
var output = id.RemoveBackground(bytes, "modnet_photographic_portrait_matting");
File.WriteAllBytes("matted.png", output.ImageBytes);
```

## Add solid background

```csharp
var output = id.AddBackground(bytes, "FFFFFF", 0);
File.WriteAllBytes("solid.jpg", output.ImageBytes);
```

## Add up-down gradient

```csharp
var output = id.AddBackground(bytes, "638CCE", 1);
```

## Add center gradient

```csharp
var output = id.AddBackground(bytes, "638CCE", 2);
```

## Supported bundled matting models

- `modnet_photographic_portrait_matting`
- `hivision_modnet`

You can use external models by setting `ModelsRootPath`.
