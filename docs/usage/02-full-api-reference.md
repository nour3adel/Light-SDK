# Full SDK API Reference

## Main class

- `IdCreator`

## Constructors

- `IdCreator()`
- `IdCreator(string licenseKey)`
- `IdCreator(IdCreatorOptions options)`

## Metadata properties

- `LicensePlan`
- `LicensedCustomer`
- `LicenseExpiresAtUtc`

## Main methods

- `Create(byte[] imageBytes, IdCreatorRequest? request = null)`
- `CreateFromFile(string imagePath, IdCreatorRequest? request = null)`
- `CreateFromBase64(string base64Image, IdCreatorRequest? request = null)`

## Single-image methods

- `RemoveBackground(byte[] imageBytes, string mattingModel, ExportOptions? export = null)`
- `AddBackground(byte[] imageBytes, string backgroundColorHex, int renderMode, ExportOptions? export = null)`
- `AddWatermark(byte[] imageBytes, string text, int fontSize, double opacity, int angle, string colorHex, int space, ExportOptions? export = null)`
- `GenerateLayoutSheet(byte[] imageBytes, int photoHeight, int photoWidth, LayoutOptions? layoutOptions = null, ExportOptions? export = null)`
- `SetTargetKb(byte[] imageBytes, int targetKb, OutputImageFormat format = OutputImageFormat.Jpeg)`
- `CropIdPhoto(byte[] imageBytes, int height, int width, double headRatio, double topDistance, bool faceAlign = false, ExportOptions? export = null)`

## Request model

- `IdCreatorRequest` includes:
  - size preset or custom width/height
  - background settings
  - face layout settings
  - model settings
  - beauty settings
  - watermark settings
  - template settings
  - layout generation settings
  - export settings for standard/hd/layout

## Fluent builder highlights

- `WithSizePreset(...)`
- `WithCustomSize(...)`
- `WithModels(...)`
- `WithFaceLayout(...)`
- `WithBeauty(...)`
- `WithWatermark(...)`
- `TryTemplate(templateName, assetsDirectory)`
- `WithLayoutSheet(...)`
- `WithStandardExport(...)`
- `WithHdExport(...)`
- `WithLayoutExport(...)`

## Result models

- `IdCreatorResult`
- `IdCreatorImageResult`

`IdCreatorResult` may include:

- Standard output bytes/base64
- HD output bytes/base64
- Layout output bytes/base64
- Template output bytes/base64
