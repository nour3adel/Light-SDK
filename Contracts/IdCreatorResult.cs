using System;
using System.IO;

namespace Light.SDK;

public sealed class IdCreatorResult
{
    public required byte[] StandardImageBytes { get; init; }
    public required byte[] HdImageBytes { get; init; }
    public byte[]? LayoutImageBytes { get; init; }
    public byte[]? TemplateImageBytes { get; init; }

    public string StandardExtension { get; init; } = ".jpg";
    public string HdExtension { get; init; } = ".jpg";
    public string LayoutExtension { get; init; } = ".jpg";
    public string TemplateExtension { get; init; } = ".jpg";

    public required string StandardImageBase64 { get; init; }
    public required string HdImageBase64 { get; init; }
    public string? LayoutImageBase64 { get; init; }
    public string? TemplateImageBase64 { get; init; }

    public void SaveToDirectory(string outputDirectory, string baseFileName = "idphoto")
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory cannot be empty.", nameof(outputDirectory));
        }

        Directory.CreateDirectory(outputDirectory);

        File.WriteAllBytes(Path.Combine(outputDirectory, $"{baseFileName}_standard{StandardExtension}"), StandardImageBytes);
        File.WriteAllBytes(Path.Combine(outputDirectory, $"{baseFileName}_hd{HdExtension}"), HdImageBytes);

        if (LayoutImageBytes is not null)
        {
            File.WriteAllBytes(Path.Combine(outputDirectory, $"{baseFileName}_layout{LayoutExtension}"), LayoutImageBytes);
        }

        if (TemplateImageBytes is not null)
        {
            File.WriteAllBytes(Path.Combine(outputDirectory, $"{baseFileName}_template{TemplateExtension}"), TemplateImageBytes);
        }
    }
}
