namespace BaseLib.Core.Security
{
    /// <summary>
    /// Generates and unwraps encryption keys for envelope encryption.
    /// The AWS implementation uses KMS via <c>KmsEncryptionKeyProvider</c>.
    /// For caching, wrap any provider with <c>S3CachedEncryptionProvider</c>.
    /// </summary>
    public interface IEncryptionKeyProvider
    {
        /// <summary>
        /// Generates a new data encryption key and its KMS-wrapped (encrypted) counterpart.
        /// </summary>
        /// <returns>
        /// A tuple containing the plaintext <c>key</c> (use for encrypting data) and
        /// the <c>wrappedKey</c> (store alongside encrypted data; never store the plaintext key).
        /// </returns>
        Task<(byte[] key, byte[] wrappedKey)> GetEncryptionKeyAsync();

        /// <summary>
        /// Decrypts a previously wrapped key back to its plaintext form.
        /// </summary>
        /// <param name="wrappedKey">The encrypted key bytes returned by <see cref="GetEncryptionKeyAsync"/>.</param>
        /// <returns>The plaintext key bytes.</returns>
        Task<byte[]> UnwrapKeyAsync(byte[] wrappedKey);
    }
}