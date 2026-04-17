using HivisionIDPhotos.Core.Models.Sdk;

namespace Light.SDK;

public sealed class IdCreatorRequest
{
    public string SizePreset { get; set; } = "one_inch";
    public int? Width { get; set; }
    public int? Height { get; set; }

    public BackgroundOptions Background { get; set; } = new();
    public FaceLayoutOptions FaceLayout { get; set; } = new();
    public ModelOptions Models { get; set; } = new();
    public BeautyOptions Beauty { get; set; } = new();
    public WatermarkOptions Watermark { get; set; } = new();
    public TemplateOptions Template { get; set; } = new();

    public bool GenerateLayoutSheet { get; set; }
    public LayoutOptions Layout { get; set; } = new();

    public ExportOptions StandardExport { get; set; } = new()
    {
        Format = OutputImageFormat.Jpeg,
        Dpi = 300
    };

    public ExportOptions HdExport { get; set; } = new()
    {
        Format = OutputImageFormat.Jpeg,
        Dpi = 300
    };

    public ExportOptions LayoutExport { get; set; } = new()
    {
        Format = OutputImageFormat.Jpeg,
        Dpi = 300
    };
}
