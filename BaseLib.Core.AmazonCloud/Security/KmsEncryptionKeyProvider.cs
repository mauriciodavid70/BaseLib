using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;

namespace BaseLib.Core.Security.AmazonCloud
{
    /// <summary>
    /// <see cref="IEncryptionKeyProvider"/> implementation backed by AWS KMS.
    /// Generates AES-256 data keys via <c>GenerateDataKey</c> and decrypts them via <c>Decrypt</c>.
    /// Wrap with <c>S3CachedEncryptionProvider</c> to avoid a KMS call on every serialization.
    /// </summary>
    public class KmsEncryptionKeyProvider : IEncryptionKeyProvider
    {
        private readonly IAmazonKeyManagementService kmsClient;
        private readonly string kmsKeyName;

        /// <summary>Initializes the provider.</summary>
        /// <param name="kmsClient">KMS service client.</param>
        /// <param name="kmsKeyName">KMS key ID, alias (e.g. <c>alias/my-key</c>), or ARN.</param>
        public KmsEncryptionKeyProvider(IAmazonKeyManagementService kmsClient, string kmsKeyName)
        {
            this.kmsClient = kmsClient;
            this.kmsKeyName = kmsKeyName;
        }

        /// <summary>
        /// Generate the encryption key with KMS GenerateDataKey method
        /// </summary>
        public async Task<(byte[] key, byte[] wrappedKey)> GetEncryptionKeyAsync()
        {
            var response = await kmsClient.GenerateDataKeyAsync(new GenerateDataKeyRequest
            {
                KeyId = kmsKeyName,
                KeySpec = DataKeySpec.AES_256,

            });
            return (response.Plaintext.ToArray(), response.CiphertextBlob.ToArray());
        }


        /// <inheritdoc/>
        public async Task<byte[]> UnwrapKeyAsync(byte[] wrappedKey)
        {
            var response = await kmsClient.DecryptAsync(new DecryptRequest
            {
                CiphertextBlob = new MemoryStream(wrappedKey)
            });
            return response.Plaintext.ToArray();
        }
    }
}