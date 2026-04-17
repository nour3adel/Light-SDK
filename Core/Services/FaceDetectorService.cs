using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using HivisionIDPhotos.Core.Abstractions;

namespace HivisionIDPhotos.Core.Services
{
    public readonly record struct FaceDetectionInfo(Rect Rectangle, double RollAngle);

    public class FaceDetectorService : IFaceDetectorService, IDisposable
    {
        private static readonly object RetinaSessionLock = new();
        private static InferenceSession? _retinaSession;
        private readonly CascadeClassifier _cascadeClassifier;
        private readonly bool _allowHaarFallback;
        private bool _disposed = false;

        public FaceDetectorService()
        {
            _allowHaarFallback = string.Equals(
                Environment.GetEnvironmentVariable("HIVISION_ALLOW_HAAR_FALLBACK"),
                "true",
                StringComparison.OrdinalIgnoreCase
            );

            var cascadePath = Path.Combine(AppContext.BaseDirectory, "haarcascade_frontalface_default.xml");
            cascadePath = Path.GetFullPath(cascadePath);

            if (!File.Exists(cascadePath))
            {
                throw new FileNotFoundException("Cascade classifier not found at: " + cascadePath);
            }

            _cascadeClassifier = new CascadeClassifier(cascadePath);
            
            if (_cascadeClassifier.Empty())
            {
                throw new InvalidOperationException("Failed to load cascade classifier from: " + cascadePath);
            }
        }

        public Rect? DetectFace(Mat srcImg)
        {
            return DetectFace(srcImg, null);
        }

        public Rect? DetectFace(Mat srcImg, string? modelName)
        {
            return DetectFaceInfo(srcImg, modelName).Rectangle;
        }

        public FaceDetectionInfo DetectFaceInfo(Mat srcImg)
        {
            return DetectFaceInfo(srcImg, null);
        }

        public FaceDetectionInfo DetectFaceInfo(Mat srcImg, string? modelName)
        {
            if (srcImg.Empty())
            {
                throw new Exception("No face detected in image");
            }

            var detectorModel = NormalizeDetectorModelName(modelName);
            if (detectorModel == "haar")
            {
                return DetectWithHaar(srcImg);
            }

            var onnxFaces = TryDetectWithRetinaFace(srcImg);
            if (onnxFaces.Count == 1)
            {
                return new FaceDetectionInfo(onnxFaces[0].Rectangle, onnxFaces[0].RollAngle);
            }
            if (onnxFaces.Count > 1)
            {
                throw new Exception("More than one face detected in the image");
            }

            if (detectorModel == "retinaface")
            {
                throw new Exception("No face detected in image");
            }

            if (!_allowHaarFallback)
            {
                throw new Exception("No face detected in image");
            }

            return DetectWithHaar(srcImg);
        }

        private FaceDetectionInfo DetectWithHaar(Mat srcImg)
        {
            using var gray = new Mat();
            switch (srcImg.Channels())
            {
                case 1:
                    srcImg.CopyTo(gray);
                    break;
                case 4:
                    Cv2.CvtColor(srcImg, gray, ColorConversionCodes.BGRA2GRAY);
                    break;
                default:
                    Cv2.CvtColor(srcImg, gray, ColorConversionCodes.BGR2GRAY);
                    break;
            }

            using var equalized = new Mat();
            Cv2.EqualizeHist(gray, equalized);

            var minDim = Math.Min(srcImg.Rows, srcImg.Cols);
            var baseMin = Math.Max(20, minDim / 12);

            var candidates = new List<Rect>();
            candidates.AddRange(_cascadeClassifier.DetectMultiScale(
                gray,
                scaleFactor: 1.08,
                minNeighbors: 3,
                flags: HaarDetectionTypes.ScaleImage,
                minSize: new OpenCvSharp.Size(baseMin, baseMin)
            ));
            candidates.AddRange(_cascadeClassifier.DetectMultiScale(
                equalized,
                scaleFactor: 1.05,
                minNeighbors: 3,
                flags: HaarDetectionTypes.ScaleImage,
                minSize: new OpenCvSharp.Size(Math.Max(16, baseMin / 2), Math.Max(16, baseMin / 2))
            ));

            var distinctFaces = SuppressOverlaps(candidates, 0.35);

            if (distinctFaces.Count == 0)
            {
                throw new Exception("No face detected in image");
            }

            if (distinctFaces.Count > 1)
            {
                throw new Exception("More than one face detected in the image");
            }

            return new FaceDetectionInfo(distinctFaces[0], 0.0);
        }

