using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using HivisionIDPhotos.Core.Abstractions;
using HivisionIDPhotos.Core.Models;
using HivisionIDPhotos.Core.Pipelines.Plugins;

namespace HivisionIDPhotos.Core.Services
{
    public class InferenceService : IInferenceService
    {
        private readonly IFaceDetectorService _faceDetectorService;
        private static readonly object SessionLock = new object();
        private static readonly Dictionary<string, InferenceSession> MattingSessions = new(StringComparer.OrdinalIgnoreCase);
        private static readonly IReadOnlyDictionary<string, string> MattingModelFileMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["hivision_modnet"] = "hivision_modnet.onnx",
                ["modnet_photographic_portrait_matting"] = "modnet_photographic_portrait_matting.onnx",
                ["rmbg-1.4"] = "rmbg-1.4.onnx",
                ["bria-rmbg"] = "bria-rmbg-2.0.onnx",
                ["bria-rmbg-2.0"] = "bria-rmbg-2.0.onnx",
                ["birefnet-v1-lite"] = "birefnet-v1-lite.onnx",
                ["da"] = "depth_anything_v2_vits.onnx",
                 ["dam"] = "depth_anything_v2_vitb.onnx",
                ["birefnet-general"] = "birefnet-general.onnx",
                ["birefnet-general-lite"] = "birefnet-general-lite.onnx",
                ["birefnet-portrait"] = "birefnet-portrait.onnx",
                ["birefnet-dis"] = "birefnet-dis.onnx",
                ["birefnet-hrsod"] = "birefnet-hrsod.onnx",
                ["u2net"] = "u2net.onnx",
                ["u2netp"] = "u2netp.onnx",
                ["u2net_human_seg"] = "u2net_human_seg.onnx",
                ["u2net_cloth_seg"] = "u2net_cloth_seg.onnx",
                ["u2net-portrait-matting"] = "u2net-portrait-matting.onnx",
                ["silueta"] = "silueta.onnx",
                ["isnet-general-use"] = "isnet-general-use.onnx",
                ["isnet-anime"] = "isnet-anime.onnx"
            };

