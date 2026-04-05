using BaseLib.Core.Security;

namespace BaseLib.Core.Containers
{
    /// <summary>
    /// <see cref="ICoreSecretsVault"/> implementation that reads secrets from environment variables.
    /// The <c>secretName</c> parameter is used directly as the environment variable name.
    /// Follows standard container secret-injection practice (Docker secrets, Kubernetes secrets).
    /// </summary>
    public class EnvironmentSecretsVault : ICoreSecretsVault
    {
        /// <inheritdoc/>
        public Task<string> GetSecretValueAsync(string secretName)
        {
            var value = Environment.GetEnvironmentVariable(secretName);
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException(
                    $"Environment variable '{secretName}' is not set or is empty.");

            return Task.FromResult(value);
        }
    }
}
