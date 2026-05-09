using System;
using System.IO;
using System.Security.Cryptography;

namespace FileChecksum.Core.Services
{
    /// <summary>
    /// Calculates and verifies file checksums using various cryptographic algorithms.
    /// Single responsibility: cryptographic hash computation.
    /// </summary>
    public class ChecksumCalculator
    {
        private static readonly int BufferSize = 4096;

        /// <summary>
        /// Calculates the checksum of a file using the specified algorithm.
        /// </summary>
        /// <param name="filePath">Full path to the file.</param>
        /// <param name="algorithm">Algorithm name: SHA256, SHA1, MD5, SHA512, SHA384.</param>
        /// <returns>The hash as a lowercase hexadecimal string.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        /// <exception cref="NotSupportedException">Thrown if the algorithm is not supported.</exception>
        public string CalculateChecksum(string filePath, string algorithm)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            using (var hashAlgorithm = CreateHashAlgorithm(algorithm))
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: false))
            {
                byte[] hash = hashAlgorithm.ComputeHash(fileStream);
                return ConvertHashToHexString(hash);
            }
        }

        /// <summary>
        /// Verifies that a file's checksum matches an expected value.
        /// </summary>
        /// <param name="filePath">Full path to the file.</param>
        /// <param name="algorithm">Algorithm name: SHA256, SHA1, MD5, SHA512, SHA384.</param>
        /// <param name="expectedChecksum">The checksum to verify against.</param>
        /// <returns>True if checksums match (case-insensitive), false otherwise.</returns>
        public bool VerifyChecksum(string filePath, string algorithm, string expectedChecksum)
        {
            try
            {
                string calculated = CalculateChecksum(filePath, algorithm);
                return calculated.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a hash algorithm instance from an algorithm name.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown if algorithm is not recognized.</exception>
        private HashAlgorithm CreateHashAlgorithm(string algorithm)
        {
            if (string.IsNullOrWhiteSpace(algorithm))
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            switch (algorithm.ToUpper())
            {
                case "SHA256":
                    return SHA256.Create();
                case "SHA1":
                    return SHA1.Create();
                case "MD5":
                    return MD5.Create();
                case "SHA512":
                    return SHA512.Create();
                case "SHA384":
                    return SHA384.Create();
                default:
                    throw new NotSupportedException($"Algorithm '{algorithm}' is not supported. Use: SHA256, SHA1, MD5, SHA512, SHA384");
            }
        }

        /// <summary>
        /// Converts a byte array to lowercase hexadecimal string.
        /// </summary>
        private string ConvertHashToHexString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
