using HivisionIDPhotos.Core.Models.Sdk;

namespace Light.SDK.Internal;

internal static class OutputFormatHelper
{
    public static string GetExtension(OutputImageFormat format)
    {
        return format == OutputImageFormat.Png ? ".png" : ".jpg";
    }
}
