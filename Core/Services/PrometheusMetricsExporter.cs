using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FileChecksum.Core.Abstractions;
using FileChecksum.Core.Models;
using Prometheus;

namespace FileChecksum.Core.Services
{
    /// <summary>
    /// Collects checksum verification results and exports them as Prometheus metrics.
    /// Implements IMetricsCollector for clean integration with the service.
    /// </summary>
    public class PrometheusMetricsExporter : IMetricsCollector
    {
        private readonly Counter _checksumCalculationsTotal;
        private readonly Gauge _checksumMatches;
        private readonly Gauge _checksumMismatches;
        private readonly Gauge _filesMonitored;
        private readonly Gauge _lastCheckTimestamp;
        private readonly Counter _checksumCheckCycles;

        public PrometheusMetricsExporter()
        {
            // Total checksum calculations performed
            _checksumCalculationsTotal = Metrics.CreateCounter(
                "filechecksum_calculations_total",
                "Total number of checksum calculations performed",
                new CounterConfiguration { LabelNames = new[] { "file", "algorithm" } });

            // Current verification status: matches = 1 (pass), 0 (fail)
            _checksumMatches = Metrics.CreateGauge(
                "filechecksum_matches",
                "Checksum validation result (1=pass, 0=fail)",
                new GaugeConfiguration { LabelNames = new[] { "file", "description", "algorithm" } });

            // Current verification status: mismatches = 1 (fail), 0 (pass)
            _checksumMismatches = Metrics.CreateGauge(
                "filechecksum_mismatches",
                "Checksum validation failure (1=fail, 0=pass)",
                new GaugeConfiguration { LabelNames = new[] { "file", "description", "algorithm" } });

            // Number of files currently being monitored
            _filesMonitored = Metrics.CreateGauge(
                "filechecksum_files_monitored",
                "Number of files being monitored");

            // Unix timestamp of the last check for each file
            _lastCheckTimestamp = Metrics.CreateGauge(
                "filechecksum_last_check_timestamp_seconds",
                "Unix timestamp of the last checksum check",
                new GaugeConfiguration { LabelNames = new[] { "file" } });

            // Number of complete check cycles
            _checksumCheckCycles = Metrics.CreateCounter(
                "filechecksum_check_cycles_total",
                "Total number of checksum verification cycles");
        }

        /// <summary>
        /// Records the results of a checksum verification run.
        /// </summary>
        public void RecordCheckResults(IEnumerable<FileCheckItem> monitoredFiles)
        {
            var filesList = monitoredFiles.ToList();
            
            _filesMonitored.Set(filesList.Count);
            _checksumCheckCycles.Inc();

            foreach (var file in filesList)
            {
                RecordFileCheckResult(file);
            }
        }

        /// <summary>
        /// Exports all collected metrics in Prometheus text format.
        /// </summary>
        public string ExportMetrics()
        {
            using (var stream = new MemoryStream())
            {
                Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream).GetAwaiter().GetResult();
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private void RecordFileCheckResult(FileCheckItem file)
        {
            var fileLabel = file.Path;
            var descriptionLabel = file.Description ?? string.Empty;
            var algorithmLabel = file.Algorithm;

            // Increment calculation counter
            _checksumCalculationsTotal
                .WithLabels(fileLabel, algorithmLabel)
                .Inc();

            // Record match/mismatch status
            if (file.IsValid)
            {
                _checksumMatches
                    .WithLabels(fileLabel, descriptionLabel, algorithmLabel)
                    .Set(1);

                _checksumMismatches
                    .WithLabels(fileLabel, descriptionLabel, algorithmLabel)
                    .Set(0);
            }
            else
            {
                _checksumMatches
                    .WithLabels(fileLabel, descriptionLabel, algorithmLabel)
                    .Set(0);

                _checksumMismatches
                    .WithLabels(fileLabel, descriptionLabel, algorithmLabel)
                    .Set(1);
            }

            // Record last check timestamp
            var unixTimestamp = ToUnixTimestamp(file.LastChecked);
            _lastCheckTimestamp
                .WithLabels(fileLabel)
                .Set(unixTimestamp);
        }

        private double ToUnixTimestamp(DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (dateTime.ToUniversalTime() - epoch).TotalSeconds;
        }
    }
}
