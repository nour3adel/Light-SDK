using HivisionIDPhotos.Core.Models.Sdk;

namespace Light.SDK;

/// <summary>
/// Fluent builder for composing <see cref="IdCreatorRequest"/> instances.
/// </summary>
public sealed class IdCreatorRequestBuilder
{
    private readonly IdCreatorRequest _request;

    /// <summary>
    /// Creates a new builder with a fresh request instance.
    /// </summary>
    public IdCreatorRequestBuilder()
        : this(null)
    {
    }

    /// <summary>
    /// Creates a new builder seeded from an existing request instance.
    /// </summary>
    /// <param name="seed">Existing request to continue editing.</param>
    public IdCreatorRequestBuilder(IdCreatorRequest? seed)
    {
        _request = seed ?? new IdCreatorRequest();
    }

    /// <summary>
    /// Sets the SDK size preset name (for example: one_inch, american_visa).
    /// </summary>
    /// <param name="preset">Preset key.</param>
    public IdCreatorRequestBuilder WithSizePreset(string preset)
    {
        _request.SizePreset = string.IsNullOrWhiteSpace(preset) ? _request.SizePreset : preset;
        return this;
    }

    /// <summary>
    /// Sets the SDK size preset using a strongly-typed enum.
    /// </summary>
    /// <param name="preset">Preset enum value.</param>
    public IdCreatorRequestBuilder WithSizePreset(IdPhotoSizePreset preset)
    {
        _request.SizePreset = preset switch
        {
            IdPhotoSizePreset.OneInch => "one_inch",
            IdPhotoSizePreset.TwoInches => "two_inches",
            IdPhotoSizePreset.SmallOneInch => "small_one_inch",
            IdPhotoSizePreset.SmallTwoInches => "small_two_inches",
            IdPhotoSizePreset.LargeOneInch => "large_one_inch",
            IdPhotoSizePreset.LargeTwoInches => "large_two_inches",
            IdPhotoSizePreset.FiveInches => "five_inches",
            IdPhotoSizePreset.TeacherQualificationCertificate => "teacher_qualification_certificate",
            IdPhotoSizePreset.NationalCivilServiceExam => "national_civil_service_exam",
            IdPhotoSizePreset.PrimaryAccountingExam => "primary_accounting_exam",
            IdPhotoSizePreset.EnglishCet => "english_cet",
            IdPhotoSizePreset.ComputerLevelExam => "computer_level_exam",
            IdPhotoSizePreset.GraduateEntranceExam => "graduate_entrance_exam",
            IdPhotoSizePreset.SocialSecurityCard => "social_security_card",
            IdPhotoSizePreset.ElectronicDriversLicense => "electronic_drivers_license",
            IdPhotoSizePreset.AmericanVisa => "american_visa",
            IdPhotoSizePreset.JapaneseVisa => "japanese_visa",
            IdPhotoSizePreset.KoreanVisa => "korean_visa",
            _ => _request.SizePreset
        };

        return this;
    }

    /// <summary>
    /// Overrides preset sizing with custom pixel width and height.
    /// </summary>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    public IdCreatorRequestBuilder WithCustomSize(int width, int height)
    {
        _request.Width = width;
        _request.Height = height;
        return this;
    }

    /// <summary>
    /// Applies a solid background color.
    /// </summary>
    /// <param name="hexColor">Color in hex format (for example: FFFFFF).</param>
    public IdCreatorRequestBuilder WithBackgroundColor(string hexColor)
    {
        _request.Background.PrimaryColorHex = hexColor;
        _request.Background.RenderKind = BackgroundRenderKind.Solid;
        return this;
    }

    /// <summary>
    /// Applies a background render mode with primary/secondary colors.
    /// </summary>
    /// <param name="renderKind">Solid or gradient mode.</param>
    /// <param name="primaryHex">Primary color in hex format.</param>
    /// <param name="secondaryHex">Optional secondary color for gradients.</param>
    public IdCreatorRequestBuilder WithBackgroundRender(BackgroundRenderKind renderKind, string primaryHex, string? secondaryHex = null)
    {
        _request.Background.RenderKind = renderKind;
        _request.Background.PrimaryColorHex = primaryHex;
        _request.Background.SecondaryColorHex = secondaryHex ?? _request.Background.SecondaryColorHex;
        return this;
    }

    /// <summary>
    /// Configures face layout constraints used during auto-cropping.
    /// </summary>
    /// <param name="headRatio">Desired head-to-image ratio.</param>
    /// <param name="topDistance">Distance from head top to image top.</param>
    /// <param name="topDistanceMin">Minimum top distance allowed.</param>
    /// <param name="headHeightRatio">Expected head vertical ratio.</param>
    /// <param name="faceAlign">Whether face alignment should be applied.</param>
    public IdCreatorRequestBuilder WithFaceLayout(double headRatio, double topDistance, double topDistanceMin = 0.1, double headHeightRatio = 0.45, bool faceAlign = true)
    {
        _request.FaceLayout.HeadRatio = headRatio;
        _request.FaceLayout.TopDistance = topDistance;
        _request.FaceLayout.TopDistanceMin = topDistanceMin;
        _request.FaceLayout.HeadHeightRatio = headHeightRatio;
        _request.FaceLayout.FaceAlign = faceAlign;
        return this;
    }

