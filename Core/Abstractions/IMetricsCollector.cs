using System.Collections.Generic;
using FileChecksum.Core.Models;

namespace FileChecksum.Core.Abstractions
{
    /// <summary>
    /// Abstraction for collecting and exporting metrics.
    /// Decouples metrics implementation from the core service logic.
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Updates metrics based on the current configuration and results.
        /// </summary>
        /// <param name="monitoredFiles">The files that were just monitored with their results.</param>
        void RecordCheckResults(IEnumerable<FileCheckItem> monitoredFiles);

        /// <summary>
        /// Exports metrics in Prometheus format.
        /// </summary>
        /// <returns>Metrics in Prometheus text format.</returns>
        string ExportMetrics();
    }
}
