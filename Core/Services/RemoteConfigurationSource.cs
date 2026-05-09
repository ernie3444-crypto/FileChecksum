using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FileChecksum.Core.Abstractions;
using FileChecksum.Core.Models;

namespace FileChecksum.Core.Services
{
    /// <summary>
    /// Loads configuration from a remote HTTP server.
    /// Implements the primary configuration source with timeout handling.
    /// </summary>
    public class RemoteConfigurationSource : IConfigurationSource
    {
        private readonly string _configServerUrl;
        private readonly HttpClient _httpClient;
        private readonly XmlSerializer _serializer;
        private readonly int _timeoutSeconds;

        public string SourceDescription => $"Remote server: {_configServerUrl}";

        public RemoteConfigurationSource(string configServerUrl, int timeoutSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(configServerUrl))
            {
                throw new ArgumentNullException(nameof(configServerUrl));
            }

            _configServerUrl = configServerUrl;
            _timeoutSeconds = timeoutSeconds;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            _serializer = new XmlSerializer(typeof(FileChecksumsConfig));
        }

        public async Task<FileChecksumsConfig> LoadConfigurationAsync()
        {
            try
            {
                string configContent = await _httpClient.GetStringAsync(_configServerUrl);
                
                using (var reader = new StringReader(configContent))
                {
                    return (FileChecksumsConfig)_serializer.Deserialize(reader);
                }
            }
            catch (Exception)
            {
                // Return null on any error - caller will use fallback
                return null;
            }
        }
    }
}
