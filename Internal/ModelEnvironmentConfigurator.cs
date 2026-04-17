using System;
using System.IO;
using System.Linq;

namespace Light.SDK.Internal;

internal static class ModelEnvironmentConfigurator
{
    public static void Configure(IdCreatorOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        // By default, the NuGet target copies bundled models under the app output models folder.
        var root = options.ModelsRootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "models");
        }

        root = Path.GetFullPath(root);
        Environment.SetEnvironmentVariable("LIGHT_SDK_MODELS_ROOT", root);

        var detectorModel = string.IsNullOrWhiteSpace(options.RetinaFaceModelPath)
            ? Path.Combine(root, "detector models", "Light_faceDetect.lsdkm")
            : Path.GetFullPath(options.RetinaFaceModelPath);

        var detectorFallbackOnnx = string.IsNullOrWhiteSpace(options.RetinaFaceModelPath)
            ? Path.Combine(root, "detector models", "retinaface-resnet50.onnx")
            : Path.ChangeExtension(Path.GetFullPath(options.RetinaFaceModelPath), ".onnx");

        var mattingRoot = string.IsNullOrWhiteSpace(options.MattingModelsDirectory)
            ? Path.Combine(root, "matting models")
            : Path.GetFullPath(options.MattingModelsDirectory);

        if (File.Exists(detectorModel))
        {
            Environment.SetEnvironmentVariable("RETINAFACE_MODEL_PATH", detectorModel);
        }
        else if (File.Exists(detectorFallbackOnnx))
        {
            Environment.SetEnvironmentVariable("RETINAFACE_MODEL_PATH", detectorFallbackOnnx);
        }
        else
        {
            // Clear stale values when custom options point to missing files.
            Environment.SetEnvironmentVariable("RETINAFACE_MODEL_PATH", null);
        }

        if (Directory.Exists(mattingRoot))
        {
            Environment.SetEnvironmentVariable("MATTING_MODEL_DIR", mattingRoot);
        }
        else
        {
            Environment.SetEnvironmentVariable("MATTING_MODEL_DIR", null);
        }
    }
}
