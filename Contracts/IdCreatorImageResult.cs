namespace Light.SDK;

public sealed class IdCreatorImageResult
{
    public required byte[] ImageBytes { get; init; }
    public required string ImageBase64 { get; init; }
    public string Extension { get; init; } = ".jpg";
}
