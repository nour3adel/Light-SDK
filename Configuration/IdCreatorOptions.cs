namespace Light.SDK;

public enum SdkLogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

public sealed class IdCreatorOptions
{
    // Optional custom model root if your host app stores models elsewhere.
    public string? ModelsRootPath { get; set; }

    // Optional explicit RetinaFace model file path. Overrides ModelsRootPath detector default when provided.
    public string? RetinaFaceModelPath { get; set; }

    // Optional explicit matting models directory. Overrides ModelsRootPath matting default when provided.
    public string? MattingModelsDirectory { get; set; }

    // Enables production file logging for SDK operations.
    public bool EnableFileLogging { get; set; } = true;

    // Optional custom logs directory. Defaults to <AppBase>/logs.
    public string? LogsDirectoryPath { get; set; }

    // Log file name when file logging is enabled.
    public string LogFileName { get; set; } = "light-sdk.log";

    // Minimum level emitted to file logs.
    public SdkLogLevel MinimumLogLevel { get; set; } = SdkLogLevel.Info;
}