    /// <summary>
    /// Selects face detection and matting models.
    /// </summary>
    /// <param name="faceDetectionModel">Face detector model key.</param>
    /// <param name="mattingModel">Matting model key.</param>
    public IdCreatorRequestBuilder WithModels(string faceDetectionModel = "retinaface", string mattingModel = "modnet_photographic_portrait_matting")
    {
        _request.Models.FaceDetectionModel = faceDetectionModel;
        _request.Models.MattingModel = mattingModel;
        return this;
    }

    /// <summary>
    /// Selects face detection and matting models using strongly-typed enums.
    /// </summary>
    /// <param name="faceDetectionModel">Face detector preset.</param>
    /// <param name="mattingModel">Matting model preset.</param>
    public IdCreatorRequestBuilder WithModels(
        FaceDetectionModelPreset faceDetectionModel = FaceDetectionModelPreset.LightFaceDetect,
        MattingModelPreset mattingModel = MattingModelPreset.LightLite)
    {
        _request.Models.FaceDetectionModel = faceDetectionModel switch
        {
            FaceDetectionModelPreset.Auto => "auto",
            FaceDetectionModelPreset.LightFaceFallback => "haar",
            _ => "retinaface"
        };

        _request.Models.MattingModel = mattingModel switch
        {
            MattingModelPreset.LightHuma => "hivision_modnet",
            MattingModelPreset.LightM01 => "rmbg-1.4",
            MattingModelPreset.LightM02 => "bria-rmbg",
            MattingModelPreset.LightM03 => "birefnet-v1-lite",
            MattingModelPreset.LightM04 => "da",
            MattingModelPreset.LightM05 => "dam",
            MattingModelPreset.LightM06 => "birefnet-general",
            MattingModelPreset.LightM07 => "birefnet-general-lite",
            MattingModelPreset.LightM08 => "birefnet-portrait",
            MattingModelPreset.LightM09 => "birefnet-dis",
            MattingModelPreset.LightM10 => "birefnet-hrsod",
            MattingModelPreset.LightM11 => "u2net",
            MattingModelPreset.LightM12 => "u2netp",
            MattingModelPreset.LightM13 => "u2net_human_seg",
            MattingModelPreset.LightM14 => "u2net_cloth_seg",
            MattingModelPreset.LightM15 => "u2net-portrait-matting",
            MattingModelPreset.LightM16 => "silueta",
            MattingModelPreset.LightM17 => "isnet-general-use",
            MattingModelPreset.LightM18 => "isnet-anime",
            _ => "modnet_photographic_portrait_matting"
        };

        return this;
    }

    /// <summary>
    /// Configures portrait beauty enhancements.
    /// </summary>
    /// <param name="whitening">Whitening intensity.</param>
    /// <param name="brightness">Brightness adjustment.</param>
    /// <param name="contrast">Contrast adjustment.</param>
    /// <param name="saturation">Saturation adjustment.</param>
    /// <param name="sharpen">Sharpen amount.</param>
    public IdCreatorRequestBuilder WithBeauty(int whitening, int brightness, int contrast, int saturation, int sharpen)
    {
        _request.Beauty.Whitening = whitening;
        _request.Beauty.Brightness = brightness;
        _request.Beauty.Contrast = contrast;
        _request.Beauty.Saturation = saturation;
        _request.Beauty.Sharpen = sharpen;
        return this;
    }

    /// <summary>
    /// Enables text watermark with style settings.
    /// </summary>
    /// <param name="text">Watermark text.</param>
    /// <param name="fontSize">Font size in points.</param>
    /// <param name="opacity">Opacity from 0 to 1.</param>
    /// <param name="angle">Rotation angle in degrees.</param>
    /// <param name="colorHex">Watermark color in hex format.</param>
    /// <param name="space">Spacing between repeated marks.</param>
    public IdCreatorRequestBuilder WithWatermark(string text, int fontSize = 18, double opacity = 0.15, int angle = 30, string colorHex = "999999", int space = 30)
    {
        _request.Watermark.Enabled = true;
        _request.Watermark.Text = text;
        _request.Watermark.FontSize = fontSize;
        _request.Watermark.Opacity = opacity;
        _request.Watermark.Angle = angle;
        _request.Watermark.ColorHex = colorHex;
        _request.Watermark.Space = space;
        return this;
    }

