namespace HivisionIDPhotos.Core.Models
{
    public sealed class IdPhotoRequestOptions
    {
        public int Height { get; set; } = 413;
        public int Width { get; set; } = 295;
        public string BackgroundColor { get; set; } = "638cce";
        public string MattingModel { get; set; } = "modnet_photographic_portrait_matting";
        public string FaceDetectModel { get; set; } = "retinaface";
        public double HeadRatio { get; set; } = 0.2;
        public double TopDistance { get; set; } = 0.12;
        public int WhiteningStrength { get; set; }
        public int BrightnessStrength { get; set; }
        public int ContrastStrength { get; set; }
        public int SaturationStrength { get; set; }
        public int SharpenStrength { get; set; }
        public int RenderMode { get; set; }
        public bool FaceAlign { get; set; }
        public double HeadHeightRatio { get; set; } = 0.45;
        public double TopDistanceMin { get; set; } = 0.10;
    }
}
