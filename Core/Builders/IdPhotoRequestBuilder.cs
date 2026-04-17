using HivisionIDPhotos.Core.Models;

namespace HivisionIDPhotos.Core.Builders
{
    public sealed class IdPhotoRequestBuilder
    {
        private readonly IdPhotoRequestOptions _options = new();

        public IdPhotoRequestBuilder WithSize(int width, int height)
        {
            _options.Width = width;
            _options.Height = height;
            return this;
        }

        public IdPhotoRequestBuilder WithBackground(string backgroundColor, int renderMode = 0)
        {
            _options.BackgroundColor = backgroundColor;
            _options.RenderMode = renderMode;
            return this;
        }

        public IdPhotoRequestBuilder WithMatting(string mattingModel)
        {
            _options.MattingModel = mattingModel;
            return this;
        }

        public IdPhotoRequestBuilder WithFaceDetection(string faceDetectModel)
        {
            _options.FaceDetectModel = faceDetectModel;
            return this;
        }

        public IdPhotoRequestBuilder WithFaceLayout(double headRatio, double topDistance, double headHeightRatio = 0.45, double topDistanceMin = 0.10)
        {
            _options.HeadRatio = headRatio;
            _options.TopDistance = topDistance;
            _options.HeadHeightRatio = headHeightRatio;
            _options.TopDistanceMin = topDistanceMin;
            return this;
        }

        public IdPhotoRequestBuilder WithEnhancement(int whitening, int brightness, int contrast, int saturation, int sharpen)
        {
            _options.WhiteningStrength = whitening;
            _options.BrightnessStrength = brightness;
            _options.ContrastStrength = contrast;
            _options.SaturationStrength = saturation;
            _options.SharpenStrength = sharpen;
            return this;
        }

        public IdPhotoRequestBuilder EnableFaceAlign(bool enabled = true)
        {
            _options.FaceAlign = enabled;
            return this;
        }

        public IdPhotoRequestOptions Build() => _options;
    }
}
