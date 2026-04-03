using Amazon.S3;
using Amazon.S3.Model;

namespace BaseLib.Core.Security.AmazonCloud
{
    /// <summary>
    /// Provides s3 caching to an encryption provider
    /// A new key is requested every day and the wrapped/encrypted versions is stored in s3
    /// when retrieved is unwrapped/decrypted using the inner provider
    /// a local copy of the key is cached in memory 
    /// </summary>
    public class S3CachedEncryptionProvider : IEncryptionKeyProvider
    {
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        static EncryptionKeyPair? keyPair;

        private readonly IEncryptionKeyProvider innerProvider;
        private readonly IAmazonS3 s3;
        private readonly string bucketName;
        private readonly string folderName;
        
        /// <summary>Initializes the caching provider.</summary>
        /// <param name="innerProvider">Underlying provider used to generate or unwrap keys when the cache misses.</param>
        /// <param name="s3">S3 client used to read and write wrapped key files.</param>
        /// <param name="bucketName">S3 bucket where wrapped key files are stored.</param>
        /// <param name="folderName">S3 key prefix for key files. Defaults to <c>cache/keys</c>.</param>
        public S3CachedEncryptionProvider(IEncryptionKeyProvider innerProvider, IAmazonS3 s3, string bucketName, string folderName = "cache/keys")
        {
            this.innerProvider = innerProvider;
            this.s3 = s3;
            this.bucketName = bucketName;
            this.folderName = folderName;
        }

        /// <inheritdoc/>
        public async Task<(byte[] key, byte[] wrappedKey)> GetEncryptionKeyAsync()
        {
            if (keyPair == null)
            {
                await semaphore.WaitAsync();
                try
                {
                    keyPair ??= await GetEncryptionKeyPairFromS3Async() ?? await GetEncryptionKeyPairFromInnerProviderAsync();
                }
                finally
                {
                    semaphore.Release();
                }
            }

            return (keyPair.Key, keyPair.WrappedKey);
        }

        private async Task<EncryptionKeyPair> GetEncryptionKeyPairFromInnerProviderAsync()
        {
            var (key, wrappedKey) = await this.innerProvider.GetEncryptionKeyAsync();

            //store wrapped key in s3            
            await this.s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = this.GetKeyFileName(),
                InputStream = new MemoryStream(wrappedKey),
            });

            //return the newly created key pair
            return new EncryptionKeyPair(key, wrappedKey);
        }

        private async Task<EncryptionKeyPair?> GetEncryptionKeyPairFromS3Async()
        {

            var (exists, response) = await this.s3.GetObjectIfExistsAsync(new GetObjectRequest
            {
                BucketName = this.bucketName,
                Key = GetKeyFileName()
            });

            if (exists && response != null)
            {
                using (var responseStream = response.ResponseStream)
                using (var stream = new MemoryStream())
                {
                    responseStream.CopyTo(stream);
                    var wrappedKey = stream.ToArray();
                    var key = await this.innerProvider.UnwrapKeyAsync(wrappedKey);
                    return new EncryptionKeyPair(key, wrappedKey);
                }
            }
            return null;
        }

        private string GetKeyFileName()
        {
            long sequence = ((DateTimeOffset)DateTimeOffset.UtcNow.Date).ToUnixTimeSeconds();

            var objectKey = $"{this.folderName}/wrapped_{sequence}.key";
            return objectKey;
        }

        /// <inheritdoc/>
        public Task<byte[]> UnwrapKeyAsync(byte[] wrappedKey)
        {
            if (keyPair != null && keyPair.WrappedKey==wrappedKey)
            {
                return Task.FromResult(keyPair.Key);
            }
            
            return this.innerProvider.UnwrapKeyAsync(wrappedKey);
        }

        internal record EncryptionKeyPair(byte[] Key, byte[] WrappedKey);
    }
}