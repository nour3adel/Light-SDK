using System;
using System.IO;
using HivisionIDPhotos.Core.Abstractions;
using HivisionIDPhotos.Core.Models.Sdk;
using HivisionIDPhotos.Core.Services;
using Light.SDK.Internal;
using OpenCvSharp;

namespace Light.SDK;

public sealed class IdCreator : IDisposable
{
    private readonly IFaceDetectorService _faceDetector;
    private readonly IInferenceService _inference;
    private readonly IIdPhotoSdk _sdk;
    private readonly ILightSdkLogger _logger;
    private bool _disposed;

    public string LicensePlan => "unrestricted";
    public string LicensedCustomer => "open";
    public DateTimeOffset LicenseExpiresAtUtc => DateTimeOffset.MaxValue;

    public IdCreator()
        : this(new IdCreatorOptions())
    {
    }

    public IdCreator(string licenseKey)
        : this(new IdCreatorOptions())
    {
    }

    public IdCreator(IdCreatorOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _logger = LightSdkLoggerFactory.Create(options);
        _logger.Info("Initializing IdCreator.");

        // Model paths are configured once so core services can discover bundled or custom models.
        ModelEnvironmentConfigurator.Configure(options);
        _logger.Debug($"Models root configured: {options.ModelsRootPath ?? "<default>"}");

        _faceDetector = new FaceDetectorService();
        _inference = new InferenceService(_faceDetector);
        _sdk = new IdPhotoSdk(_inference);
        _logger.Info("IdCreator initialized successfully.");
    }

    public IdCreatorResult Create(byte[] imageBytes, IdCreatorRequest? request = null)
    {
        EnsureNotDisposed();

        var safeRequest = request ?? new IdCreatorRequest();
        // Decode -> map request -> generate -> export keeps the high-level API predictable.
        using var source = ImageInputDecoder.DecodeBytes(imageBytes);
        var generationOptions = IdCreatorRequestMapper.ToGenerationOptions(safeRequest);
        using var generated = _sdk.Generate(source, generationOptions);
        var result = BuildResult(generated, safeRequest);
        _logger.Debug("Create completed.");
        return result;
    }

    public IdCreatorResult CreateFromFile(string imagePath, IdCreatorRequest? request = null)
    {
        EnsureNotDisposed();
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be empty.", nameof(imagePath));
        }

