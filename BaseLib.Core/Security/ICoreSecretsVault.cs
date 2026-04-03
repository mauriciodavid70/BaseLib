namespace BaseLib.Core.Security
{
    /// <summary>
    /// Retrieves sensitive configuration values (credentials, API keys, connection strings)
    /// from a secure vault. The AWS implementation delegates to AWS Secrets Manager
    /// via <c>AmazonSecretsVault</c>.
    /// </summary>
    public interface ICoreSecretsVault
    {
        /// <summary>
        /// Retrieves the plain-text value of the secret identified by <paramref name="secretName"/>.
        /// </summary>
        /// <param name="secretName">The name or ARN of the secret to retrieve.</param>
        /// <returns>The secret's plain-text string value.</returns>
        Task<string> GetSecretValueAsync(string secretName);
    }
}