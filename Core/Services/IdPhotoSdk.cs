using System;
using System.IO;
using System.Text.Json;
using HivisionIDPhotos.Core.Abstractions;
using HivisionIDPhotos.Core.Models;
using HivisionIDPhotos.Core.Models.Sdk;
using OpenCvSharp;

namespace HivisionIDPhotos.Core.Services;

public sealed class IdPhotoSdk : IIdPhotoSdk
{
    private readonly IInferenceService _inferenceService;

    public IdPhotoSdk(IInferenceService inferenceService)
    {
        _inferenceService = inferenceService;
    }

    public IdPhotoGenerationResult Generate(Mat sourceImage, IdPhotoGenerationOptions options)
    {
        if (sourceImage.Empty())
        {
            throw new ArgumentException("Input image is empty.", nameof(sourceImage));
        }

        var requestOptions = new IdPhotoRequestOptions
        {
            Width = options.TargetSize.Width,
            Height = options.TargetSize.Height,
            BackgroundColor = ResolveBackgroundColor(options.Background),
            RenderMode = (int)options.Background.RenderKind,
            MattingModel = options.Models.MattingModel,
            FaceDetectModel = options.Models.FaceDetectionModel,
            HeadRatio = options.FaceLayout.HeadRatio,
            TopDistance = options.FaceLayout.TopDistance,
            TopDistanceMin = options.FaceLayout.TopDistanceMin,
            HeadHeightRatio = options.FaceLayout.HeadHeightRatio,
            FaceAlign = options.FaceLayout.FaceAlign,
            WhiteningStrength = options.Beauty.Whitening,
            BrightnessStrength = options.Beauty.Brightness,
            ContrastStrength = options.Beauty.Contrast,
            SaturationStrength = options.Beauty.Saturation,
            SharpenStrength = options.Beauty.Sharpen
        };

        var (rawStandard, rawHd) = _inferenceService.RunIdPhoto(sourceImage, requestOptions);
        var standard = rawStandard;
        var hd = rawHd;

        if (options.Watermark.Enabled && !string.IsNullOrWhiteSpace(options.Watermark.Text))
        {
            var watermarkedStandard = ApplyWatermark(standard, options.Watermark);
            standard.Dispose();
            standard = watermarkedStandard;

            var watermarkedHd = ApplyWatermark(hd, options.Watermark);
            hd.Dispose();
            hd = watermarkedHd;
        }

        Mat? layout = null;
        if (options.GenerateLayoutSheet)
        {
            layout = GenerateLayoutSheet(standard, options.TargetSize, options.Layout);
        }

        Mat? template = null;
        if (options.Template.Enabled && !string.IsNullOrWhiteSpace(options.Template.TemplateName))
        {
            template = ApplyTemplate(standard, options.Template);
        }

        var result = new IdPhotoGenerationResult(standard, hd, layout, template);
        result.Metadata["face_model"] = options.Models.FaceDetectionModel;
        result.Metadata["matting_model"] = options.Models.MattingModel;
        result.Metadata["size_name"] = options.TargetSize.Name;
        result.Metadata["size_px"] = $"{options.TargetSize.Width}x{options.TargetSize.Height}";
        return result;
    }

    public Mat RemoveBackground(Mat sourceImage, string mattingModel)
    {
        return _inferenceService.RunHumanMatting(sourceImage, mattingModel);
    }

    public Mat ApplyBackground(Mat sourceImage, BackgroundOptions options)
    {
        var color = ResolveBackgroundColor(options);
        return _inferenceService.RunAddBackground(sourceImage, color, (int)options.RenderKind);
    }

    public Mat ApplyWatermark(Mat sourceImage, WatermarkOptions options)
    {
        return _inferenceService.RunWatermark(
            sourceImage,
            options.Text,
            options.FontSize,
            options.Opacity,
            options.Angle,
            options.ColorHex,
            options.Space);
    }

