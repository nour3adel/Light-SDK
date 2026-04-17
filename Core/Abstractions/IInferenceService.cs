using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenCvSharp;
using HivisionIDPhotos.Core.Models;

namespace HivisionIDPhotos.Core.Abstractions
{
    public interface IInferenceService
    {
        Task<Mat> ParseImageAsync(IFormFile? inputImage, string? inputImageBase64, ImreadModes mode = ImreadModes.Color);
        string EncodeToBase64(Mat img, int kb = 0);
        Mat RunHumanMatting(Mat srcImg, string mattingModel = "hivision_modnet");
        Mat RunAddBackground(Mat srcImg, string colorHex, int renderMode = 0);
        Mat RunWatermark(Mat srcImg, string text, int fontSize, double opacity, int angle, string colorHex, int space);
        (Mat standardImg, Mat hdImg) RunIdPhoto(Mat srcImg, IdPhotoRequestOptions options);
        (Mat standardImg, Mat hdImg) RunIdPhoto(
            Mat srcImg,
            int height,
            int width,
            string backgroundColor,
            string mattingModel,
            string faceDetectModel,
            double headRatio,
            double topDistance,
            int whiteningStrength,
            int brightnessStrength,
            int contrastStrength,
            int saturationStrength,
            int sharpenStrength,
            int renderMode,
            bool faceAlign = false,
            double headHeightRatio = 0.45,
            double topDistanceMin = 0.10);

        (Mat standardImg, Mat hdImg) RunIdPhoto(
            Mat srcImg,
            int height,
            int width,
            string backgroundColor,
            string mattingModel,
            double headRatio,
            double topDistance,
            int whiteningStrength,
            int brightnessStrength,
            int contrastStrength,
            int saturationStrength,
            int sharpenStrength,
            int renderMode,
            bool faceAlign = false,
            double headHeightRatio = 0.45,
            double topDistanceMin = 0.10);
        (Mat standardImg, Mat hdImg) RunIdPhotoCrop(
            Mat srcImg,
            int height,
            int width,
            double headRatio,
            double topDistance,
            double headHeightRatio = 0.45,
            double topDistanceMin = 0.10,
            bool faceAlign = false);
        Mat RunGenerateLayout(Mat srcImg, int height, int width);
        Task<(int successCount, int failureCount)> ProcessFolderBackgroundRemovalAsync(
            string sourceFolder,
            string outputFolder,
            string mattingModel);
    }
}
