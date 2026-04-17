namespace Light.SDK;

/// <summary>
/// Fluent extension helpers for configuring and executing Light.SDK requests.
/// </summary>
public static class IdCreatorFluentExtensions
{
    /// <summary>
    /// Opens an existing request for fluent editing.
    /// </summary>
    /// <param name="request">Existing request instance.</param>
    public static IdCreatorRequestBuilder ConfigureRequest(this IdCreatorRequest request)
    {
        return new IdCreatorRequestBuilder(request);
    }

    /// <summary>
    /// Creates an ID photo from image bytes using fluent configuration.
    /// </summary>
    /// <param name="creator">SDK creator instance.</param>
    /// <param name="imageBytes">Source image bytes.</param>
    /// <param name="configure">Builder configuration delegate.</param>
    public static IdCreatorResult Create(this IdCreator creator, byte[] imageBytes, System.Action<IdCreatorRequestBuilder> configure)
    {
        var builder = new IdCreatorRequestBuilder();
        configure(builder);
        return creator.Create(imageBytes, builder.Build());
    }

    /// <summary>
    /// Creates an ID photo from an image file path using fluent configuration.
    /// </summary>
    /// <param name="creator">SDK creator instance.</param>
    /// <param name="imagePath">Source image path.</param>
    /// <param name="configure">Builder configuration delegate.</param>
    public static IdCreatorResult CreateFromFile(this IdCreator creator, string imagePath, System.Action<IdCreatorRequestBuilder> configure)
    {
        var builder = new IdCreatorRequestBuilder();
        configure(builder);
        return creator.CreateFromFile(imagePath, builder.Build());
    }

    /// <summary>
    /// Creates an ID photo from a base64 image string using fluent configuration.
    /// </summary>
    /// <param name="creator">SDK creator instance.</param>
    /// <param name="base64Image">Source image in base64 format.</param>
    /// <param name="configure">Builder configuration delegate.</param>
    public static IdCreatorResult CreateFromBase64(this IdCreator creator, string base64Image, System.Action<IdCreatorRequestBuilder> configure)
    {
        var builder = new IdCreatorRequestBuilder();
        configure(builder);
        return creator.CreateFromBase64(base64Image, builder.Build());
    }
}
