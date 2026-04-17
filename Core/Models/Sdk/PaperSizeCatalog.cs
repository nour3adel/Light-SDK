using System.Collections.Generic;

namespace HivisionIDPhotos.Core.Models.Sdk;

public static class PaperSizeCatalog
{
    public static readonly PaperSpec FiveInch = new(LayoutPaperKind.FiveInch, 1499, 1050, 300, "5 inch");
    public static readonly PaperSpec SixInch = new(LayoutPaperKind.SixInch, 1800, 1200, 300, "6 inch");
    public static readonly PaperSpec A4 = new(LayoutPaperKind.A4, 2480, 3508, 300, "A4");
    public static readonly PaperSpec ThreeR = new(LayoutPaperKind.ThreeR, 1500, 1051, 300, "3R");
    public static readonly PaperSpec FourR = new(LayoutPaperKind.FourR, 1800, 1200, 300, "4R");

    private static readonly IReadOnlyDictionary<LayoutPaperKind, PaperSpec> PaperMap =
        new Dictionary<LayoutPaperKind, PaperSpec>
        {
            [LayoutPaperKind.FiveInch] = FiveInch,
            [LayoutPaperKind.SixInch] = SixInch,
            [LayoutPaperKind.A4] = A4,
            [LayoutPaperKind.ThreeR] = ThreeR,
            [LayoutPaperKind.FourR] = FourR
        };

    public static PaperSpec Resolve(LayoutOptions options)
    {
        if (options.PaperKind == LayoutPaperKind.Custom)
        {
            var width = options.CustomWidthPx <= 0 ? 1499 : options.CustomWidthPx;
            var height = options.CustomHeightPx <= 0 ? 1050 : options.CustomHeightPx;
            return new PaperSpec(LayoutPaperKind.Custom, width, height, options.Dpi, "Custom");
        }

        return PaperMap[options.PaperKind];
    }
}
