using System;
using System.Collections.Generic;
using FileChecksum.Core.Models;

namespace FileChecksum.Core.Services
{
    /// <summary>
    /// Monitors files by calculating their checksums and comparing against expected values.
    /// Updates file items with calculated checksums, validation results, and timestamps.
    /// </summary>
    public class FileMonitorService
    {
        private readonly ChecksumCalculator _checksumCalculator;

        public FileMonitorService(ChecksumCalculator checksumCalculator = null)
        {
            _checksumCalculator = checksumCalculator ?? new ChecksumCalculator();
        }

        /// <summary>
        /// Performs checksum verification on all configured files.
        /// Updates each file item with calculated checksum, validation result, and timestamp.
        /// </summary>
        /// <param name="filesToMonitor">Collection of files to check.</param>
        public void VerifyFileChecksums(IEnumerable<FileCheckItem> filesToMonitor)
        {
            foreach (var fileItem in filesToMonitor)
            {
                VerifyFileChecksum(fileItem);
            }
        }

        /// <summary>
        /// Verifies a single file's checksum.
        /// </summary>
        private void VerifyFileChecksum(FileCheckItem fileItem)
        {
            try
            {
                // Calculate the checksum
                fileItem.CalculatedChecksum = _checksumCalculator.CalculateChecksum(
                    fileItem.Path,
                    fileItem.Algorithm);

                // Compare with expected value
                fileItem.IsValid = fileItem.CalculatedChecksum.Equals(
                    fileItem.ExpectedChecksum,
                    StringComparison.OrdinalIgnoreCase);

                fileItem.LastChecked = DateTime.UtcNow;
                fileItem.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                fileItem.IsValid = false;
                fileItem.LastChecked = DateTime.UtcNow;
                fileItem.ErrorMessage = ex.Message;
                fileItem.CalculatedChecksum = null;
            }
        }
    }
}