    /// <summary>
    /// Tries to apply a social-media template overlay (Python plugin style).
    /// </summary>
    /// <param name="templateName">Template key from template_config.json, for example: template_1.</param>
    /// <param name="assetsDirectory">Optional path to template assets directory containing png files and template_config.json.</param>
    public IdCreatorRequestBuilder TryTemplate(string templateName = "template_1", string? assetsDirectory = null)
    {
        _request.Template.Enabled = true;
        _request.Template.TemplateName = string.IsNullOrWhiteSpace(templateName) ? "template_1" : templateName;
        _request.Template.AssetsDirectory = assetsDirectory;
        return this;
    }

    /// <summary>
    /// Disables template output.
    /// </summary>
    public IdCreatorRequestBuilder WithoutTemplate()
    {
        _request.Template.Enabled = false;
        _request.Template.TemplateName = "template_1";
        _request.Template.AssetsDirectory = null;
        return this;
    }

    /// <summary>
    /// Disables watermark output.
    /// </summary>
    public IdCreatorRequestBuilder WithoutWatermark()
    {
        _request.Watermark.Enabled = false;
        _request.Watermark.Text = string.Empty;
        return this;
    }

    /// <summary>
    /// Enables printable layout sheet output.
    /// </summary>
    /// <param name="paperKind">Target paper preset.</param>
    /// <param name="dpi">Layout DPI.</param>
    /// <param name="gapPx">Gap between repeated photos.</param>
    /// <param name="marginPx">Layout margins in pixels.</param>
    /// <param name="drawCutLines">Whether cut lines should be drawn.</param>
    public IdCreatorRequestBuilder WithLayoutSheet(LayoutPaperKind paperKind = LayoutPaperKind.FiveInch, int dpi = 300, int gapPx = 20, int marginPx = 40, bool drawCutLines = true)
    {
        _request.GenerateLayoutSheet = true;
        _request.Layout.PaperKind = paperKind;
        _request.Layout.Dpi = dpi;
        _request.Layout.GapPx = gapPx;
        _request.Layout.MarginPx = marginPx;
        _request.Layout.DrawCutLines = drawCutLines;
        return this;
    }

    /// <summary>
    /// Disables layout sheet output.
    /// </summary>
    public IdCreatorRequestBuilder WithoutLayoutSheet()
    {
        _request.GenerateLayoutSheet = false;
        return this;
    }

    /// <summary>
    /// Configures export settings for the standard output image.
    /// </summary>
    /// <param name="format">Target image format.</param>
    /// <param name="dpi">Output DPI.</param>
    /// <param name="targetKb">Optional target size in KB.</param>
    /// <param name="includeDataUriPrefix">Whether base64 output includes data URI prefix.</param>
    public IdCreatorRequestBuilder WithStandardExport(OutputImageFormat format = OutputImageFormat.Jpeg, int dpi = 300, int? targetKb = null, bool includeDataUriPrefix = false)
    {
        _request.StandardExport.Format = format;
        _request.StandardExport.Dpi = dpi;
        _request.StandardExport.TargetKb = targetKb ?? 0;
        _request.StandardExport.IncludeDataUriPrefix = includeDataUriPrefix;
        return this;
    }

    /// <summary>
    /// Configures export settings for the HD output image.
    /// </summary>
    /// <param name="format">Target image format.</param>
    /// <param name="dpi">Output DPI.</param>
    /// <param name="targetKb">Optional target size in KB.</param>
    /// <param name="includeDataUriPrefix">Whether base64 output includes data URI prefix.</param>
    public IdCreatorRequestBuilder WithHdExport(OutputImageFormat format = OutputImageFormat.Jpeg, int dpi = 300, int? targetKb = null, bool includeDataUriPrefix = false)
    {
        _request.HdExport.Format = format;
        _request.HdExport.Dpi = dpi;
        _request.HdExport.TargetKb = targetKb ?? 0;
        _request.HdExport.IncludeDataUriPrefix = includeDataUriPrefix;
        return this;
    }

    /// <summary>
    /// Configures export settings for the optional layout-sheet output image.
    /// </summary>
    /// <param name="format">Target image format.</param>
    /// <param name="dpi">Output DPI.</param>
    /// <param name="targetKb">Optional target size in KB.</param>
    /// <param name="includeDataUriPrefix">Whether base64 output includes data URI prefix.</param>
    public IdCreatorRequestBuilder WithLayoutExport(OutputImageFormat format = OutputImageFormat.Jpeg, int dpi = 300, int? targetKb = null, bool includeDataUriPrefix = false)
    {
        _request.LayoutExport.Format = format;
        _request.LayoutExport.Dpi = dpi;
        _request.LayoutExport.TargetKb = targetKb ?? 0;
        _request.LayoutExport.IncludeDataUriPrefix = includeDataUriPrefix;
        return this;
    }

    /// <summary>
    /// Returns the built request instance.
    /// </summary>
    public IdCreatorRequest Build()
    {
        return _request;
    }
}
