using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Timers;
using FileChecksum.Core.Services;

namespace FileChecksum
{
    /// <summary>
    /// Windows service for monitoring file checksums and exposing Prometheus metrics.
    /// Focuses on service lifecycle (start, stop) with business logic delegated to injected services.
    /// </summary>
    public partial class FileChecksumService : ServiceBase
    {
        private const string ServiceDisplayName = "FileChecksumService";
        private const string LogSource = "FileChecksumService";

        private ServiceContext _serviceContext;
        private Timer _checkTimer;
        private MetricsHttpServer _metricsServer;

        public FileChecksumService()
        {
            ServiceName = ServiceDisplayName;
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            WriteEventLog("Service starting...");

            try
            {
                // 1. Resolve configuration path
                string configPath = ResolveConfigPath();
                WriteEventLog($"Configuration path: {configPath}");

                // 2. Bootstrap and build service context
                var bootstrapper = new ServiceBootstrapper(configPath);
                _serviceContext = bootstrapper.BuildServiceContext();

                // 3. Load and validate configuration
                var loadResult = _serviceContext.ConfigurationService.LoadConfigurationAsync().Result;

                if (!loadResult.Success)
                {
                    WriteEventLog($"Failed to load configuration:\n{loadResult.GetDetailedLog()}", EventLogEntryType.Error);
                    throw new InvalidOperationException("Configuration loading failed. See event log for details.");
                }

                WriteEventLog($"Configuration loaded successfully from: {loadResult.SourceUsed}");
                var config = loadResult.LoadedConfiguration;

                // 4. Apply PCType filtering if this machine has a PCType
                if (!string.IsNullOrEmpty(config.PCType))
                {
                    config = _serviceContext.ConfigurationService.FilterByPCType(config, config.PCType);
                    WriteEventLog($"Applied PCType filter: {config.PCType}. Monitoring {config.Files.Count} files.");
                }
                else
                {
                    WriteEventLog($"No PCType filtering. Monitoring {config.Files.Count} files.");
                }

                if (config.Files.Count == 0)
                {
                    throw new InvalidOperationException("Configuration contains no files to monitor after filtering.");
                }

                // 5. Start HTTP metrics server
                _metricsServer = new MetricsHttpServer(config.Port, _serviceContext.MetricsCollector);
                _metricsServer.Start();
                WriteEventLog($"Prometheus metrics server started on port {config.Port}");

                // 6. Set up periodic checksum verification
                _checkTimer = new Timer(config.CheckIntervalMs);
                _checkTimer.Elapsed += (s, e) => PerformChecksumVerification(config);
                _checkTimer.AutoReset = true;
                _checkTimer.Start();

                WriteEventLog($"Service started successfully. Check interval: {config.CheckIntervalMs}ms");
            }
            catch (Exception ex)
            {
                WriteEventLog($"Error during service startup: {ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error);
                throw;
            }
        }

        protected override void OnStop()
        {
            WriteEventLog("Service stopping...");

            try
            {
                // Stop periodic checks
                if (_checkTimer != null)
                {
                    _checkTimer.Stop();
                    _checkTimer.Dispose();
                    _checkTimer = null;
                }

                // Stop HTTP server
                if (_metricsServer != null)
                {
                    _metricsServer.Stop();
                    _metricsServer.Dispose();
                    _metricsServer = null;
                }

                WriteEventLog("Service stopped successfully.");
            }
            catch (Exception ex)
            {
                WriteEventLog($"Error during service shutdown: {ex.Message}", EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Performs one cycle of checksum verification.
        /// </summary>
        private void PerformChecksumVerification(Core.Models.FileChecksumsConfig config)
        {
            try
            {
                // Perform verification on all configured files
                _serviceContext.FileMonitorService.VerifyFileChecksums(config.Files);

                // Record results in metrics
                _serviceContext.MetricsCollector.RecordCheckResults(config.Files);
            }
            catch (Exception ex)
            {
                WriteEventLog($"Error during checksum verification: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private string ResolveConfigPath()
        {
            // Priority order:
            // 1. App.config setting
            // 2. Local config.xml in service directory
            // 3. Config in C:\ProgramData\FileChecksum\

            string appConfigPath = System.Configuration.ConfigurationManager.AppSettings["ConfigFilePath"];
            if (!string.IsNullOrEmpty(appConfigPath) && File.Exists(appConfigPath))
            {
                return appConfigPath;
            }

            string localPath = Path.Combine(AppContext.BaseDirectory, "config.xml");
            if (File.Exists(localPath))
            {
                return localPath;
            }

            string programDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "FileChecksum",
                "config.xml");

            return programDataPath;
        }

        private void WriteEventLog(string message, EventLogEntryType entryType = EventLogEntryType.Information)
        {
            try
            {
                EventLog.WriteEntry(LogSource, message, entryType);
            }
            catch
            {
                // If event logging fails, continue anyway
            }
        }
    }
}