using System.Threading.Tasks;
using FileChecksum.Core.Models;

namespace FileChecksum.Core.Abstractions
{
    /// <summary>
    /// Abstraction for loading configuration from different sources.
    /// Enables swapping between local files and remote servers without changing consumer code.
    /// </summary>
    public interface IConfigurationSource
    {
        /// <summary>
        /// Loads configuration from the source asynchronously.
        /// </summary>
        /// <returns>The loaded configuration, or null if unable to load from this source.</returns>
        Task<FileChecksumsConfig> LoadConfigurationAsync();

        /// <summary>
        /// Human-readable description of this source (e.g., "Local file: C:\config.xml").
        /// </summary>
        string SourceDescription { get; }
    }
}
