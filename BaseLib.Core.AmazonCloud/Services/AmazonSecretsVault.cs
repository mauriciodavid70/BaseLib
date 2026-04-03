using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BaseLib.Core.Security.AmazonCloud
{
    /// <summary>
    /// <see cref="ICoreSecretsVault"/> implementation backed by AWS Secrets Manager.
    /// Retrieves secret string values by name or ARN.
    /// </summary>
    public class AmazonSecretsVault : ICoreSecretsVault
    {
        private readonly IAmazonSecretsManager secretsManager;

        /// <summary>Initializes the vault with a Secrets Manager client.</summary>
        /// <param name="amazonSecretsManager">The AWS Secrets Manager service client.</param>
        public AmazonSecretsVault(IAmazonSecretsManager amazonSecretsManager)
        {
            secretsManager = amazonSecretsManager;
        }

        /// <inheritdoc/>
        public async Task<string> GetSecretValueAsync(string secretName)
        {

            var response = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretName
            });

            return response.SecretString;

        }
    }

    
}