    public Mat? ApplyTemplate(Mat sourceImage, TemplateOptions options)
    {
        var templateName = string.IsNullOrWhiteSpace(options.TemplateName) ? "template_1" : options.TemplateName;
        var assetsDir = ResolveTemplateAssetsDirectory(options.AssetsDirectory);
        if (string.IsNullOrWhiteSpace(assetsDir))
        {
            return null;
        }

        var configPath = Path.Combine(assetsDir, "template_config.json");
        var pngPath = Path.Combine(assetsDir, $"{templateName}.png");
        if (!File.Exists(configPath) || !File.Exists(pngPath))
        {
            return null;
        }

        var config = LoadTemplateConfig(configPath, templateName);
        if (config is null)
        {
            return null;
        }

        using var sourceBgr = EnsureBgr(sourceImage);
        using var rotated = RotateBound(sourceBgr, -config.AnchorPoints.Rotation);

        var targetWidth = config.AnchorPoints.Rotation < 0
            ? config.AnchorPoints.RightTop.X - config.AnchorPoints.LeftBottom.X
            : config.AnchorPoints.LeftBottom.X - config.AnchorPoints.RightTop.X;
        var targetHeight = config.AnchorPoints.Rotation < 0
            ? config.AnchorPoints.RightBottom.Y - config.AnchorPoints.LeftTop.Y
            : config.AnchorPoints.LeftTop.Y - config.AnchorPoints.RightBottom.Y;

        if (targetWidth <= 0 || targetHeight <= 0)
        {
            return null;
        }

        var scale = Math.Max(
            targetWidth / (double)Math.Max(1, rotated.Cols),
            targetHeight / (double)Math.Max(1, rotated.Rows));
        using var resized = new Mat();
        Cv2.Resize(rotated, resized, new Size(), scale, scale, interpolation: InterpolationFlags.Linear);

        var result = new Mat(config.Height, config.Width, MatType.CV_8UC3, new Scalar(255, 255, 255));

        var pasteX = config.AnchorPoints.LeftBottom.X;
        var pasteY = config.AnchorPoints.LeftTop.Y;
        var copyWidth = Math.Min(resized.Cols, config.Width - pasteX);
        var copyHeight = Math.Min(resized.Rows, config.Height - pasteY);

        if (copyWidth > 0 && copyHeight > 0)
        {
            using var srcRoi = new Mat(resized, new Rect(0, 0, copyWidth, copyHeight));
            using var dstRoi = new Mat(result, new Rect(pasteX, pasteY, copyWidth, copyHeight));
            srcRoi.CopyTo(dstRoi);
        }

        using var templateOverlay = Cv2.ImRead(pngPath, ImreadModes.Unchanged);
        if (!templateOverlay.Empty() && templateOverlay.Channels() == 4)
        {
            OverlayBgraOnBgr(result, templateOverlay);
        }

        return result;
    }

