using System;
using System.Collections.Generic;
using System.Linq;
using FileChecksum.Core.Models;

namespace FileChecksum.Core.Services
{
    /// <summary>
    /// Validates configuration objects to ensure they meet requirements.
    /// Provides detailed validation results with specific error messages.
    /// </summary>
    public class ConfigurationValidator
    {
        private readonly List<string> _errors = new List<string>();

        /// <summary>
        /// Validates a configuration object comprehensively.
        /// </summary>
        /// <param name="config">Configuration to validate.</param>
        /// <returns>True if configuration is valid, false if any validation rules failed.</returns>
        public bool Validate(FileChecksumsConfig config)
        {
            _errors.Clear();

            if (config == null)
            {
                _errors.Add("Configuration is null.");
                return false;
            }

            // Validate files list
            if (config.Files == null || config.Files.Count == 0)
            {
                _errors.Add("Configuration contains no files to monitor.");
                return false;
            }

            // Validate each file
            for (int i = 0; i < config.Files.Count; i++)
            {
                ValidateFileItem(config.Files[i], i);
            }

            // Validate port
            if (config.Port < 1 || config.Port > 65535)
            {
                _errors.Add($"Port {config.Port} is out of valid range (1-65535).");
            }

            // Validate check interval
            if (config.CheckIntervalMs < 100)
            {
                _errors.Add($"CheckIntervalMs {config.CheckIntervalMs} is too small (minimum 100ms).");
            }

            return _errors.Count == 0;
        }

        /// <summary>
        /// Gets the list of validation errors from the last validation call.
        /// </summary>
        public IEnumerable<string> GetErrors()
        {
            return _errors.AsReadOnly();
        }

        private void ValidateFileItem(FileCheckItem file, int index)
        {
            if (file == null)
            {
                _errors.Add($"File at index {index} is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(file.Path))
            {
                _errors.Add($"File at index {index}: Path is required.");
            }

            if (string.IsNullOrWhiteSpace(file.Algorithm))
            {
                _errors.Add($"File at index {index} ({file.Path}): Algorithm is required.");
            }
            else if (!IsValidAlgorithm(file.Algorithm))
            {
                _errors.Add($"File at index {index} ({file.Path}): Algorithm '{file.Algorithm}' is not supported. Use: SHA256, SHA1, MD5, SHA512, SHA384.");
            }

            if (string.IsNullOrWhiteSpace(file.ExpectedChecksum))
            {
                _errors.Add($"File at index {index} ({file.Path}): ExpectedChecksum is required.");
            }
        }

        private bool IsValidAlgorithm(string algorithm)
        {
            var validAlgorithms = new[] { "SHA256", "SHA1", "MD5", "SHA512", "SHA384" };
            return validAlgorithms.Contains(algorithm.ToUpper());
        }
    }
}
