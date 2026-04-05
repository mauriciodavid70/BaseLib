using BaseLib.Core.Containers;
using Xunit;

namespace BaseLib.Core.Tests.Services
{
    public class EnvironmentSecretsVaultTests
    {
        private readonly EnvironmentSecretsVault vault = new();

        [Fact]
        public async Task GetSecretValueAsync_VariablePresent_ReturnsValue()
        {
            // Arrange
            var varName = $"BASELIB_TEST_{Guid.NewGuid():N}";
            var expectedValue = "super-secret";
            Environment.SetEnvironmentVariable(varName, expectedValue);

            try
            {
                // Act
                var result = await vault.GetSecretValueAsync(varName);

                // Assert
                Assert.Equal(expectedValue, result);
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, null);
            }
        }

        [Fact]
        public async Task GetSecretValueAsync_VariableAbsent_ThrowsInvalidOperationException()
        {
            // Arrange — use a name that almost certainly does not exist
            var varName = $"BASELIB_TEST_{Guid.NewGuid():N}";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => vault.GetSecretValueAsync(varName));
        }
    }
}
