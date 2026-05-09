using System;
using System.IO;
using FileChecksum.Core.Abstractions;
using FileChecksum.Core.Services;

namespace FileChecksum
{
    /// <summary>
    /// Bootstrapper for dependency setup and initialization.
    /// Handles creation of service components with proper configuration.
    /// </summary>
    public class ServiceBootstrapper
    {
        private readonly string _configPath;

        public ServiceBootstrapper(string configPath)
        {
            _configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
        }

        /// <summary>
        /// Builds a fully configured service context with all dependencies.
        /// </summary>
        public ServiceContext BuildServiceContext()
        {
            // 1. Create configuration sources
            var localSource = new LocalConfigurationSource(_configPath);
            
            // 2. Create configuration service with primary (remote) and fallback (local) sources
            IConfigurationSource primarySource = null;
            
            // We'll determine the primary source after loading local config to check for ConfigServerUrl
            var localConfig = localSource.LoadConfigurationAsync().Result;
            if (localConfig != null && !string.IsNullOrEmpty(localConfig.ConfigServerUrl))
            {
                primarySource = new RemoteConfigurationSource(localConfig.ConfigServerUrl);
            }

            // If no remote server configured, use local as primary
            primarySource = primarySource ?? localSource;
            var configService = new ConfigurationService(primarySource, localSource);

            // 3. Create file monitoring service
            var checksumCalculator = new ChecksumCalculator();
            var fileMonitor = new FileMonitorService(checksumCalculator);

            // 4. Create metrics collector
            IMetricsCollector metricsCollector = new PrometheusMetricsExporter();

            return new ServiceContext
            {
                ConfigurationService = configService,
                FileMonitorService = fileMonitor,
                MetricsCollector = metricsCollector
            };
        }
    }

    /// <summary>
    /// Contains all dependencies needed for the service to operate.
    /// </summary>
    public class ServiceContext
    {
        public ConfigurationService ConfigurationService { get; set; }
        public FileMonitorService FileMonitorService { get; set; }
        public IMetricsCollector MetricsCollector { get; set; }
    }
}
