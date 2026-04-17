using HivisionIDPhotos.Core.Models.Sdk;

namespace Light.SDK.Internal;

internal static class IdCreatorRequestMapper
{
    public static IdPhotoGenerationOptions ToGenerationOptions(IdCreatorRequest request)
    {
        var safeRequest = request ?? new IdCreatorRequest();

        // Centralizing mapping here keeps the public facade small and easier to test.
        return new IdPhotoGenerationOptions
        {
            TargetSize = ResolveSize(safeRequest),
            Background = safeRequest.Background,
            FaceLayout = safeRequest.FaceLayout,
            Models = safeRequest.Models,
            Beauty = safeRequest.Beauty,
            Watermark = safeRequest.Watermark,
            Template = safeRequest.Template,
            GenerateLayoutSheet = safeRequest.GenerateLayoutSheet,
            Layout = safeRequest.Layout
        };
    }

    private static IdPhotoPixelSize ResolveSize(IdCreatorRequest request)
    {
        if (request.Width is > 0 && request.Height is > 0)
        {
            return new IdPhotoPixelSize(request.Width.Value, request.Height.Value, "Custom");
        }

        if (IdPhotoSizeCatalog.TryGet(request.SizePreset, out var preset))
        {
            return preset;
        }

        return IdPhotoSizeCatalog.OneInch;
    }
}
