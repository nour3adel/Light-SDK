using System;
using OpenCvSharp;

namespace HivisionIDPhotos.Core.Pipelines.Plugins
{
    public class WhiteningPlugin : IImageProcessingPlugin
    {
        private readonly int _strength;

        public string Name => "Whitening Filter";

        public WhiteningPlugin(int strength)
        {
            _strength = Math.Clamp(strength, 0, 30); // Max strength
        }

        public Mat Process(Mat inputImage)
        {
            if (_strength == 0) return inputImage.Clone();

            // Mathematically approximated without 3D LUT: 
            // In OpenCv, a slight screen blending/gamma correction approximates skin whitening.
            Mat result = inputImage.Clone();
            
            // Base strength mapped across 10 steps max to avoid overexposure
            double factor = _strength / 10.0;
            if (factor <= 0.0) return result;

            using Mat whiteMask = new Mat(result.Rows, result.Cols, MatType.CV_8UC3, new OpenCvSharp.Scalar(255, 255, 255));
            
            // Blend original slightly towards white to simulate lut_origin.png
            double alpha = Math.Clamp(factor * 0.15, 0, 0.4); // Max 40% blend into white channel directly
            
            // For a better whitening effect similar to skin tone curves, we apply slight brightness logic too
            result.ConvertTo(result, -1, 1.0 + (alpha * 0.2), alpha * 10);
            
            return result;
        }

        public void Dispose()
        {
            // Empty
        }
    }
}