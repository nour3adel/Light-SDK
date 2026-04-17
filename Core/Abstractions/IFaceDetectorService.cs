using OpenCvSharp;
using HivisionIDPhotos.Core.Services;

namespace HivisionIDPhotos.Core.Abstractions
{
    public interface IFaceDetectorService
    {
        Rect? DetectFace(Mat srcImg);
        Rect? DetectFace(Mat srcImg, string? modelName);
        FaceDetectionInfo DetectFaceInfo(Mat srcImg);
        FaceDetectionInfo DetectFaceInfo(Mat srcImg, string? modelName);
    }
}