        var bytes = File.ReadAllBytes(imagePath);
        return Create(bytes, request);
    }

    public IdCreatorResult CreateFromBase64(string base64Image, IdCreatorRequest? request = null)
    {
        EnsureNotDisposed();
        var bytes = ImageInputDecoder.DecodeBase64ToBytes(base64Image);
        return Create(bytes, request);
    }

    public IdCreatorImageResult RemoveBackground(byte[] imageBytes, string mattingModel, ExportOptions? export = null)
    {
        EnsureNotDisposed();
        using var source = ImageInputDecoder.DecodeBytes(imageBytes);
        using var matted = _sdk.RemoveBackground(source, mattingModel);
        var result = BuildSingleImageResult(matted, export ?? new ExportOptions());
        _logger.Debug($"RemoveBackground completed using model: {mattingModel}");
        return result;
    }

    public IdCreatorImageResult AddBackground(byte[] imageBytes, string backgroundColorHex, int renderMode, ExportOptions? export = null)
    {
        EnsureNotDisposed();
        using var source = ImageInputDecoder.DecodeBytes(imageBytes);
        using var composited = _sdk.ApplyBackground(source, new BackgroundOptions
        {
            PrimaryColorHex = backgroundColorHex,
            RenderKind = ToRenderKind(renderMode)
        });

        var result = BuildSingleImageResult(composited, export ?? new ExportOptions());
        _logger.Debug($"AddBackground completed with mode={renderMode} color={backgroundColorHex}");
        return result;
    }

    public IdCreatorImageResult AddWatermark(
        byte[] imageBytes,
        string text,
        int fontSize,
        double opacity,
        int angle,
        string colorHex,
        int space,
        ExportOptions? export = null)
    {
        EnsureNotDisposed();
        using var source = ImageInputDecoder.DecodeBytes(imageBytes);
        using var watermarked = _sdk.ApplyWatermark(source, new WatermarkOptions
        {
            Enabled = true,
            Text = text,
            FontSize = fontSize,
            Opacity = opacity,
            Angle = angle,
            ColorHex = colorHex,
            Space = space
        });

        var result = BuildSingleImageResult(watermarked, export ?? new ExportOptions());
        _logger.Debug("AddWatermark completed.");
        return result;
    }

    public IdCreatorImageResult GenerateLayoutSheet(
        byte[] imageBytes,
        int photoHeight,
        int photoWidth,
        LayoutOptions? layoutOptions = null,
        ExportOptions? export = null)
    {
        EnsureNotDisposed();
        using var source = ImageInputDecoder.DecodeBytes(imageBytes);
        using var layout = _sdk.GenerateLayoutSheet(
            source,
            new IdPhotoPixelSize(photoWidth, photoHeight, "Custom"),
            layoutOptions ?? new LayoutOptions());

        var result = BuildSingleImageResult(layout, export ?? new ExportOptions());
        _logger.Debug("GenerateLayoutSheet completed.");
        return result;
    }

    public IdCreatorImageResult SetTargetKb(byte[] imageBytes, int targetKb, OutputImageFormat format = OutputImageFormat.Jpeg)
    {
        EnsureNotDisposed();
        using var source = ImageInputDecoder.DecodeBytes(imageBytes);
        var export = new ExportOptions
        {
            Format = format,
            TargetKb = targetKb,
            Dpi = 300
        };

        var result = BuildSingleImageResult(source, export);
        _logger.Debug($"SetTargetKb completed. targetKb={targetKb}");
        return result;
    }

    public IdCreatorResult CropIdPhoto(
        byte[] imageBytes,
        int height,
        int width,
        double headRatio,
        double topDistance,
        bool faceAlign = false,
        ExportOptions? export = null)
    {
        EnsureNotDisposed();
        using var source = ImageInputDecoder.DecodeBytes(imageBytes);
        var (std, hd) = _inference.RunIdPhotoCrop(source, height, width, headRatio, topDistance, faceAlign: faceAlign);

        using var stdMat = std;
        using var hdMat = hd;

        var standardExport = export ?? new ExportOptions();
        var hdExport = export ?? new ExportOptions();

        var result = new IdCreatorResult
        {
            StandardImageBytes = _sdk.Export(stdMat, standardExport),
            HdImageBytes = _sdk.Export(hdMat, hdExport),
            StandardImageBase64 = _sdk.ExportBase64(stdMat, standardExport),
            HdImageBase64 = _sdk.ExportBase64(hdMat, hdExport),
            StandardExtension = OutputFormatHelper.GetExtension(standardExport.Format),
            HdExtension = OutputFormatHelper.GetExtension(hdExport.Format)
        };
        _logger.Debug("CropIdPhoto completed.");
        return result;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_faceDetector is IDisposable disposableFaceDetector)
        {
            disposableFaceDetector.Dispose();
        }

        _disposed = true;
        _logger.Info("IdCreator disposed.");
        GC.SuppressFinalize(this);
    }

    private IdCreatorResult BuildResult(IdPhotoGenerationResult generated, IdCreatorRequest request)
    {
        // Export policies are independent per output target.
        var standardExport = request.StandardExport ?? new ExportOptions();
        var hdExport = request.HdExport ?? new ExportOptions();

        var standardBytes = _sdk.Export(generated.StandardImage, standardExport);
        var hdBytes = _sdk.Export(generated.HdImage, hdExport);

        var standardBase64 = _sdk.ExportBase64(generated.StandardImage, standardExport);
        var hdBase64 = _sdk.ExportBase64(generated.HdImage, hdExport);

        byte[]? layoutBytes = null;
        string? layoutBase64 = null;
        var layoutExtension = ".jpg";
        byte[]? templateBytes = null;
        string? templateBase64 = null;
        var templateExtension = ".jpg";

        if (generated.LayoutImage is not null)
        {
            var layoutExport = request.LayoutExport ?? new ExportOptions();
            layoutBytes = _sdk.Export(generated.LayoutImage, layoutExport);
            layoutBase64 = _sdk.ExportBase64(generated.LayoutImage, layoutExport);
            layoutExtension = OutputFormatHelper.GetExtension(layoutExport.Format);
        }

        if (generated.TemplateImage is not null)
        {
            var templateExport = request.LayoutExport ?? new ExportOptions();
            templateBytes = _sdk.Export(generated.TemplateImage, templateExport);
            templateBase64 = _sdk.ExportBase64(generated.TemplateImage, templateExport);
            templateExtension = OutputFormatHelper.GetExtension(templateExport.Format);
        }

        return new IdCreatorResult
        {
            StandardImageBytes = standardBytes,
            HdImageBytes = hdBytes,
            LayoutImageBytes = layoutBytes,
            TemplateImageBytes = templateBytes,
            StandardImageBase64 = standardBase64,
            HdImageBase64 = hdBase64,
            LayoutImageBase64 = layoutBase64,
            TemplateImageBase64 = templateBase64,
            StandardExtension = OutputFormatHelper.GetExtension(standardExport.Format),
            HdExtension = OutputFormatHelper.GetExtension(hdExport.Format),
            LayoutExtension = layoutExtension,
            TemplateExtension = templateExtension
        };
    }

    private IdCreatorImageResult BuildSingleImageResult(Mat image, ExportOptions export)
    {
        return new IdCreatorImageResult
        {
            ImageBytes = _sdk.Export(image, export),
            ImageBase64 = _sdk.ExportBase64(image, export),
            Extension = OutputFormatHelper.GetExtension(export.Format)
        };
    }

    private static BackgroundRenderKind ToRenderKind(int renderMode)
    {
        return renderMode switch
        {
            1 => BackgroundRenderKind.UpDownGradient,
            2 => BackgroundRenderKind.CenterGradient,
            _ => BackgroundRenderKind.Solid
        };
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(IdCreator));
        }
    }
}
