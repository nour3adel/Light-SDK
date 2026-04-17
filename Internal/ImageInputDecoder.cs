using System;
using OpenCvSharp;

namespace Light.SDK.Internal;

internal static class ImageInputDecoder
{
    public static Mat DecodeBytes(byte[] imageBytes)
    {
        if (imageBytes is null || imageBytes.Length == 0)
        {
            throw new ArgumentException("Image bytes are empty.", nameof(imageBytes));
        }

        // Use unchanged mode to preserve alpha where available.
        var source = Cv2.ImDecode(imageBytes, ImreadModes.Unchanged);
        if (source.Empty())
        {
            source.Dispose();
            throw new InvalidOperationException("Unable to decode input image bytes.");
        }

        return source;
    }

    public static byte[] DecodeBase64ToBytes(string base64Image)
    {
        if (string.IsNullOrWhiteSpace(base64Image))
        {
            throw new ArgumentException("Base64 image cannot be empty.", nameof(base64Image));
        }

        var payload = base64Image.Contains(',')
            ? base64Image[(base64Image.IndexOf(',') + 1)..]
            : base64Image;

        return Convert.FromBase64String(payload);
    }
}