        private static string NormalizeDetectorModelName(string? modelName)
        {
            var normalized = (modelName ?? "auto").Trim().ToLowerInvariant();
            return normalized switch
            {
                "" => "auto",
                "auto" => "auto",
                "retina" => "retinaface",
                "retinaface" => "retinaface",
                "mtcnn" => "retinaface",
                "face++" => "retinaface",
                "haar" => "haar",
                "haarcascade" => "haar",
                _ => "auto"
            };
        }

        private List<FaceDetectionInfo> TryDetectWithRetinaFace(Mat srcImg)
        {
            try
            {
                using var bgr = srcImg.Channels() switch
                {
                    4 => srcImg.CvtColor(ColorConversionCodes.BGRA2BGR),
                    1 => srcImg.CvtColor(ColorConversionCodes.GRAY2BGR),
                    _ => srcImg.Clone()
                };

                var session = GetRetinaSession();
                var input = BuildRetinaInputTensor(bgr);
                using var results = session.Run(new[]
                {
                    NamedOnnxValue.CreateFromTensor("input", input)
                });

                var outputs = results.Select(r => r.AsTensor<float>()).ToArray();
                if (outputs.Length < 3)
                {
                    return new List<FaceDetectionInfo>();
                }

                var priors = GeneratePriors(bgr.Rows, bgr.Cols);
                var detections = DecodeRetinaDetections(outputs[0], outputs[1], outputs[2], priors, bgr.Rows, bgr.Cols);

                return detections
                    .Where(d => d.Score > 0.80f)
                    .OrderByDescending(d => d.Score)
                    .Take(5000)
                    .ApplyRetinaNms(0.2f)
                    .Take(750)
                    .Select(d => new FaceDetectionInfo(
                        new Rect(
                            (int)d.X1,
                            (int)d.Y1,
                            Math.Max(1, (int)(d.X2 - d.X1 + 1)),
                            Math.Max(1, (int)(d.Y2 - d.Y1 + 1))),
                        d.RollAngle))
                    .Where(r => r.Rectangle.Width > 0 && r.Rectangle.Height > 0)
                    .ToList();
            }
            catch
            {
                // Keep pipeline resilient: if ONNX fails at runtime, optional Haar fallback can still run.
                return new List<FaceDetectionInfo>();
            }
        }

        private static InferenceSession GetRetinaSession()
        {
            lock (RetinaSessionLock)
            {
                if (_retinaSession is not null)
                {
                    return _retinaSession;
                }

                var modelPath = ResolveRetinaModelPath();
                _retinaSession = new InferenceSession(modelPath);
                return _retinaSession;
            }
        }

