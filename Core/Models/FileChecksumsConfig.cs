using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace FileChecksum.Core.Models
{
    /// <summary>
    /// Root configuration object representing the file checksum monitoring setup.
    /// Single source of truth for what files to monitor and how to monitor them.
    /// </summary>
    [XmlRoot("FileChecksums")]
    public class FileChecksumsConfig
    {
        /// <summary>
        /// The type of PC this configuration is for (e.g., "Workstation-A", "Server-B").
        /// Used by clients to filter files from the master server.
        /// </summary>
        [XmlAttribute("pcType")]
        public string PCType { get; set; }

        /// <summary>
        /// Port for the Prometheus metrics HTTP endpoint.
        /// </summary>
        [XmlAttribute("port")]
        public int Port { get; set; } = 5000;

        /// <summary>
        /// Interval in milliseconds between checksum verification checks.
        /// </summary>
        [XmlAttribute("checkIntervalMs")]
        public int CheckIntervalMs { get; set; } = 60000;

        /// <summary>
        /// URL to the master configuration server.
        /// If specified, the client will attempt to fetch the master config from this URL.
        /// If fetch fails, it falls back to local config.
        /// </summary>
        [XmlAttribute("configServerUrl")]
        public string ConfigServerUrl { get; set; }

        /// <summary>
        /// List of files to monitor for integrity.
        /// </summary>
        [XmlElement("File")]
        public List<FileCheckItem> Files { get; set; } = new List<FileCheckItem>();

        /// <summary>
        /// Validates that the configuration has minimum required values.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid()
        {
            return Files != null && Files.Count > 0;
        }
    }

    /// <summary>
    /// Represents a single file to be monitored for checksum integrity.
    /// </summary>
    public class FileCheckItem
    {
        /// <summary>
        /// Full path to the file on disk.
        /// </summary>
        [XmlAttribute("path")]
        public string Path { get; set; }

        /// <summary>
        /// Hash algorithm to use: SHA256, SHA1, MD5, SHA512, SHA384.
        /// </summary>
        [XmlAttribute("algorithm")]
        public string Algorithm { get; set; }

        /// <summary>
        /// Human-readable description of this file (appears in metrics labels).
        /// </summary>
        [XmlAttribute("description")]
        public string Description { get; set; }

        /// <summary>
        /// The expected checksum value for integrity verification.
        /// </summary>
        [XmlElement("ExpectedChecksum")]
        public string ExpectedChecksum { get; set; }

        /// <summary>
        /// The PC type this file applies to (server-side filtering).
        /// When loading from a master server, the client filters to only files matching its PCType.
        /// </summary>
        [XmlAttribute("pcType")]
        public string PCType { get; set; }

        // === Runtime state (not serialized) ===

        /// <summary>
        /// The actual checksum calculated from the file.
        /// </summary>
        [XmlIgnore]
        public string CalculatedChecksum { get; set; }

        /// <summary>
        /// Whether the calculated checksum matches the expected value.
        /// </summary>
        [XmlIgnore]
        public bool IsValid { get; set; }

        /// <summary>
        /// Timestamp when the checksum was last calculated.
        /// </summary>
        [XmlIgnore]
        public DateTime LastChecked { get; set; }

        /// <summary>
        /// If checksum calculation failed, this contains the error message.
        /// </summary>
        [XmlIgnore]
        public string ErrorMessage { get; set; }
    }
}
