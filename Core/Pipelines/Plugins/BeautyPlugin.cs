using System;
using OpenCvSharp;

namespace HivisionIDPhotos.Core.Pipelines.Plugins
{
    public class BeautyPlugin : IImageProcessingPlugin
    {
        private readonly int _brightness;
        private readonly int _contrast;
        private readonly int _saturation;
        private readonly int _sharpen;

        public string Name => "Beauty Filter";

        public BeautyPlugin(int brightness, int contrast, int saturation, int sharpen)
        {
            _brightness = brightness;
            _contrast = contrast;
            _saturation = saturation;
            _sharpen = sharpen;
        }

        public Mat Process(Mat inputImage)
        {
            if (_brightness == 0 && _contrast == 0 && _saturation == 0 && _sharpen == 0) 
            {
                return inputImage.Clone(); // Pass-through for max speed
            }
                
            Mat result = inputImage.Clone();

            // 1. Saturation adjustment (Matches base_adjust.py -> adjust_saturation)
            if (_saturation != 0)
            {
                using Mat hsv = new Mat();
                Cv2.CvtColor(result, hsv, ColorConversionCodes.BGR2HSV);
                Mat[] channels = Cv2.Split(hsv);
                
                // Mathematically scale the S channel
                using Mat satFloat = new Mat();
                channels[1].ConvertTo(satFloat, MatType.CV_32F);
                
                using Mat satAdjusted = satFloat + (satFloat * (_saturation / 100.0f));
                
                // Clamp and finalize natively
                using Mat satClamped = new Mat();
                Cv2.Min(satAdjusted, 255.0, satClamped);
                Cv2.Max(satClamped, 0.0, satClamped);
                satClamped.ConvertTo(channels[1], MatType.CV_8U);
                
                Cv2.Merge(channels, hsv);
                Cv2.CvtColor(hsv, result, ColorConversionCodes.HSV2BGR);

                foreach (var c in channels) c.Dispose();
            }

            // 2. Adjust Brightness & Contrast (Matches base_adjust.py -> convertScaleAbs)
            if (_brightness != 0 || _contrast != 0)
            {
                double alpha = 1.0 + (_contrast / 100.0);
                double beta = _brightness;
                result.ConvertTo(result, -1, alpha, beta);
            }

            // 3. Sharpen (Matches base_adjust.py -> sharpen_image)
            if (_sharpen > 0)
            {
                double kernelStrength = 1.0 + (_sharpen * 20.0 / 500.0);
                float[] kernelData = { 
                    -0.5f, -0.5f, -0.5f, 
                    -0.5f,  5.0f, -0.5f, 
                    -0.5f, -0.5f, -0.5f 
                };
                using Mat kernel = Mat.FromPixelData(3, 3, MatType.CV_32FC1, kernelData) * kernelStrength;
                
                using Mat sharpened = new Mat();
                Cv2.Filter2D(result, sharpened, -1, kernel);
                
                // Blend natively
                double alphaBlend = (_sharpen * 20.0) / 200.0;
                Cv2.AddWeighted(result, 1 - alphaBlend, sharpened, alphaBlend, 0, result);
            }

            return result;
        }

        public void Dispose() 
        { 
            // Cleanup state if any LUTs or large objects were cached
        }
    }
}