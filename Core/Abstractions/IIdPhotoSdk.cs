using HivisionIDPhotos.Core.Models.Sdk;
using OpenCvSharp;

namespace HivisionIDPhotos.Core.Abstractions;

public interface IIdPhotoSdk
{
    IdPhotoGenerationResult Generate(Mat sourceImage, IdPhotoGenerationOptions options);
    Mat RemoveBackground(Mat sourceImage, string mattingModel);
    Mat ApplyBackground(Mat sourceImage, BackgroundOptions options);
    Mat ApplyWatermark(Mat sourceImage, WatermarkOptions options);
    Mat? ApplyTemplate(Mat sourceImage, TemplateOptions options);
    Mat GenerateLayoutSheet(Mat sourceImage, IdPhotoPixelSize photoSize, LayoutOptions options);
    byte[] Export(Mat image, ExportOptions options);
    string ExportBase64(Mat image, ExportOptions options);
}
