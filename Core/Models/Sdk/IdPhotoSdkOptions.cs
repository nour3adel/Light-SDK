using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace HivisionIDPhotos.Core.Models.Sdk;

public enum BackgroundRenderKind
{
    Solid = 0,
    UpDownGradient = 1,
    CenterGradient = 2
}

public enum OutputImageFormat
{
    Png = 0,
    Jpeg = 1
}

public enum LayoutPaperKind
{
    FiveInch = 0,
    SixInch = 1,
    A4 = 2,
    ThreeR = 3,
    FourR = 4,
    Custom = 5
}

public sealed record IdPhotoPixelSize(int Width, int Height, string Name);

public sealed record PaperSpec(LayoutPaperKind Kind, int WidthPx, int HeightPx, int Dpi, string Name);

public sealed class BackgroundOptions
{
    public string PrimaryColorHex { get; set; } = "638CCE";
    public string SecondaryColorHex { get; set; } = "FFFFFF";
    public BackgroundRenderKind RenderKind { get; set; } = BackgroundRenderKind.Solid;
    public bool AmericanStyleBackground { get; set; }
}

public sealed class FaceLayoutOptions
{
    public double HeadRatio { get; set; } = 0.2;
    public double TopDistance { get; set; } = 0.12;
    public double TopDistanceMin { get; set; } = 0.10;
    public double HeadHeightRatio { get; set; } = 0.45;
    public bool FaceAlign { get; set; }
}

public sealed class ModelOptions
{
    public string FaceDetectionModel { get; set; } = "retinaface";
    public string MattingModel { get; set; } = "modnet_photographic_portrait_matting";
}

public sealed class BeautyOptions
{
    public int Whitening { get; set; }
    public int Brightness { get; set; }
    public int Contrast { get; set; }
    public int Saturation { get; set; }
    public int Sharpen { get; set; }
}

public sealed class WatermarkOptions
{
    public bool Enabled { get; set; }
    public string Text { get; set; } = "CONFIDENTIAL";
    public int FontSize { get; set; } = 20;
    public double Opacity { get; set; } = 0.20;
    public int Angle { get; set; } = 30;
    public int Space { get; set; } = 25;
    public string ColorHex { get; set; } = "B0B0B0";
}

public sealed class TemplateOptions
{
    public bool Enabled { get; set; }
    public string TemplateName { get; set; } = "template_1";
    public string? AssetsDirectory { get; set; }
}

public sealed class LayoutOptions
{
    public LayoutPaperKind PaperKind { get; set; } = LayoutPaperKind.FiveInch;
    public int CustomWidthPx { get; set; }
    public int CustomHeightPx { get; set; }
    public int Dpi { get; set; } = 300;
    public int MarginPx { get; set; } = 40;
    public int GapPx { get; set; } = 20;
    public bool DrawCutLines { get; set; } = true;
}

public sealed class ExportOptions
{
    public OutputImageFormat Format { get; set; } = OutputImageFormat.Jpeg;
    public int Dpi { get; set; } = 300;
    public int TargetKb { get; set; }
    public int JpegQuality { get; set; } = 95;
    public bool IncludeDataUriPrefix { get; set; }
}

public sealed class IdPhotoGenerationOptions
{
    public IdPhotoPixelSize TargetSize { get; set; } = IdPhotoSizeCatalog.OneInch;
    public BackgroundOptions Background { get; set; } = new();
    public FaceLayoutOptions FaceLayout { get; set; } = new();
    public ModelOptions Models { get; set; } = new();
    public BeautyOptions Beauty { get; set; } = new();
    public WatermarkOptions Watermark { get; set; } = new();
    public TemplateOptions Template { get; set; } = new();
    public LayoutOptions Layout { get; set; } = new();
    public bool GenerateLayoutSheet { get; set; }
}

public sealed class IdPhotoGenerationResult : IDisposable
{
    public IdPhotoGenerationResult(Mat standardImage, Mat hdImage, Mat? layoutImage, Mat? templateImage)
    {
        StandardImage = standardImage;
        HdImage = hdImage;
        LayoutImage = layoutImage;
        TemplateImage = templateImage;
    }

    public Mat StandardImage { get; }
    public Mat HdImage { get; }
    public Mat? LayoutImage { get; }
    public Mat? TemplateImage { get; }
    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public void Dispose()
    {
        StandardImage.Dispose();
        HdImage.Dispose();
        LayoutImage?.Dispose();
        TemplateImage?.Dispose();
    }
}
