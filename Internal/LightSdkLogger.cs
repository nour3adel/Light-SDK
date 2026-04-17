using System;
using System.IO;

namespace Light.SDK.Internal;

internal interface ILightSdkLogger
{
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
}

internal static class LightSdkLoggerFactory
{
    public static ILightSdkLogger Create(IdCreatorOptions options)
    {
        if (!options.EnableFileLogging)
        {
            return new NullLightSdkLogger();
        }

        var logsDirectory = ResolveLogsDirectory(options.LogsDirectoryPath);
        var fileName = string.IsNullOrWhiteSpace(options.LogFileName) ? "light-sdk.log" : options.LogFileName.Trim();
        var filePath = Path.Combine(logsDirectory, fileName);

        return new FileLightSdkLogger(filePath, options.MinimumLogLevel);
    }

    private static string ResolveLogsDirectory(string? configuredDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return configuredDirectory.Trim();
        }

        var baseDir = AppContext.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = Environment.CurrentDirectory;
        }

        return Path.Combine(baseDir, "logs");
    }
}

internal sealed class NullLightSdkLogger : ILightSdkLogger
{
    public void Debug(string message) { }
    public void Info(string message) { }
    public void Warn(string message) { }
    public void Error(string message, Exception? exception = null) { }
}

internal sealed class FileLightSdkLogger : ILightSdkLogger
{
    private readonly string _filePath;
    private readonly SdkLogLevel _minimumLevel;
    private readonly object _gate = new();

    public FileLightSdkLogger(string filePath, SdkLogLevel minimumLevel)
    {
        _filePath = filePath;
        _minimumLevel = minimumLevel;
    }

    public void Debug(string message) => Write(SdkLogLevel.Debug, message, null);
    public void Info(string message) => Write(SdkLogLevel.Info, message, null);
    public void Warn(string message) => Write(SdkLogLevel.Warning, message, null);
    public void Error(string message, Exception? exception = null) => Write(SdkLogLevel.Error, message, exception);

    private void Write(SdkLogLevel level, string message, Exception? exception)
    {
        if (level < _minimumLevel)
        {
            return;
        }

        var line = $"[{DateTimeOffset.UtcNow:O}] [{level}] {message}";
        if (exception is not null)
        {
            line += $"{Environment.NewLine}{exception}";
        }

        lock (_gate)
        {
            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                RotateIfNeeded();
                File.AppendAllText(_filePath, line + Environment.NewLine);
            }
            catch
            {
                // Logging is best-effort and should not break SDK execution.
            }
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        var fileInfo = new FileInfo(_filePath);
        const long maxBytes = 5 * 1024 * 1024;
        if (fileInfo.Length < maxBytes)
        {
            return;
        }

        var archivePath = _filePath + ".1";
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        File.Move(_filePath, archivePath);
    }
}
