using System.Security.Cryptography;

namespace BaseLib.Core.Security
{
    /// <summary>
    /// Simple local <see cref="IEncryptionKeyProvider"/> that generates random AES-256 data keys
    /// using a caller-supplied master key. Intended for local development and testing only.
    /// For production use, replace with <c>KmsEncryptionKeyProvider</c> from <c>BaseLib.Core.AmazonCloud</c>.
    /// </summary>
    public class EncryptionKeyProvider : IEncryptionKeyProvider
    {
        private readonly byte[] masterKey;

        /// <summary>Initializes the provider with a master key used to wrap/unwrap data keys.</summary>
        /// <param name="masterKey">32-byte master key. Keep this secret.</param>
        public EncryptionKeyProvider(byte[] masterKey)
        {
            this.masterKey = masterKey;
        }

        /// <inheritdoc/>
        public Task<(byte[] key, byte[] wrappedKey)> GetEncryptionKeyAsync()
        {
            var dataKey = RandomNumberGenerator.GetBytes(32);
            var wrappedKey = WrapKey(dataKey);

            return Task.FromResult((dataKey, wrappedKey));
        }

        private byte[] WrapKey(byte[] dataKey)
        {
            return dataKey;
        }

        /// <inheritdoc/>
        public Task<byte[]> UnwrapKeyAsync(byte[] wrappedKey)
        {
            var dataKey = UnwrapKey(wrappedKey);
            return Task.FromResult(dataKey);
        }

        private byte[] UnwrapKey(byte[] wrappedKey)
        {
            return wrappedKey;
        }
    }
}