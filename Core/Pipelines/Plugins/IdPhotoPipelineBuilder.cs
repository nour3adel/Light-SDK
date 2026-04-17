using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace HivisionIDPhotos.Core.Pipelines.Plugins
{
    public class IdPhotoPipelineBuilder
    {
        private readonly List<IImageProcessingPlugin> _pipeline = new();

        // Fluent API Builder Methods
        public IdPhotoPipelineBuilder ApplyBeauty(int brightness, int contrast, int saturation, int sharpen)
        {
            _pipeline.Add(new BeautyPlugin(brightness, contrast, saturation, sharpen));
            return this;
        }

        public IdPhotoPipelineBuilder ApplyWhitening(int strength)
        {
            _pipeline.Add(new WhiteningPlugin(strength));
            return this;
        }

        // Add dynamically more later if you wish: ApplyWatermark etc.

        // High-Performance Engine Executor
        public Mat Execute(Mat originalImage)
        {
            if (_pipeline.Count == 0) return originalImage.Clone();

            // Start processing natively
            Mat currentExecution = originalImage.Clone();

            try
            {
                foreach (var plugin in _pipeline)
                {
                    // Run the plugin
                    Mat nextExecution = plugin.Process(currentExecution);
                    
                    // CRITICAL for high-load servers: 
                    // Dispose intermediate states, but NEVER dispose the explicit orginal source reference blindly
                    if (currentExecution != nextExecution && currentExecution != originalImage)
                    {
                        currentExecution.Dispose(); 
                    }
                    currentExecution = nextExecution;
                }

                return currentExecution;
            }
            finally
            {
                // The Builder guarantees RAM is flushed after execution for plugins
                foreach (var plugin in _pipeline)
                {
                    plugin.Dispose();
                }
            }
        }
    }
}