    public Mat GenerateLayoutSheet(Mat sourceImage, IdPhotoPixelSize photoSize, LayoutOptions options)
    {
        var paperSpec = PaperSizeCatalog.Resolve(options);
        var paperWidth = ScaleDimension(paperSpec.WidthPx, paperSpec.Dpi, options.Dpi);
        var paperHeight = ScaleDimension(paperSpec.HeightPx, paperSpec.Dpi, options.Dpi);

        using var sourceBgr = EnsureBgr(sourceImage);
        using var tile = new Mat();
        Cv2.Resize(sourceBgr, tile, new Size(photoSize.Width, photoSize.Height), interpolation: InterpolationFlags.Area);

        var margin = Math.Max(0, options.MarginPx);
        var gap = Math.Max(0, options.GapPx);

        var canvas = new Mat(paperHeight, paperWidth, MatType.CV_8UC3, new Scalar(255, 255, 255));

        var cols = Math.Max(1, (paperWidth - margin * 2 + gap) / (photoSize.Width + gap));
        var rows = Math.Max(1, (paperHeight - margin * 2 + gap) / (photoSize.Height + gap));

        var usedWidth = cols * photoSize.Width + (cols - 1) * gap;
        var usedHeight = rows * photoSize.Height + (rows - 1) * gap;
        var startX = Math.Max(margin, (paperWidth - usedWidth) / 2);
        var startY = Math.Max(margin, (paperHeight - usedHeight) / 2);

        var verticalLines = new SortedSet<int>();
        var horizontalLines = new SortedSet<int>();

        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                var x = startX + col * (photoSize.Width + gap);
                var y = startY + row * (photoSize.Height + gap);
                if (x + photoSize.Width > paperWidth || y + photoSize.Height > paperHeight)
                {
                    continue;
                }

                tile.CopyTo(canvas[new Rect(x, y, photoSize.Width, photoSize.Height)]);
                verticalLines.Add(x);
                verticalLines.Add(x + photoSize.Width);
                horizontalLines.Add(y);
                horizontalLines.Add(y + photoSize.Height);
            }
        }

        if (options.DrawCutLines)
        {
            var lineColor = new Scalar(200, 200, 200);
            const int lineThickness = 1;
            const int dashLength = 14;
            const int gapLength = 10;

            foreach (var x in verticalLines)
            {
                DrawDashedLine(
                    canvas,
                    new Point(x, 0),
                    new Point(x, paperHeight),
                    lineColor,
                    lineThickness,
                    dashLength,
                    gapLength);
            }

            foreach (var y in horizontalLines)
            {
                DrawDashedLine(
                    canvas,
                    new Point(0, y),
                    new Point(paperWidth, y),
                    lineColor,
                    lineThickness,
                    dashLength,
                    gapLength);
            }
        }

        return canvas;
    }

    public byte[] Export(Mat image, ExportOptions options)
    {
        using var exportImage = ApplyDpiScaling(image, options.Dpi);

        return options.Format switch
        {
            OutputImageFormat.Jpeg => EncodeJpeg(exportImage, options),
            _ => EncodePng(exportImage, options)
        };
    }

    public string ExportBase64(Mat image, ExportOptions options)
    {
        var bytes = Export(image, options);
        var base64 = Convert.ToBase64String(bytes);
        if (!options.IncludeDataUriPrefix)
        {
            return base64;
        }

        var mime = options.Format == OutputImageFormat.Jpeg ? "image/jpeg" : "image/png";
        return $"data:{mime};base64,{base64}";
    }

    private static Mat EnsureBgr(Mat image)
    {
        return image.Channels() switch
        {
            4 => image.CvtColor(ColorConversionCodes.BGRA2BGR),
            1 => image.CvtColor(ColorConversionCodes.GRAY2BGR),
            _ => image.Clone()
        };
    }

    private static void DrawDashedLine(
        Mat canvas,
        Point start,
        Point end,
        Scalar color,
        int thickness,
        int dashLength,
        int gapLength)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance <= 0)
        {
            return;
        }

        var ux = dx / distance;
        var uy = dy / distance;
        var step = Math.Max(1, dashLength + gapLength);

        for (var offset = 0.0; offset < distance; offset += step)
        {
            var dashEnd = Math.Min(distance, offset + dashLength);

            var x1 = (int)Math.Round(start.X + ux * offset);
            var y1 = (int)Math.Round(start.Y + uy * offset);
            var x2 = (int)Math.Round(start.X + ux * dashEnd);
            var y2 = (int)Math.Round(start.Y + uy * dashEnd);

            Cv2.Line(canvas, new Point(x1, y1), new Point(x2, y2), color, thickness, LineTypes.AntiAlias);
        }
    }

    private static int ScaleDimension(int source, int sourceDpi, int targetDpi)
    {
        if (sourceDpi <= 0 || targetDpi <= 0 || sourceDpi == targetDpi)
        {
            return source;
        }

        return Math.Max(1, (int)Math.Round(source * (targetDpi / (double)sourceDpi)));
    }

    private static Mat ApplyDpiScaling(Mat image, int targetDpi)
    {
        if (targetDpi <= 0 || targetDpi == 300)
        {
            return image.Clone();
        }

        var width = Math.Max(1, (int)Math.Round(image.Cols * (targetDpi / 300.0)));
        var height = Math.Max(1, (int)Math.Round(image.Rows * (targetDpi / 300.0)));

        var resized = new Mat();
        Cv2.Resize(image, resized, new Size(width, height), interpolation: InterpolationFlags.Area);
        return resized;
    }

    private static byte[] EncodePng(Mat image, ExportOptions options)
    {
        Cv2.ImEncode(".png", image, out var bytes);
        if (options.TargetKb <= 0)
        {
            return bytes;
        }

        var limitBytes = options.TargetKb * 1024;
        for (var compression = 9; compression >= 0; compression--)
        {
            Cv2.ImEncode(".png", image, out bytes, new[] { (int)ImwriteFlags.PngCompression, compression });
            if (bytes.Length <= limitBytes)
            {
                return bytes;
            }
        }

        return DownscaleUntilFits(image, limitBytes, ".png", options.JpegQuality);
    }

    private static byte[] EncodeJpeg(Mat image, ExportOptions options)
    {
        var clampedQuality = Math.Clamp(options.JpegQuality, 25, 100);
        Cv2.ImEncode(".jpg", image, out var bytes, new[] { (int)ImwriteFlags.JpegQuality, clampedQuality });

        if (options.TargetKb <= 0)
        {
            return bytes;
        }

        var limitBytes = options.TargetKb * 1024;
        for (var quality = clampedQuality; quality >= 25; quality -= 5)
        {
            Cv2.ImEncode(".jpg", image, out bytes, new[] { (int)ImwriteFlags.JpegQuality, quality });
            if (bytes.Length <= limitBytes)
            {
                return bytes;
            }
        }

        return DownscaleUntilFits(image, limitBytes, ".jpg", 85);
    }

    private static byte[] DownscaleUntilFits(Mat image, int byteLimit, string extension, int quality)
    {
        using var resized = image.Clone();
        var scale = 0.95;

        while (scale >= 0.55)
        {
            var width = Math.Max(1, (int)(image.Cols * scale));
            var height = Math.Max(1, (int)(image.Rows * scale));
            Cv2.Resize(image, resized, new Size(width, height), interpolation: InterpolationFlags.Area);

            if (extension == ".jpg")
            {
                Cv2.ImEncode(extension, resized, out var bytes, new[] { (int)ImwriteFlags.JpegQuality, Math.Clamp(quality, 25, 100) });
                if (bytes.Length <= byteLimit)
                {
                    return bytes;
                }
            }
            else
            {
                Cv2.ImEncode(extension, resized, out var bytes, new[] { (int)ImwriteFlags.PngCompression, 9 });
                if (bytes.Length <= byteLimit)
                {
                    return bytes;
                }
            }

            scale -= 0.05;
        }

        Cv2.ImEncode(extension, image, out var fallbackBytes);
        return fallbackBytes;
    }

    private static string ResolveBackgroundColor(BackgroundOptions options)
    {
        if (options.AmericanStyleBackground)
        {
            return "FFFFFF";
        }

        return string.IsNullOrWhiteSpace(options.PrimaryColorHex)
            ? "638CCE"
            : options.PrimaryColorHex;
    }

    private static void OverlayBgraOnBgr(Mat backgroundBgr, Mat overlayBgra)
    {
        var h = Math.Min(backgroundBgr.Rows, overlayBgra.Rows);
        var w = Math.Min(backgroundBgr.Cols, overlayBgra.Cols);

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var fg = overlayBgra.At<Vec4b>(y, x);
                if (fg.Item3 == 0)
                {
                    continue;
                }

                var bg = backgroundBgr.At<Vec3b>(y, x);
                var alpha = fg.Item3 / 255.0;

                backgroundBgr.Set(y, x, new Vec3b(
                    (byte)Math.Clamp(bg.Item0 * (1.0 - alpha) + fg.Item0 * alpha, 0.0, 255.0),
                    (byte)Math.Clamp(bg.Item1 * (1.0 - alpha) + fg.Item1 * alpha, 0.0, 255.0),
                    (byte)Math.Clamp(bg.Item2 * (1.0 - alpha) + fg.Item2 * alpha, 0.0, 255.0)));
            }
        }
    }

    private static Mat RotateBound(Mat image, double angle)
    {
        var h = image.Rows;
        var w = image.Cols;
        var center = new Point2f(w / 2f, h / 2f);
        using var rotationMatrix = Cv2.GetRotationMatrix2D(center, -angle, 1.0);

        var cos = Math.Abs(rotationMatrix.At<double>(0, 0));
        var sin = Math.Abs(rotationMatrix.At<double>(0, 1));
        var newW = (int)((h * sin) + (w * cos));
        var newH = (int)((h * cos) + (w * sin));

        rotationMatrix.Set(0, 2, rotationMatrix.At<double>(0, 2) + (newW / 2.0) - center.X);
        rotationMatrix.Set(1, 2, rotationMatrix.At<double>(1, 2) + (newH / 2.0) - center.Y);

        var rotated = new Mat();
        Cv2.WarpAffine(image, rotated, rotationMatrix, new Size(newW, newH));
        return rotated;
    }

    private static string? ResolveTemplateAssetsDirectory(string? requestedAssetsDirectory)
    {
        var baseDir = AppContext.BaseDirectory;

        if (!string.IsNullOrWhiteSpace(requestedAssetsDirectory))
        {
            var resolved = Path.IsPathRooted(requestedAssetsDirectory)
                ? requestedAssetsDirectory
                : Path.GetFullPath(Path.Combine(baseDir, requestedAssetsDirectory));

            if (Directory.Exists(resolved))
            {
                return resolved;
            }
        }

        var envAssetsDir = Environment.GetEnvironmentVariable("LIGHT_TEMPLATE_ASSETS_DIR");
        if (!string.IsNullOrWhiteSpace(envAssetsDir))
        {
            var resolved = Path.IsPathRooted(envAssetsDir)
                ? envAssetsDir
                : Path.GetFullPath(Path.Combine(baseDir, envAssetsDir));

            if (Directory.Exists(resolved))
            {
                return resolved;
            }
        }

        // Default location is the host application's BaseDirectory.
        return baseDir;
    }

    private static TemplateConfigEntry? LoadTemplateConfig(string configPath, string templateName)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(configPath));
            if (!json.RootElement.TryGetProperty(templateName, out var templateObj))
            {
                return null;
            }

            var width = templateObj.GetProperty("width").GetInt32();
            var height = templateObj.GetProperty("height").GetInt32();
            var anchor = templateObj.GetProperty("anchor_points");

            return new TemplateConfigEntry
            {
                Width = width,
                Height = height,
                AnchorPoints = new TemplateAnchorPoints
                {
                    LeftTop = ReadPoint(anchor.GetProperty("left_top")),
                    RightTop = ReadPoint(anchor.GetProperty("right_top")),
                    LeftBottom = ReadPoint(anchor.GetProperty("left_bottom")),
                    RightBottom = ReadPoint(anchor.GetProperty("right_bottom")),
                    Rotation = anchor.GetProperty("rotation").GetDouble()
                }
            };
        }
        catch
        {
            return null;
        }
    }

    private static Point ReadPoint(JsonElement pointArray)
    {
        return new Point(pointArray[0].GetInt32(), pointArray[1].GetInt32());
    }

    private sealed class TemplateConfigEntry
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public TemplateAnchorPoints AnchorPoints { get; init; } = new();
    }

    private sealed class TemplateAnchorPoints
    {
        public Point LeftTop { get; init; }
        public Point RightTop { get; init; }
        public Point LeftBottom { get; init; }
        public Point RightBottom { get; init; }
        public double Rotation { get; init; }
    }
}