        private static string ResolveRetinaModelPath()
        {
            var envPath = Environment.GetEnvironmentVariable("RETINAFACE_MODEL_PATH");
            if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
            {
                return envPath;
            }

            var baseDir = AppContext.BaseDirectory;
            var roots = new List<string>();

            var configuredRoot = Environment.GetEnvironmentVariable("LIGHT_SDK_MODELS_ROOT");
            if (!string.IsNullOrWhiteSpace(configuredRoot))
            {
                roots.Add(configuredRoot);
            }

            var detectorDirFromEnv = Environment.GetEnvironmentVariable("MATTING_MODEL_DIR");
            if (!string.IsNullOrWhiteSpace(detectorDirFromEnv))
            {
                var parent = Directory.GetParent(detectorDirFromEnv);
                if (parent is not null)
                {
                    roots.Add(parent.FullName);
                }
            }

            roots.Add(Path.Combine(baseDir, "models"));

            var cursor = new DirectoryInfo(baseDir);
            while (cursor is not null)
            {
                roots.Add(Path.Combine(cursor.FullName, "models"));
                cursor = cursor.Parent;
            }

            var found = roots
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .SelectMany(r => new[]
                {
                    Path.Combine(r, "detector models", "Light_faceDetect.lsdkm"),
                    Path.Combine(r, "detector models", "retinaface-resnet50.onnx")
                })
                .FirstOrDefault(File.Exists);

            if (found is null)
            {
                throw new FileNotFoundException("RetinaFace model not found. Set IdCreatorOptions.RetinaFaceModelPath or IdCreatorOptions.ModelsRootPath (or RETINAFACE_MODEL_PATH). Expected file: models/detector models/Light_faceDetect.lsdkm or retinaface-resnet50.onnx.");
            }

            return found;
        }

        private static DenseTensor<float> BuildRetinaInputTensor(Mat bgr)
        {
            int h = bgr.Rows;
            int w = bgr.Cols;
            var tensor = new DenseTensor<float>(new[] { 1, 3, h, w });

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var px = bgr.At<Vec3b>(y, x);
                    tensor[0, 0, y, x] = px.Item2 - 104f;
                    tensor[0, 1, y, x] = px.Item1 - 117f;
                    tensor[0, 2, y, x] = px.Item0 - 123f;
                }
            }

