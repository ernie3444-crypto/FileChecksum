using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileChecksum.Core.Abstractions;
using FileChecksum.Core.Models;

namespace FileChecksum.Core.Services
{
    /// <summary>
    /// Orchestrates configuration loading with the following strategy:
    /// 1. Try to load from remote server (if configured)
    /// 2. Filter by PCType on the client (master server returns all files)
    /// 3. Apply drive letter override (if configured)
    /// 4. Fall back to local configuration if remote fetch fails
    /// 5. Validate the final configuration
    /// </summary>
    public class ConfigurationService
    {
        private readonly IConfigurationSource _primarySource;
        private readonly IConfigurationSource _fallbackSource;
        private readonly ConfigurationValidator _validator;

        public ConfigurationService(IConfigurationSource primarySource, IConfigurationSource fallbackSource)
        {
            _primarySource = primarySource ?? throw new ArgumentNullException(nameof(primarySource));
            _fallbackSource = fallbackSource ?? throw new ArgumentNullException(nameof(fallbackSource));
            _validator = new ConfigurationValidator();
        }

        /// <summary>
        /// Loads configuration with fallback strategy.
        /// Attempts primary source first, falls back to fallback source if primary fails.
        /// </summary>
        /// <returns>Loaded and validated configuration, or null if all sources fail.</returns>
        public async Task<LoadConfigurationResult> LoadConfigurationAsync()
        {
            var result = new LoadConfigurationResult();

            // Try primary source first
            var config = await _primarySource.LoadConfigurationAsync();
            result.PrimarySourceAttempted = true;
            result.PrimarySourceDescription = _primarySource.SourceDescription;

            if (config != null && _validator.Validate(config))
            {
                result.LoadedConfiguration = config;
                result.SourceUsed = _primarySource.SourceDescription;
                result.Success = true;
                return result;
            }

            // Primary source failed or produced invalid config, try fallback
            config = await _fallbackSource.LoadConfigurationAsync();
            result.FallbackSourceAttempted = true;
            result.FallbackSourceDescription = _fallbackSource.SourceDescription;

            if (config != null && _validator.Validate(config))
            {
                result.LoadedConfiguration = config;
                result.SourceUsed = _fallbackSource.SourceDescription;
                result.Success = true;
                result.FallbackUsed = true;
                return result;
            }

            // Both sources failed
            result.Success = false;
            result.ValidationErrors = _validator.GetErrors();
            return result;
        }

        /// <summary>
        /// Filters configuration files by PCType.
        /// The master server returns all files; the client filters to files matching its own PCType.
        /// </summary>
        /// <param name="config">Configuration with potentially multiple PCTypes.</param>
        /// <param name="clientPCType">The PC type to filter for (e.g., "Workstation-A").</param>
        /// <returns>New configuration with only files matching the client's PCType.</returns>
        public FileChecksumsConfig FilterByPCType(FileChecksumsConfig config, string clientPCType)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // If no PCType filtering is defined, return all files
            if (string.IsNullOrWhiteSpace(clientPCType))
            {
                return config;
            }

            // Filter files: include files with matching PCType or files with no PCType specified
            var filteredFiles = config.Files
                .Where(f => string.IsNullOrWhiteSpace(f.PCType) || 
                           f.PCType.Equals(clientPCType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new FileChecksumsConfig
            {
                PCType = config.PCType,
                Port = config.Port,
                CheckIntervalMs = config.CheckIntervalMs,
                ConfigServerUrl = config.ConfigServerUrl,
                DriveOverride = config.DriveOverride,
                Files = filteredFiles
            };
        }

        /// <summary>
        /// Applies drive letter override to file paths.
        /// Useful when machines have identical configurations but files on different drives.
        /// 
        /// Example:
        ///   Master config: C:\Critical\file.exe
        ///   DriveOverride: D
        ///   Result: D:\Critical\file.exe
        /// </summary>
        /// <param name="config">Configuration to modify.</param>
        /// <param name="driveOverride">Drive letter (without colon), e.g., "D", "E", "Z".</param>
        /// <returns>New configuration with updated file paths.</returns>
        public FileChecksumsConfig ApplyDriveOverride(FileChecksumsConfig config, string driveOverride)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // No override specified
            if (string.IsNullOrWhiteSpace(driveOverride))
            {
                return config;
            }

            // Normalize drive letter (remove colon if present, ensure single letter)
            driveOverride = driveOverride.Trim().ToUpper();
            if (driveOverride.EndsWith(":"))
            {
                driveOverride = driveOverride.Substring(0, driveOverride.Length - 1);
            }

            // Validate drive letter
            if (driveOverride.Length != 1 || !char.IsLetter(driveOverride[0]))
            {
                throw new ArgumentException($"Invalid drive letter: {driveOverride}. Use single letter (e.g., 'D', 'E').");
            }

            // Create new configuration with updated paths
            var updatedFiles = new List<FileCheckItem>();
            foreach (var file in config.Files)
            {
                var updatedFile = new FileCheckItem
                {
                    Path = ReplaceDriveLetter(file.Path, driveOverride),
                    Algorithm = file.Algorithm,
                    Description = file.Description,
                    ExpectedChecksum = file.ExpectedChecksum,
                    PCType = file.PCType
                };
                updatedFiles.Add(updatedFile);
            }

            return new FileChecksumsConfig
            {
                PCType = config.PCType,
                Port = config.Port,
                CheckIntervalMs = config.CheckIntervalMs,
                ConfigServerUrl = config.ConfigServerUrl,
                DriveOverride = driveOverride,
                Files = updatedFiles
            };
        }

        /// <summary>
        /// Replaces the drive letter in a file path.
        /// </summary>
        /// <param name="path">Original path (e.g., "C:\folder\file.txt")</param>
        /// <param name="newDrive">New drive letter (e.g., "D")</param>
        /// <returns>Updated path (e.g., "D:\folder\file.txt")</returns>
        private string ReplaceDriveLetter(string path, string newDrive)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            // Check if path starts with a drive letter (e.g., "C:", "D:")
            if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':')
            {
                // Replace the drive letter
                return newDrive + ":" + path.Substring(2);
            }

            // Not a drive-based path, return unchanged
            return path;
        }
    }

    /// <summary>
    /// Result of a configuration load operation, containing success/failure details.
    /// </summary>
    public class LoadConfigurationResult
    {
        public bool Success { get; set; }
        public FileChecksumsConfig LoadedConfiguration { get; set; }
        public string SourceUsed { get; set; }
        public bool FallbackUsed { get; set; }

        public bool PrimarySourceAttempted { get; set; }
        public string PrimarySourceDescription { get; set; }

        public bool FallbackSourceAttempted { get; set; }
        public string FallbackSourceDescription { get; set; }

        public IEnumerable<string> ValidationErrors { get; set; } = new List<string>();

        public string GetDetailedLog()
        {
            var lines = new List<string>();
            lines.Add($"Configuration Load Result: {(Success ? "SUCCESS" : "FAILED")}");
            lines.Add($"Primary source ({PrimarySourceDescription}): {(PrimarySourceAttempted ? "Attempted" : "Skipped")}");
            lines.Add($"Fallback source ({FallbackSourceDescription}): {(FallbackSourceAttempted ? "Attempted" : "Skipped")}");
            lines.Add($"Source used: {SourceUsed}");
            
            if (!Success && ValidationErrors.Any())
            {
                lines.Add("Validation errors:");
                foreach (var error in ValidationErrors)
                {
                    lines.Add($"  - {error}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
