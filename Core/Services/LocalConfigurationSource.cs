using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FileChecksum.Core.Abstractions;
using FileChecksum.Core.Models;

namespace FileChecksum.Core.Services
{
    /// <summary>
    /// Loads configuration from a local XML file on disk.
    /// Used as fallback when remote server is unavailable.
    /// </summary>
    public class LocalConfigurationSource : IConfigurationSource
    {
        private readonly string _configPath;
        private readonly XmlSerializer _serializer;

        public string SourceDescription => $"Local file: {_configPath}";

        public LocalConfigurationSource(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentNullException(nameof(configPath));
            }

            _configPath = configPath;
            _serializer = new XmlSerializer(typeof(FileChecksumsConfig));
        }

        public Task<FileChecksumsConfig> LoadConfigurationAsync()
        {
            return Task.Run(() =>
            {
                if (!File.Exists(_configPath))
                {
                    return null;
                }

                try
                {
                    using (var reader = new StreamReader(_configPath))
                    {
                        return (FileChecksumsConfig)_serializer.Deserialize(reader);
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }
    }
}