            return tensor;
        }

        private static List<Prior> GeneratePriors(int imageHeight, int imageWidth)
        {
            int[][] minSizes =
            {
                new[] { 16, 32 },
                new[] { 64, 128 },
                new[] { 256, 512 }
            };
            int[] steps = { 8, 16, 32 };
            var priors = new List<Prior>();

            for (int k = 0; k < steps.Length; k++)
            {
                int featureH = (int)Math.Ceiling(imageHeight / (double)steps[k]);
                int featureW = (int)Math.Ceiling(imageWidth / (double)steps[k]);

                for (int i = 0; i < featureH; i++)
                {
                    for (int j = 0; j < featureW; j++)
                    {
                        foreach (int minSize in minSizes[k])
                        {
                            priors.Add(new Prior(
                                (j + 0.5f) * steps[k] / imageWidth,
                                (i + 0.5f) * steps[k] / imageHeight,
                                minSize / (float)imageWidth,
                                minSize / (float)imageHeight));
                        }
                    }
                }
            }

            return priors;
        }

        private static List<Detection> DecodeRetinaDetections(
            Tensor<float> loc,
            Tensor<float> conf,
            Tensor<float> landms,
            List<Prior> priors,
            int imageHeight,
            int imageWidth)
        {
            const float variance0 = 0.1f;
            const float variance1 = 0.2f;
            int count = Math.Min(priors.Count, loc.Dimensions[1]);
            var detections = new List<Detection>(count);

            for (int i = 0; i < count; i++)
            {
                var prior = priors[i];
                float centerX = prior.Cx + loc[0, i, 0] * variance0 * prior.Sx;
                float centerY = prior.Cy + loc[0, i, 1] * variance0 * prior.Sy;
                float boxW = prior.Sx * MathF.Exp(loc[0, i, 2] * variance1);
                float boxH = prior.Sy * MathF.Exp(loc[0, i, 3] * variance1);

                float x1 = (centerX - boxW / 2f) * imageWidth;
                float y1 = (centerY - boxH / 2f) * imageHeight;
                float x2 = (centerX + boxW / 2f) * imageWidth;
                float y2 = (centerY + boxH / 2f) * imageHeight;
                var landmarks = DecodeLandmarks(landms, prior, i, imageWidth, imageHeight);
                double rollAngle = Math.Atan2(
                    landmarks.RightEyeY - landmarks.LeftEyeY,
                    landmarks.RightEyeX - landmarks.LeftEyeX) * 180.0 / Math.PI;

                detections.Add(new Detection(
                    Math.Clamp(x1, 0, imageWidth - 1),
                    Math.Clamp(y1, 0, imageHeight - 1),
                    Math.Clamp(x2, 0, imageWidth - 1),
                    Math.Clamp(y2, 0, imageHeight - 1),
                    conf[0, i, 1],
                    rollAngle));
            }

            return detections;
        }

        private static (float LeftEyeX, float LeftEyeY, float RightEyeX, float RightEyeY) DecodeLandmarks(
            Tensor<float> landms,
            Prior prior,
            int index,
            int imageWidth,
            int imageHeight)
        {
            const float variance0 = 0.1f;
            float leftEyeX = (prior.Cx + landms[0, index, 0] * variance0 * prior.Sx) * imageWidth;
            float leftEyeY = (prior.Cy + landms[0, index, 1] * variance0 * prior.Sy) * imageHeight;
            float rightEyeX = (prior.Cx + landms[0, index, 2] * variance0 * prior.Sx) * imageWidth;
            float rightEyeY = (prior.Cy + landms[0, index, 3] * variance0 * prior.Sy) * imageHeight;

            return (leftEyeX, leftEyeY, rightEyeX, rightEyeY);
        }

        private static List<Rect> SuppressOverlaps(IEnumerable<Rect> faces, double iouThreshold)
        {
            var sorted = faces
                .Where(r => r.Width > 0 && r.Height > 0)
                .OrderByDescending(r => r.Width * r.Height)
                .ToList();

            var kept = new List<Rect>();

            foreach (var face in sorted)
            {
                var overlaps = kept.Any(k => IoU(face, k) > iouThreshold);
                if (!overlaps)
                {
                    kept.Add(face);
                }
            }

            return kept;
        }

        private static double IoU(Rect a, Rect b)
        {
            var x1 = Math.Max(a.X, b.X);
            var y1 = Math.Max(a.Y, b.Y);
            var x2 = Math.Min(a.Right, b.Right);
            var y2 = Math.Min(a.Bottom, b.Bottom);

            var interW = Math.Max(0, x2 - x1);
            var interH = Math.Max(0, y2 - y1);
            var interArea = interW * interH;

            var areaA = a.Width * a.Height;
            var areaB = b.Width * b.Height;
            var union = areaA + areaB - interArea;

            if (union <= 0)
            {
                return 0.0;
            }

            return (double)interArea / union;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cascadeClassifier?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        private readonly record struct Prior(float Cx, float Cy, float Sx, float Sy);

        internal readonly record struct Detection(float X1, float Y1, float X2, float Y2, float Score, double RollAngle);
    }

    internal static class RetinaFaceDetectionExtensions
    {
        public static IEnumerable<FaceDetectorService.Detection> ApplyRetinaNms(
            this IEnumerable<FaceDetectorService.Detection> detections,
            float threshold)
        {
            var ordered = detections.OrderByDescending(d => d.Score).ToList();
            var keep = new List<FaceDetectorService.Detection>();

            while (ordered.Count > 0)
            {
                var current = ordered[0];
                keep.Add(current);
                ordered.RemoveAt(0);
                ordered = ordered.Where(candidate => RetinaIoU(current, candidate) <= threshold).ToList();
            }

            return keep;
        }

        private static float RetinaIoU(FaceDetectorService.Detection a, FaceDetectorService.Detection b)
        {
            float xx1 = MathF.Max(a.X1, b.X1);
            float yy1 = MathF.Max(a.Y1, b.Y1);
            float xx2 = MathF.Min(a.X2, b.X2);
            float yy2 = MathF.Min(a.Y2, b.Y2);

            float w = MathF.Max(0f, xx2 - xx1 + 1f);
            float h = MathF.Max(0f, yy2 - yy1 + 1f);
            float inter = w * h;
            float areaA = (a.X2 - a.X1 + 1f) * (a.Y2 - a.Y1 + 1f);
            float areaB = (b.X2 - b.X1 + 1f) * (b.Y2 - b.Y1 + 1f);
            float union = areaA + areaB - inter;

            return union <= 0f ? 0f : inter / union;
        }
    }
}
