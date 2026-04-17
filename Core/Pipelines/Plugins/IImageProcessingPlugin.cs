using System;
using OpenCvSharp;

namespace HivisionIDPhotos.Core.Pipelines.Plugins
{
    // Every Plugin is mathematically self-contained
    public interface IImageProcessingPlugin : IDisposable
    {
        string Name { get; }
        
        // Returns a NEW Mat and safely disposes intermediate memory
        Mat Process(Mat inputImage);
    }
}