        private static readonly IReadOnlyDictionary<string, string> MattingModelAliasMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["hivision_modnet.onnx"] = "Light.Huma.lsdkm",
                ["modnet_photographic_portrait_matting.onnx"] = "Light.lite.lsdkm",
                ["rmbg-1.4.onnx"] = "Light.M01.lsdkm",
                ["bria-rmbg-2.0.onnx"] = "Light.M02.lsdkm",
                ["birefnet-v1-lite.onnx"] = "Light.M03.lsdkm",
                ["depth_anything_v2_vits.onnx"] = "Light.M04.lsdkm",
                ["depth_anything_v2_vitb.onnx"] = "Light.M05.lsdkm",
                ["birefnet-general.onnx"] = "Light.M06.lsdkm",
                ["birefnet-general-lite.onnx"] = "Light.M07.lsdkm",
                ["birefnet-portrait.onnx"] = "Light.M08.lsdkm",
                ["birefnet-dis.onnx"] = "Light.M09.lsdkm",
                ["birefnet-hrsod.onnx"] = "Light.M10.lsdkm",
                ["u2net.onnx"] = "Light.M11.lsdkm",
                ["u2netp.onnx"] = "Light.M12.lsdkm",
                ["u2net_human_seg.onnx"] = "Light.M13.lsdkm",
                ["u2net_cloth_seg.onnx"] = "Light.M14.lsdkm",
                ["u2net-portrait-matting.onnx"] = "Light.M15.lsdkm",
                ["silueta.onnx"] = "Light.M16.lsdkm",
                ["isnet-general-use.onnx"] = "Light.M17.lsdkm",
                ["isnet-anime.onnx"] = "Light.M18.lsdkm"
            };

        public InferenceService(IFaceDetectorService faceDetectorService)
        {
            _faceDetectorService = faceDetectorService;
        }

        /// <summary>
        /// Parses an image from either IFormFile upload or base64-encoded string.
        /// </summary>
        public async Task<Mat> ParseImageAsync(IFormFile? inputImage, string? inputImageBase64, ImreadModes mode = ImreadModes.Color)
        {
            byte[] imageBytes;
            
            if (!string.IsNullOrEmpty(inputImageBase64))
            {
                var base64Data = inputImageBase64.Contains(",") 
                    ? inputImageBase64.Split(',')[1] 
                    : inputImageBase64;
                imageBytes = Convert.FromBase64String(base64Data);
            }
            else if (inputImage != null)
            {
                using var ms = new MemoryStream();
                await inputImage.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }
            else
            {
                return new Mat();
            }

            return Cv2.ImDecode(imageBytes, mode);
        }

        /// <summary>
        /// Encodes a Mat image to base64 PNG format.
        /// </summary>
        public string EncodeToBase64(Mat img, int kb = 0)
        {
            Cv2.ImEncode(".png", img, out byte[] outputBytes);
            
            if (kb > 0)
            {
                outputBytes = CompressToKb(outputBytes, kb);
            }

            return "data:image/png;base64," + Convert.ToBase64String(outputBytes);
        }

        /// <summary>
        /// Performs human matting (background removal) on an image.
        /// </summary>
        public Mat RunHumanMatting(Mat srcImg, string mattingModel = "hivision_modnet")
        {
            using var processingImage = ResizeImageEsp(srcImg, 2000);
            using var bgr = processingImage.Channels() switch
            {
                4 => processingImage.CvtColor(ColorConversionCodes.BGRA2BGR),
                1 => processingImage.CvtColor(ColorConversionCodes.GRAY2BGR),
                _ => processingImage.Clone()
            };

            var modelPath = ResolveMattingModelPath(mattingModel);
            if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
            {
                throw new FileNotFoundException($"Matting ONNX model '{mattingModel}' was not found. Set MATTING_MODEL_DIR or place the model under 'models/matting models'.");
            }

            try
            {
                using var alpha = InferAlphaMask(bgr, mattingModel, modelPath);
                using var matted = MergeBgrWithAlpha(bgr, alpha);
                return mattingModel.Equals("hivision_modnet", StringComparison.OrdinalIgnoreCase)
                    ? HollowOutFix(matted)
                    : matted.Clone();
            }
            catch (Exception ex)
            {
                throw new Exception($"Matting ONNX model '{mattingModel}' failed during inference: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Adds a background color or gradient to an image.
        /// </summary>
        public Mat RunAddBackground(Mat srcImg, string colorHex, int renderMode = 0)
        {
            using var fg = EnsureBgra(srcImg);
            using var bg = CreateBackground(fg.Rows, fg.Cols, colorHex, renderMode);

            return AlphaBlendWithBackground(fg, bg);
        }

        /// <summary>
        /// Applies a text watermark to an image.
        /// </summary>
        public Mat RunWatermark(Mat srcImg, string text, int fontSize, double opacity, int angle, string colorHex, int space)
        {
            using var bgr = srcImg.Channels() switch
            {
                4 => srcImg.CvtColor(ColorConversionCodes.BGRA2BGR),
                1 => srcImg.CvtColor(ColorConversionCodes.GRAY2BGR),
                _ => srcImg.Clone()
            };

            using var layer = new Mat(bgr.Rows, bgr.Cols, MatType.CV_8UC3, new OpenCvSharp.Scalar(0, 0, 0));
            var color = HexToBgr(colorHex);

            double scale = Math.Max(0.3, fontSize / 32.0);
            int thickness = Math.Max(1, fontSize / 24);
            var font = HersheyFonts.HersheySimplex;
            var textSize = Cv2.GetTextSize(text, font, scale, thickness, out _);

            int stepX = Math.Max(40, textSize.Width + Math.Max(5, space));
            int stepY = Math.Max(30, textSize.Height + Math.Max(5, space));

            for (int y = 0; y < bgr.Rows + stepY; y += stepY)
            {
                for (int x = -textSize.Width; x < bgr.Cols + stepX; x += stepX)
                {
                    Cv2.PutText(layer, text, new Point(x, y), font, scale, color, thickness, LineTypes.AntiAlias);
                }
            }

            using var rotated = new Mat();
            var center = new Point2f(layer.Cols / 2f, layer.Rows / 2f);
            using var rotMat = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            Cv2.WarpAffine(layer, rotated, rotMat, layer.Size(), InterpolationFlags.Linear, BorderTypes.Constant, new OpenCvSharp.Scalar(0, 0, 0));

            var output = new Mat();
            Cv2.AddWeighted(bgr, 1.0, rotated, Math.Clamp(opacity, 0.0, 1.0), 0.0, output);
            return output;
        }

        /// <summary>
        /// Full ID photo generation with matting, alignment, and enhancements.
        /// </summary>
        public (Mat standardImg, Mat hdImg) RunIdPhoto(Mat srcImg, IdPhotoRequestOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return RunIdPhoto(
                srcImg,
                options.Height,
                options.Width,
                options.BackgroundColor,
                options.MattingModel,
                options.FaceDetectModel,
                options.HeadRatio,
                options.TopDistance,
                options.WhiteningStrength,
                options.BrightnessStrength,
                options.ContrastStrength,
                options.SaturationStrength,
                options.SharpenStrength,
                options.RenderMode,
                options.FaceAlign,
                options.HeadHeightRatio,
                options.TopDistanceMin);
        }

        /// <summary>
        /// Full ID photo generation with explicit parameter list.
        /// </summary>
        public (Mat standardImg, Mat hdImg) RunIdPhoto(
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
            double topDistanceMin = 0.10)
        {
            try
            {
                // Python IDCreator step 0: resize input before all downstream work.
                using Mat processingImage = ResizeImageEsp(srcImg, 2000);

                // Python step 1: human matting first, using the selected ONNX model.
                using Mat mattingResult = RunHumanMatting(processingImage, mattingModel);

                // Python step 2: beauty processes origin_image, then keeps the matting alpha unchanged.
                Mat currentOrigin = processingImage.Clone();
                Mat currentMatting = ApplyBeautyLikePython(
                    currentOrigin,
                    mattingResult,
                    whiteningStrength,
                    brightnessStrength,
                    contrastStrength,
                    saturationStrength,
                    sharpenStrength);

                try
                {
                    // Python step 3: detect face on the resized original image.
                    var faceInfo = _faceDetectorService.DetectFaceInfo(currentOrigin, faceDetectModel);

                    // Python step 3.1: optional face alignment, then run detection again.
                    if (faceAlign && Math.Abs(faceInfo.RollAngle) > 2.0)
                    {
                        var rotated = RotateBound4Channels(currentMatting, -1.0 * faceInfo.RollAngle);
                        currentOrigin.Dispose();
                        currentMatting.Dispose();
                        currentOrigin = rotated.originImage;
                        currentMatting = rotated.mattingImage;
                        faceInfo = _faceDetectorService.DetectFaceInfo(currentOrigin, faceDetectModel);
                    }

                    // Python step 4: crop/adjust using the matted image and detected face.
                    var cropResult = RunIdPhotoCrop(
                        currentMatting,
                        faceInfo.Rectangle,
                        height,
                        width,
                        headRatio,
                        topDistance,
                        headHeightRatio,
                        topDistanceMin);
                    using Mat croppedStd = cropResult.standardImg;
                    using Mat croppedHd = cropResult.hdImg;

                    // Final API step: compose requested background color.
                    using Mat finalStd = RunAddBackground(croppedStd, backgroundColor, renderMode);
                    using Mat finalHd = RunAddBackground(croppedHd, backgroundColor, renderMode);

                    return (finalStd.Clone(), finalHd.Clone());
                }
                finally
                {
                    currentOrigin.Dispose();
                    currentMatting.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ID photo generation failed: {ex.Message}", ex);
            }
        }

        public (Mat standardImg, Mat hdImg) RunIdPhoto(
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
            double topDistanceMin = 0.10)
        {
            return RunIdPhoto(
                srcImg,
                height,
                width,
                backgroundColor,
                mattingModel,
                "retinaface",
                headRatio,
                topDistance,
                whiteningStrength,
                brightnessStrength,
                contrastStrength,
                saturationStrength,
                sharpenStrength,
                renderMode,
                faceAlign,
                headHeightRatio,
                topDistanceMin);
        }

        /// <summary>
        /// Crops an image using precise mathematics from photo_adjuster.py.
        /// Implements: crop_measure = face_measure / headRatio,
        /// resize_ratio_single = sqrt(crop_measure / (width * height)),
        /// crop_size = (width * resize_ratio_single, height * resize_ratio_single),
        /// y1 = face_center.y - crop_size.y * topDistance
        /// </summary>
        public (Mat standardImg, Mat hdImg) RunIdPhotoCrop(
            Mat srcImg,
            int height,
            int width,
            double headRatio,
            double topDistance,
            double headHeightRatio = 0.45,
            double topDistanceMin = 0.10,
            bool faceAlign = false)
        {
            try
            {
                using var workingImage = EnsureBgra(srcImg);
                var faceInfo = _faceDetectorService.DetectFaceInfo(workingImage);

                if (faceAlign && Math.Abs(faceInfo.RollAngle) > 2.0)
                {
                    var rotated = RotateBound4Channels(workingImage, -1.0 * faceInfo.RollAngle);
                    rotated.originImage.Dispose();
                    try
                    {
                        faceInfo = _faceDetectorService.DetectFaceInfo(rotated.mattingImage);
                        return RunIdPhotoCrop(
                            rotated.mattingImage,
                            faceInfo.Rectangle,
                            height,
                            width,
                            headRatio,
                            topDistance,
                            headHeightRatio,
                            topDistanceMin
                        );
                    }
                    finally
                    {
                        rotated.mattingImage.Dispose();
                    }
                }

                return RunIdPhotoCrop(
                    workingImage,
                    faceInfo.Rectangle,
                    height,
                    width,
                    headRatio,
                    topDistance,
                    headHeightRatio,
                    topDistanceMin
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"ID photo crop failed: {ex.Message}", ex);
            }
        }

        private (Mat standardImg, Mat hdImg) RunIdPhotoCrop(
            Mat srcImg,
            Rect faceRect,
            int height,
            int width,
            double headRatio,
            double topDistance,
            double headHeightRatio = 0.45,
            double topDistanceMin = 0.10)
        {
            try
            {
                // Extract face rectangle components (x, y, w, h)
                double faceX = faceRect.X;
                double faceY = faceRect.Y;
                double faceW = faceRect.Width;
                double faceH = faceRect.Height;

                var topDistanceMax = Math.Clamp(topDistance, 0.01, 0.50);
                topDistanceMin = Math.Clamp(topDistanceMin, 0.0, topDistanceMax);

                // Step 2: Calculate face center and measure
                double faceCenter_X = faceX + faceW / 2.0;
                double faceCenter_Y = faceY + faceH / 2.0;
                double faceMeasure = faceW * faceH;

                // Step 3: Calculate crop measure = face_measure / headRatio
                // Note: In Python code, headRatio is called head_measure_ratio
                double cropMeasure = faceMeasure / headRatio;

                // Step 4: Calculate resize ratio
                // resize_ratio = crop_measure / (width * height)
                // resize_ratio_single = sqrt(resize_ratio)
                double resizeRatio = cropMeasure / (width * height);
                double resizeRatioSingle = Math.Sqrt(resizeRatio);

                // Step 5: Calculate crop size
                // crop_size = (width * resize_ratio_single, height * resize_ratio_single)
                int cropSize_W = (int)(width * resizeRatioSingle);
                int cropSize_H = (int)(height * resizeRatioSingle);

                if (cropSize_W <= 0 || cropSize_H <= 0)
                {
                    throw new Exception("Invalid crop size from detected face");
                }

                // Step 6: Calculate crop coordinates
                // Python first pass uses head_height_ratio for vertical anchor.
                int y1 = (int)(faceCenter_Y - cropSize_H * headHeightRatio);
                int x1 = (int)(faceCenter_X - cropSize_W / 2.0);
                int y2 = y1 + cropSize_H;
                int x2 = x1 + cropSize_W;

                // Step 7: First pass crop (IDphotos_cut + resize)
                using Mat cutImage = CropWithBoundaryHandling(srcImg, x1, y1, x2, y2, cropSize_W, cropSize_H);
                using Mat cutImageResized = new Mat();
                Cv2.Resize(cutImage, cutImageResized, new Size(cropSize_W, cropSize_H), interpolation: InterpolationFlags.Area);

                // Step 8: Person position check in first-pass crop (Python get_box + detect_distance)
                var (yTop, _, xLeft, xRight) = GetBoxDistances(cutImageResized, thresh: 127);

                var widthHeightRatio = (double)height / width;
                int statusLeftRight;
                int cutValueTop;
                if (xLeft > 0 || xRight > 0)
                {
                    statusLeftRight = 1;
                    cutValueTop = (int)(((xLeft + xRight) * widthHeightRatio) / 2.0);
                }
                else
                {
                    statusLeftRight = 0;
                    cutValueTop = 0;
                }

                var (statusTop, moveValue) = DetectDistance(
                    yTop - cutValueTop,
                    cropSize_H,
                    topDistanceMax,
                    topDistanceMin
                );

                Mat resultImage;
                if (statusLeftRight == 0 && statusTop == 0)
                {
                    resultImage = cutImageResized.Clone();
                }
                else
                {
                    resultImage = CropWithBoundaryHandling(
                        srcImg,
                        x1 + xLeft,
                        y1 + cutValueTop + statusTop * moveValue,
                        x2 - xRight,
                        y2 - cutValueTop + statusTop * moveValue,
                        (x2 - xRight) - (x1 + xLeft),
                        (y2 - cutValueTop + statusTop * moveValue) - (y1 + cutValueTop + statusTop * moveValue)
                    );
                }

                // Step 9: Move subject down when bottom has gap (Python move)
                using Mat moved = MoveToBottom(resultImage);
                resultImage.Dispose();

                // Step 10: Python resize outputs
                using Mat std = StandardPhotoResize(moved, height, width);
                using Mat hd = ResizeImageByMin(moved, Math.Max(600, width));

                return (std.Clone(), hd.Clone());
            }
            catch (Exception ex)
            {
                throw new Exception($"ID photo crop failed: {ex.Message}", ex);
            }
        }

        private static (int yTop, int yBottom, int xLeft, int xRight) GetBoxDistances(Mat image, int thresh = 127)
        {
            if (image.Channels() != 4)
            {
                throw new Exception("Input image must be 4-channel (BGRA)");
            }

            using var alpha = new Mat();
            Cv2.ExtractChannel(image, alpha, 3);

            using var mask = new Mat();
            Cv2.Threshold(alpha, mask, thresh, 255, ThresholdTypes.Binary);

            Cv2.FindContours(mask, out Point[][] contours, out _, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
            if (contours.Length == 0)
            {
                return (0, image.Rows, 0, image.Cols);
            }

            var maxContour = contours
                .OrderByDescending(c => Cv2.ContourArea(c))
                .First();

            var rect = Cv2.BoundingRect(maxContour);
            var height = image.Rows;
            var width = image.Cols;

            var yUp = Math.Max(0, rect.Y);
            var yDown = Math.Min(height - 1, rect.Y + rect.Height);
            var xL = Math.Max(0, rect.X);
            var xR = Math.Min(width - 1, rect.X + rect.Width);

            return (yUp, height - yDown, xL, width - xR);
        }

        private static (int status, int moveValue) DetectDistance(int value, int cropHeight, double max, double min)
        {
            if (cropHeight <= 0)
            {
                return (0, 0);
            }

            var ratio = (double)value / cropHeight;
            if (ratio >= min && ratio <= max)
            {
                return (0, 0);
            }

            if (ratio > max)
            {
                var moveValue = (int)((ratio - max) * cropHeight);
                return (1, moveValue);
            }

            var moveDown = (int)((min - ratio) * cropHeight);
            return (-1, moveDown);
        }

        private static Mat MoveToBottom(Mat inputImage)
        {
            var (_, yHigh, _, _) = GetBoxDistances(inputImage);
            if (yHigh <= 0 || yHigh >= inputImage.Rows)
            {
                return inputImage.Clone();
            }

            using var baseMat = new Mat(yHigh, inputImage.Cols, inputImage.Type(), OpenCvSharp.Scalar.All(0));
            using var topPart = new Mat(inputImage, new Rect(0, 0, inputImage.Cols, inputImage.Rows - yHigh));

            var merged = new Mat();
            Cv2.VConcat(new[] { baseMat, topPart }, merged);
            return merged;
        }

        private static Mat StandardPhotoResize(Mat inputImage, int outHeight, int outWidth)
        {
            var resizeRatio = inputImage.Rows / (double)outHeight;
            var resizeItem = (int)Math.Round(inputImage.Rows / (double)outHeight);

            if (resizeRatio < 2.0)
            {
                var result = new Mat();
                Cv2.Resize(inputImage, result, new Size(outWidth, outHeight), interpolation: InterpolationFlags.Area);
                return result;
            }

            Mat current = inputImage.Clone();
            try
            {
                for (int i = 0; i < resizeItem - 1; i++)
                {
                    int factor = resizeItem - i - 1;
                    using var next = new Mat();
                    Cv2.Resize(current, next, new Size(outWidth * factor, outHeight * factor), interpolation: InterpolationFlags.Area);
                    current.Dispose();
                    current = next.Clone();
                }

                return current;
            }
            catch
            {
                current.Dispose();
                throw;
            }
        }

        private static Mat ResizeImageByMin(Mat inputImage, int esp)
        {
            int h = inputImage.Rows;
            int w = inputImage.Cols;
            int minBorder = Math.Min(h, w);

            if (minBorder >= esp)
            {
                return inputImage.Clone();
            }

            int newH;
            int newW;
            if (h >= w)
            {
                newW = esp;
                newH = h * esp / w;
            }
            else
            {
                newH = esp;
                newW = w * esp / h;
            }

            var result = new Mat();
            Cv2.Resize(inputImage, result, new Size(newW, newH), interpolation: InterpolationFlags.Area);
            return result;
        }

        /// <summary>
        /// Crops an image with boundary handling (padding with transparent background for out-of-bounds areas).
        /// Implements the IDphotos_cut logic from photo_adjuster.py.
        /// </summary>
        private Mat CropWithBoundaryHandling(Mat srcImg, int x1, int y1, int x2, int y2, int cropWidth, int cropHeight)
        {
            // Ensure source is BGRA for transparency support
            Mat srcBgra = srcImg.Channels() == 4 ? srcImg : new Mat();
            if (srcImg.Channels() != 4)
            {
                Cv2.CvtColor(srcImg, srcBgra, ColorConversionCodes.BGR2BGRA);
            }

            // Create output with transparent background
            Mat output = new Mat(cropHeight, cropWidth, MatType.CV_8UC4, new OpenCvSharp.Scalar(255, 255, 255, 0));

            // Calculate actual crop boundaries
            int actualY1 = Math.Max(0, y1);
            int actualX1 = Math.Max(0, x1);
            int actualY2 = Math.Min(srcBgra.Rows, y2);
            int actualX2 = Math.Min(srcBgra.Cols, x2);

            // Calculate padding
            int padTop = actualY1 - y1;
            int padLeft = actualX1 - x1;

            // Copy valid region
            int copyHeight = actualY2 - actualY1;
            int copyWidth = actualX2 - actualX1;

            if (copyHeight > 0 && copyWidth > 0)
            {
                using Mat srcRegion = new Mat(srcBgra, new Rect(actualX1, actualY1, copyWidth, copyHeight));
                srcRegion.CopyTo(output[new Rect(padLeft, padTop, copyWidth, copyHeight)]);
            }

            if (srcImg.Channels() != 4)
            {
                srcBgra.Dispose();
            }

            return output;
        }

        /// <summary>
        /// Generates a tiled layout of photos for printing.
        /// </summary>
        public Mat RunGenerateLayout(Mat srcImg, int height, int width)
        {
            // 5x7 inch print canvas at 300 DPI.
            const int canvasW = 1500;
            const int canvasH = 2100;
            const int margin = 40;
            const int gap = 20;

            using var photo = srcImg.Channels() switch
            {
                4 => srcImg.CvtColor(ColorConversionCodes.BGRA2BGR),
                1 => srcImg.CvtColor(ColorConversionCodes.GRAY2BGR),
                _ => srcImg.Clone()
            };

            var canvas = new Mat(canvasH, canvasW, MatType.CV_8UC3, new OpenCvSharp.Scalar(255, 255, 255));
            using var tile = new Mat();
            Cv2.Resize(photo, tile, new Size(width, height), interpolation: InterpolationFlags.Area);

            int cols = Math.Max(1, (canvasW - margin * 2 + gap) / (width + gap));
            int rows = Math.Max(1, (canvasH - margin * 2 + gap) / (height + gap));

            int usedW = cols * width + (cols - 1) * gap;
            int usedH = rows * height + (rows - 1) * gap;
            int startX = Math.Max(margin, (canvasW - usedW) / 2);
            int startY = Math.Max(margin, (canvasH - usedH) / 2);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int x = startX + c * (width + gap);
                    int y = startY + r * (height + gap);
                    if (x + width <= canvasW && y + height <= canvasH)
                    {
                        tile.CopyTo(canvas[new Rect(x, y, width, height)]);
                        Cv2.Rectangle(canvas, new Rect(x, y, width, height), new OpenCvSharp.Scalar(180, 180, 180), 1);
                    }
                }
            }

            return canvas;
        }

        /// <summary>
        /// Processes a folder of images for batch background removal.
        /// </summary>
        public async Task<(int successCount, int failureCount)> ProcessFolderBackgroundRemovalAsync(
            string sourceFolder,
            string outputFolder,
            string mattingModel)
        {
            int successCount = 0;
            int failureCount = 0;

            try
            {
                Directory.CreateDirectory(outputFolder);

                var imageFiles = Directory.GetFiles(sourceFolder, "*.*")
                    .Where(f => new[] { ".jpg", ".jpeg", ".png", ".bmp" }.Contains(Path.GetExtension(f).ToLower()))
                    .ToList();

                foreach (var imageFile in imageFiles)
                {
                    try
                    {
                        using Mat srcImg = Cv2.ImRead(imageFile, ImreadModes.Color);
                        if (srcImg.Empty())
                        {
                            failureCount++;
                            continue;
                        }

                        using Mat output = RunHumanMatting(srcImg, mattingModel);
                        string outputPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(imageFile) + "_matted.png");
                        Cv2.ImWrite(outputPath, output);
                        successCount++;
                    }
                    catch
                    {
                        failureCount++;
                    }
                }
            }
            catch
            {
                failureCount++;
            }

            return (successCount, failureCount);
        }

        /// <summary>
        /// Compresses image bytes to a target kilobyte size.
        /// </summary>
        private byte[] CompressToKb(byte[] imageBytes, int targetKb)
        {
            if (imageBytes.Length <= targetKb * 1024)
                return imageBytes;

            using var src = Cv2.ImDecode(imageBytes, ImreadModes.Unchanged);
            if (src.Empty())
            {
                return imageBytes;
            }

            for (int level = 9; level >= 0; level--)
            {
                Cv2.ImEncode(".png", src, out byte[] pngBytes, new[] { (int)ImwriteFlags.PngCompression, level });
                if (pngBytes.Length <= targetKb * 1024)
                {
                    return pngBytes;
                }
            }

            using var resized = new Mat();
            double scale = 0.95;
            while (scale >= 0.60)
            {
                int w = Math.Max(1, (int)(src.Cols * scale));
                int h = Math.Max(1, (int)(src.Rows * scale));
                Cv2.Resize(src, resized, new Size(w, h), interpolation: InterpolationFlags.Area);
                Cv2.ImEncode(".png", resized, out byte[] resizedBytes, new[] { (int)ImwriteFlags.PngCompression, 9 });
                if (resizedBytes.Length <= targetKb * 1024)
                {
                    return resizedBytes;
                }
                scale -= 0.05;
            }

            return imageBytes;
        }

        private static Mat EnsureBgra(Mat src)
        {
            if (src.Channels() == 4)
            {
                return src.Clone();
            }

            var bgra = new Mat();
            if (src.Channels() == 3)
            {
                Cv2.CvtColor(src, bgra, ColorConversionCodes.BGR2BGRA);
            }
            else if (src.Channels() == 1)
            {
                Cv2.CvtColor(src, bgra, ColorConversionCodes.GRAY2BGRA);
            }
            else
            {
                throw new Exception("Unsupported image channels");
            }

            return bgra;
        }

        private static Mat ApplyBeautyLikePython(
            Mat originImage,
            Mat mattingImage,
            int whiteningStrength,
            int brightnessStrength,
            int contrastStrength,
            int saturationStrength,
            int sharpenStrength)
        {
            var processed = whiteningStrength > 0 ||
                            brightnessStrength != 0 ||
                            contrastStrength != 0 ||
                            saturationStrength != 0 ||
                            sharpenStrength != 0;

            if (!processed)
            {
                return mattingImage.Clone();
            }

            using var originBgr = originImage.Channels() switch
            {
                4 => originImage.CvtColor(ColorConversionCodes.BGRA2BGR),
                1 => originImage.CvtColor(ColorConversionCodes.GRAY2BGR),
                _ => originImage.Clone()
            };

            var pipeline = new IdPhotoPipelineBuilder()
                .ApplyWhitening(whiteningStrength)
                .ApplyBeauty(brightnessStrength, contrastStrength, saturationStrength, sharpenStrength);

            using var beautifiedBgr = pipeline.Execute(originBgr);
            using var mattingBgra = EnsureBgra(mattingImage);
            using var alpha = new Mat();
            Cv2.ExtractChannel(mattingBgra, alpha, 3);
            return MergeBgrWithAlpha(beautifiedBgr, alpha);
        }

        private static (Mat originImage, Mat mattingImage) RotateBound4Channels(Mat mattingImage, double angle)
        {
            using var bgra = EnsureBgra(mattingImage);
            var channels = bgra.Split();
            try
            {
                using var bgr = new Mat();
                Cv2.Merge(new[] { channels[0], channels[1], channels[2] }, bgr);

                Mat rotatedOrigin = RotateBound(bgr, angle);
                Mat rotatedAlpha = RotateBound(channels[3], angle);
                Mat rotatedMatting = MergeBgrWithAlpha(rotatedOrigin, rotatedAlpha);
                rotatedAlpha.Dispose();
                return (rotatedOrigin, rotatedMatting);
            }
            finally
            {
                foreach (var channel in channels)
                {
                    channel.Dispose();
                }
            }
        }

        private static Mat RotateBound(Mat image, double angle)
        {
            int h = image.Rows;
            int w = image.Cols;
            var center = new Point2f(w / 2f, h / 2f);
            using var rotationMatrix = Cv2.GetRotationMatrix2D(center, -angle, 1.0);

            double cos = Math.Abs(rotationMatrix.At<double>(0, 0));
            double sin = Math.Abs(rotationMatrix.At<double>(0, 1));
            int newW = (int)((h * sin) + (w * cos));
            int newH = (int)((h * cos) + (w * sin));

            rotationMatrix.Set(0, 2, rotationMatrix.At<double>(0, 2) + (newW / 2.0) - center.X);
            rotationMatrix.Set(1, 2, rotationMatrix.At<double>(1, 2) + (newH / 2.0) - center.Y);

            var rotated = new Mat();
            Cv2.WarpAffine(image, rotated, rotationMatrix, new Size(newW, newH));
            return rotated;
        }

        private static OpenCvSharp.Scalar HexToBgr(string hex)
        {
            const string defaultHex = "638CCE";

            var raw = (hex ?? string.Empty).Trim();
            if (TryNamedColorToBgr(raw, out var namedColor))
            {
                return namedColor;
            }

            // Accept csv/rgb-like input such as "99,140,206" or "rgb(99,140,206)".
            var rgbTokenChars = raw
                .Where(ch => char.IsDigit(ch) || ch == ',' || ch == ' ')
                .ToArray();
            var rgbTokens = new string(rgbTokenChars)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (rgbTokens.Length >= 3 &&
                byte.TryParse(rgbTokens[0], out var rFromRgb) &&
                byte.TryParse(rgbTokens[1], out var gFromRgb) &&
                byte.TryParse(rgbTokens[2], out var bFromRgb))
            {
                return new OpenCvSharp.Scalar(bFromRgb, gFromRgb, rFromRgb);
            }

            // Keep only valid hex chars so accidental prefixes/suffixes do not crash parsing.
            var hexOnly = new string(raw.Where(Uri.IsHexDigit).ToArray());
            if (hexOnly.Length == 3)
            {
                hexOnly = string.Concat(hexOnly.Select(ch => new string(ch, 2)));
            }
            else if (hexOnly.Length == 8)
            {
                // Handle AARRGGBB by dropping alpha.
                hexOnly = hexOnly.Substring(2, 6);
            }

            if (hexOnly.Length != 6)
            {
                hexOnly = defaultHex;
            }

            if (!byte.TryParse(hexOnly.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) ||
                !byte.TryParse(hexOnly.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) ||
                !byte.TryParse(hexOnly.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
            {
                // Final safe fallback to avoid throwing during request handling.
                r = 0x63;
                g = 0x8C;
                b = 0xCE;
            }

            return new OpenCvSharp.Scalar(b, g, r);
        }

        private static bool TryNamedColorToBgr(string raw, out OpenCvSharp.Scalar color)
        {
            switch ((raw ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "white":
                    color = new OpenCvSharp.Scalar(255, 255, 255);
                    return true;
                case "black":
                    color = new OpenCvSharp.Scalar(0, 0, 0);
                    return true;
                case "red":
                    color = new OpenCvSharp.Scalar(0, 0, 255);
                    return true;
                case "green":
                    color = new OpenCvSharp.Scalar(0, 255, 0);
                    return true;
                case "blue":
                    color = new OpenCvSharp.Scalar(255, 0, 0);
                    return true;
                default:
                    color = default;
                    return false;
            }
        }

        private static Mat ResizeImageEsp(Mat inputImage, int esp)
        {
            int height = inputImage.Rows;
            int width = inputImage.Cols;
            int maxBorder = Math.Max(height, width);

            if (maxBorder <= esp)
            {
                return inputImage.Clone();
            }

            int newHeight = height;
            int newWidth = width;
            if (height == maxBorder)
            {
                newWidth = (int)((esp / (double)height) * width);
                newHeight = esp;
            }
            else
            {
                newHeight = (int)((esp / (double)width) * height);
                newWidth = esp;
            }

            var resized = new Mat();
            Cv2.Resize(inputImage, resized, new Size(newWidth, newHeight), interpolation: InterpolationFlags.Area);
            return resized;
        }

        private static Mat MergeBgrWithAlpha(Mat bgr, Mat alpha)
        {
            var bgrChannels = bgr.Split();
            var rgba = new Mat();
            Cv2.Merge(new[] { bgrChannels[0], bgrChannels[1], bgrChannels[2], alpha }, rgba);
            foreach (var channel in bgrChannels)
            {
                channel.Dispose();
            }

            return rgba;
        }

        private static Mat AlphaBlendWithBackground(Mat fgBgra, Mat bgBgr)
        {
            var output = new Mat(fgBgra.Rows, fgBgra.Cols, MatType.CV_8UC3);

            for (int y = 0; y < fgBgra.Rows; y++)
            {
                for (int x = 0; x < fgBgra.Cols; x++)
                {
                    var fg = fgBgra.At<Vec4b>(y, x);
                    var bg = bgBgr.At<Vec3b>(y, x);
                    double alpha = fg.Item3 / 255.0;

                    output.Set(y, x, new Vec3b(
                        (byte)Math.Clamp((fg.Item0 - bg.Item0) * alpha + bg.Item0, 0.0, 255.0),
                        (byte)Math.Clamp((fg.Item1 - bg.Item1) * alpha + bg.Item1, 0.0, 255.0),
                        (byte)Math.Clamp((fg.Item2 - bg.Item2) * alpha + bg.Item2, 0.0, 255.0)));
                }
            }

            return output;
        }

        private static Mat InferAlphaMask(Mat bgr, string mattingModel, string modelPath)
        {
            var (inputW, inputH) = GetModelInputSize(modelPath, mattingModel);
            using var resized = new Mat();
            Cv2.Resize(bgr, resized, new Size(inputW, inputH), interpolation: GetMattingInputInterpolation(mattingModel));

            var inputTensor = BuildModelInputTensor(resized, mattingModel);
            var session = GetMattingSession(modelPath);
            using var results = session.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor(session.InputMetadata.Keys.First(), inputTensor)
            });

            if (mattingModel.Contains("modnet", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractModNetMaskAsMat(results, bgr.Cols, bgr.Rows);
            }

            if (mattingModel.Equals("rmbg-1.4", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractMinMaxMaskAsMat(results, bgr.Cols, bgr.Rows, InterpolationFlags.Linear);
            }

            if (mattingModel.StartsWith("birefnet", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractBirefNetMaskAsMat(results, bgr.Cols, bgr.Rows);
            }

            return ExtractMaskAsMat(results, bgr.Cols, bgr.Rows, mattingModel);
        }

        private static InterpolationFlags GetMattingInputInterpolation(string mattingModel)
        {
            if (mattingModel.Equals("rmbg-1.4", StringComparison.OrdinalIgnoreCase) ||
                mattingModel.StartsWith("birefnet", StringComparison.OrdinalIgnoreCase))
            {
                return InterpolationFlags.Linear;
            }

            return InterpolationFlags.Area;
        }

        private static DenseTensor<float> BuildModelInputTensor(Mat resizedBgr, string mattingModel)
        {
            int h = resizedBgr.Rows;
            int w = resizedBgr.Cols;
            var tensor = new DenseTensor<float>(new[] { 1, 3, h, w });

            bool isBirefnet = mattingModel.StartsWith("birefnet", StringComparison.OrdinalIgnoreCase);
            bool isRmbgLike = mattingModel.Equals("rmbg-1.4", StringComparison.OrdinalIgnoreCase) ||
                              mattingModel.StartsWith("bria-rmbg", StringComparison.OrdinalIgnoreCase);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var px = resizedBgr.At<Vec3b>(y, x);
                    float c0 = px.Item0 / 255f;
                    float c1 = px.Item1 / 255f;
                    float c2 = px.Item2 / 255f;

                    if (isBirefnet)
                    {
                        c0 = (c0 - 0.485f) / 0.229f;
                        c1 = (c1 - 0.456f) / 0.224f;
                        c2 = (c2 - 0.406f) / 0.225f;
                    }
                    else if (isRmbgLike || mattingModel.Contains("modnet", StringComparison.OrdinalIgnoreCase) || mattingModel.StartsWith("u2net", StringComparison.OrdinalIgnoreCase))
                    {
                        c0 = (c0 - 0.5f) / 0.5f;
                        c1 = (c1 - 0.5f) / 0.5f;
                        c2 = (c2 - 0.5f) / 0.5f;
                    }

                    tensor[0, 0, y, x] = c0;
                    tensor[0, 1, y, x] = c1;
                    tensor[0, 2, y, x] = c2;
                }
            }

            return tensor;
        }

        private static Mat ExtractModNetMaskAsMat(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results, int targetW, int targetH)
        {
            var tensorResult = results.Select(r => r.AsTensor<float>()).FirstOrDefault();
            if (tensorResult is null)
            {
                throw new Exception("MODNet model did not return a float tensor output.");
            }

            var (srcH, srcW, readAt) = CreateTensorReader(tensorResult);
            using var u8Mask = new Mat(srcH, srcW, MatType.CV_8UC1);

            for (int y = 0; y < srcH; y++)
            {
                for (int x = 0; x < srcW; x++)
                {
                    var value = Math.Clamp(readAt(y, x), 0f, 1f);
                    u8Mask.Set(y, x, (byte)(value * 255f));
                }
            }

            var resizedMask = new Mat();
            Cv2.Resize(u8Mask, resizedMask, new Size(targetW, targetH), interpolation: InterpolationFlags.Area);
            return resizedMask;
        }

        private static Mat ExtractMinMaxMaskAsMat(
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results,
            int targetW,
            int targetH,
            InterpolationFlags interpolation)
        {
            var tensorResult = results.Select(r => r.AsTensor<float>()).FirstOrDefault();
            if (tensorResult is null)
            {
                throw new Exception("Matting model did not return a float tensor output.");
            }

            var (srcH, srcW, readAt) = CreateTensorReader(tensorResult);
            var rawValues = new float[srcH * srcW];
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;

            for (int y = 0; y < srcH; y++)
            {
                for (int x = 0; x < srcW; x++)
                {
                    float value = readAt(y, x);
                    rawValues[y * srcW + x] = value;
                    minValue = Math.Min(minValue, value);
                    maxValue = Math.Max(maxValue, value);
                }
            }

            float range = Math.Max(1e-6f, maxValue - minValue);
            using var u8Mask = new Mat(srcH, srcW, MatType.CV_8UC1);
            for (int y = 0; y < srcH; y++)
            {
                for (int x = 0; x < srcW; x++)
                {
                    float normalized = (rawValues[y * srcW + x] - minValue) / range;
                    u8Mask.Set(y, x, (byte)(Math.Clamp(normalized, 0f, 1f) * 255f));
                }
            }

            var resizedMask = new Mat();
            Cv2.Resize(u8Mask, resizedMask, new Size(targetW, targetH), interpolation: interpolation);
            return resizedMask;
        }

        private static Mat ExtractBirefNetMaskAsMat(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results, int targetW, int targetH)
        {
            var tensorResult = results.Select(r => r.AsTensor<float>()).LastOrDefault();
            if (tensorResult is null)
            {
                throw new Exception("BiRefNet model did not return a float tensor output.");
            }

            var (srcH, srcW, readAt) = CreateTensorReader(tensorResult);
            using var u8Mask = new Mat(srcH, srcW, MatType.CV_8UC1);
            for (int y = 0; y < srcH; y++)
            {
                for (int x = 0; x < srcW; x++)
                {
                    float sigmoid = 1f / (1f + MathF.Exp(-readAt(y, x)));
                    u8Mask.Set(y, x, (byte)(Math.Clamp(sigmoid, 0f, 1f) * 255f));
                }
            }

            var resizedMask = new Mat();
            Cv2.Resize(u8Mask, resizedMask, new Size(targetW, targetH), interpolation: InterpolationFlags.Linear);
            return resizedMask;
        }

        private static (int h, int w, Func<int, int, float> readAt) CreateTensorReader(Tensor<float> tensorResult)
        {
            var dims = tensorResult.Dimensions.ToArray();
            if (dims.Length == 4)
            {
                return (dims[2], dims[3], (y, x) => tensorResult[0, 0, y, x]);
            }

            if (dims.Length == 3)
            {
                return (dims[1], dims[2], (y, x) => tensorResult[0, y, x]);
            }

            if (dims.Length == 2)
            {
                return (dims[0], dims[1], (y, x) => tensorResult[y, x]);
            }

            throw new Exception("Unsupported matting output tensor rank.");
        }

        private static Mat ExtractMaskAsMat(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results, int targetW, int targetH, string mattingModel)
        {
            var tensorResult = results
                .Select(r => r.AsTensor<float>())
                .FirstOrDefault();

            if (tensorResult is null)
            {
                throw new Exception("Matting model did not return a float tensor output.");
            }

            var (srcH, srcW, readAt) = CreateTensorReader(tensorResult);

            var rawMask = new Mat(srcH, srcW, MatType.CV_32FC1);
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;

            for (int y = 0; y < srcH; y++)
            {
                for (int x = 0; x < srcW; x++)
                {
                    float value = readAt(y, x);
                    if (mattingModel.StartsWith("birefnet", StringComparison.OrdinalIgnoreCase))
                    {
                        value = 1f / (1f + MathF.Exp(-value));
                    }

                    minValue = Math.Min(minValue, value);
                    maxValue = Math.Max(maxValue, value);
                    rawMask.Set(y, x, value);
                }
            }

            if (maxValue <= 1.001f && minValue >= -0.001f)
            {
                Cv2.Multiply(rawMask, 255.0, rawMask);
            }
            else
            {
                var range = Math.Max(1e-6f, maxValue - minValue);
                Cv2.Subtract(rawMask, minValue, rawMask);
                Cv2.Multiply(rawMask, 255.0 / range, rawMask);
            }

            var u8Mask = new Mat();
            rawMask.ConvertTo(u8Mask, MatType.CV_8UC1);
            rawMask.Dispose();

            var resizedMask = new Mat();
            Cv2.Resize(u8Mask, resizedMask, new Size(targetW, targetH), interpolation: InterpolationFlags.Area);
            u8Mask.Dispose();
            return resizedMask;
        }

        private static Mat HollowOutFix(Mat srcBgra)
        {
            if (srcBgra.Channels() != 4)
            {
                return srcBgra.Clone();
            }

            var channels = srcBgra.Split();
            try
            {
                using var srcBgr = new Mat();
                Cv2.Merge(new[] { channels[0], channels[1], channels[2] }, srcBgr);

                using var alphaPaddedVertical = new Mat();
                using var verticalPad = new Mat(10, channels[3].Cols, MatType.CV_8UC1, OpenCvSharp.Scalar.All(0));
                Cv2.VConcat(new[] { verticalPad, channels[3], verticalPad }, alphaPaddedVertical);

                using var alphaPadded = new Mat();
                using var horizontalPad = new Mat(alphaPaddedVertical.Rows, 10, MatType.CV_8UC1, OpenCvSharp.Scalar.All(0));
                Cv2.HConcat(new[] { horizontalPad, alphaPaddedVertical, horizontalPad }, alphaPadded);

                using var threshold = new Mat();
                Cv2.Threshold(alphaPadded, threshold, 127, 255, ThresholdTypes.Binary);

                using var eroded = new Mat();
                using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
                Cv2.Erode(threshold, eroded, kernel, iterations: 3);

                Cv2.FindContours(eroded, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                if (contours.Length == 0)
                {
                    return srcBgra.Clone();
                }

                var largest = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                using var contourMask = new Mat(alphaPadded.Size(), MatType.CV_8UC1, OpenCvSharp.Scalar.All(0));
                Cv2.DrawContours(contourMask, new Point[][] { largest }, -1, OpenCvSharp.Scalar.All(255), 2);

                using var floodMask = new Mat(contourMask.Rows + 2, contourMask.Cols + 2, MatType.CV_8UC1, OpenCvSharp.Scalar.All(0));
                Cv2.FloodFill(contourMask, floodMask, new Point(0, 0), OpenCvSharp.Scalar.All(255));

                using var invertedContour = new Mat();
                using var fullMask = new Mat(contourMask.Size(), MatType.CV_8UC1, OpenCvSharp.Scalar.All(255));
                Cv2.Subtract(fullMask, contourMask, invertedContour);

                using var fixedAlphaPadded = new Mat();
                Cv2.Add(alphaPadded, invertedContour, fixedAlphaPadded);

                using var fixedAlpha = new Mat(fixedAlphaPadded, new Rect(10, 10, srcBgra.Cols, srcBgra.Rows));
                return MergeBgrWithAlpha(srcBgr, fixedAlpha);
            }
            finally
            {
                foreach (var channel in channels)
                {
                    channel.Dispose();
                }
            }
        }

        private static InferenceSession GetMattingSession(string modelPath)
        {
            lock (SessionLock)
            {
                if (MattingSessions.TryGetValue(modelPath, out var cached))
                {
                    return cached;
                }

                var options = new Microsoft.ML.OnnxRuntime.SessionOptions();
                var session = new InferenceSession(modelPath, options);
                MattingSessions[modelPath] = session;
                return session;
            }
        }

        private static (int w, int h) GetModelInputSize(string modelPath, string mattingModel)
        {
            var session = GetMattingSession(modelPath);
            var firstInput = session.InputMetadata.Values.FirstOrDefault();
            if (firstInput is not null)
            {
                var dims = firstInput.Dimensions.ToArray();
                if (dims.Length == 4)
                {
                    int w = dims[3] > 0 ? dims[3] : 0;
                    int h = dims[2] > 0 ? dims[2] : 0;
                    if (w > 0 && h > 0)
                    {
                        return (w, h);
                    }
                }
            }

            if (mattingModel.Equals("rmbg-1.4", StringComparison.OrdinalIgnoreCase) ||
                mattingModel.StartsWith("bria-rmbg", StringComparison.OrdinalIgnoreCase) ||
                mattingModel.StartsWith("birefnet", StringComparison.OrdinalIgnoreCase))
            {
                return (1024, 1024);
            }

            return (512, 512);
        }

        private static string? ResolveMattingModelPath(string modelName)
        {
            var normalized = string.IsNullOrWhiteSpace(modelName) ? "hivision_modnet" : modelName.Trim();

            string root = Environment.GetEnvironmentVariable("MATTING_MODEL_DIR") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(root))
            {
                var baseDir = AppContext.BaseDirectory;
                var candidates = new List<string>();

                var configuredRoot = Environment.GetEnvironmentVariable("LIGHT_SDK_MODELS_ROOT");
                if (!string.IsNullOrWhiteSpace(configuredRoot))
                {
                    candidates.Add(Path.Combine(configuredRoot, "matting models"));
                }

                candidates.Add(Path.Combine(baseDir, "models", "matting models"));

                var cursor = new DirectoryInfo(baseDir);
                while (cursor is not null)
                {
                    candidates.Add(Path.Combine(cursor.FullName, "models", "matting models"));
                    cursor = cursor.Parent;
                }

                root = candidates
                    .Select(Path.GetFullPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(Directory.Exists)
                    ?? string.Empty;
            }

            if (Path.IsPathRooted(normalized) && File.Exists(normalized))
            {
                return normalized;
            }

            if (normalized.EndsWith(".lsdkm", StringComparison.OrdinalIgnoreCase))
            {
                var directProtectedPath = Path.Combine(root, normalized);
                if (File.Exists(directProtectedPath))
                {
                    return directProtectedPath;
                }
            }

            if (normalized.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
            {
                if (MattingModelAliasMap.TryGetValue(normalized, out var aliasFromOnnx))
                {
                    var protectedFromOnnx = Path.Combine(root, aliasFromOnnx);
                    if (File.Exists(protectedFromOnnx))
                    {
                        return protectedFromOnnx;
                    }
                }

                var directPath = Path.Combine(root, normalized);
                if (File.Exists(directPath))
                {
                    return directPath;
                }
            }

            if (MattingModelFileMap.TryGetValue(normalized, out var filename))
            {
                if (MattingModelAliasMap.TryGetValue(filename, out var aliasName))
                {
                    var mappedProtectedPath = Path.Combine(root, aliasName);
                    if (File.Exists(mappedProtectedPath))
                    {
                        return mappedProtectedPath;
                    }
                }

                var mappedPath = Path.Combine(root, filename);
                if (File.Exists(mappedPath))
                {
                    return mappedPath;
                }
            }

            var fallback = Path.Combine(root, normalized + ".onnx");
            return File.Exists(fallback) ? fallback : null;
        }

        private static Mat CreateBackground(int h, int w, string colorHex, int renderMode)
        {
            var color = HexToBgr(colorHex);
            var bg = new Mat(h, w, MatType.CV_8UC3, color);

            if (renderMode == 1)
            {
                for (int y = 0; y < h; y++)
                {
                    double t = h <= 0 ? 0 : y / (double)h;
                    var rowColor = new OpenCvSharp.Scalar(
                        (byte)Math.Clamp(t * 255.0 + (1.0 - t) * color.Val0, 0.0, 255.0),
                        (byte)Math.Clamp(t * 255.0 + (1.0 - t) * color.Val1, 0.0, 255.0),
                        (byte)Math.Clamp(t * 255.0 + (1.0 - t) * color.Val2, 0.0, 255.0));
                    Cv2.Line(bg, new Point(0, y), new Point(w - 1, y), rowColor, 1);
                }
            }
            else if (renderMode == 2)
            {
                int endAxes = Math.Max(h, w);
                var center = new Point(w / 2, h / 2);
                bg.SetTo(OpenCvSharp.Scalar.All(0));

                for (int y = 0; y < endAxes; y++)
                {
                    double t = endAxes <= 0 ? 0 : y / (double)endAxes;
                    var ellipseColor = new OpenCvSharp.Scalar(
                        (byte)Math.Clamp(t * 255.0 + (1.0 - t) * color.Val0, 0.0, 255.0),
                        (byte)Math.Clamp(t * 255.0 + (1.0 - t) * color.Val1, 0.0, 255.0),
                        (byte)Math.Clamp(t * 255.0 + (1.0 - t) * color.Val2, 0.0, 255.0));
                    Cv2.Ellipse(bg, center, new Size(endAxes - y, endAxes - y), 0, 0, 360, ellipseColor, -1);
                }
            }

            return bg;
        }
    }
}
