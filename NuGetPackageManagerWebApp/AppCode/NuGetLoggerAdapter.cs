using NuGet.Common;
using Microsoft.Extensions.Logging;

namespace NuGetPackageManagerWebApp.AppCode
{
    public class NuGetLoggerAdapter : NuGet.Common.ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public NuGetLoggerAdapter(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
        }

        public void LogDebug(string data) => _logger.LogDebug(data);
        public void LogVerbose(string data) => _logger.LogTrace(data);
        public void LogInformation(string data) => _logger.LogInformation(data);
        public void LogMinimal(string data) => _logger.LogInformation(data);
        public void LogWarning(string data) => _logger.LogWarning(data);
        public void LogError(string data) => _logger.LogError(data);
        public void LogInformationSummary(string data) => _logger.LogInformation(data);
        public void Log(NuGet.Common.LogLevel level, string data)
        {
            switch (level)
            {
                case NuGet.Common.LogLevel.Debug:
                    LogDebug(data);
                    break;
                case NuGet.Common.LogLevel.Verbose:
                    LogVerbose(data);
                    break;
                case NuGet.Common.LogLevel.Information:
                    LogInformation(data);
                    break;
                case NuGet.Common.LogLevel.Minimal:
                    LogMinimal(data);
                    break;
                case NuGet.Common.LogLevel.Warning:
                    LogWarning(data);
                    break;
                case NuGet.Common.LogLevel.Error:
                    LogError(data);
                    break;
                default:
                    _logger.LogInformation(data);
                    break;
            }
        }

        public Task LogAsync(NuGet.Common.LogLevel level, string data)
        {
            Log(level, data);
            return Task.CompletedTask;
        }

        public void Log(ILogMessage message) => Log(message.Level, message.Message);
        public Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }
    }
}