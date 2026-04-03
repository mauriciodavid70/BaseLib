namespace Amazon.Runtime.CredentialManagement
{
    /// <summary>
    /// Convenience factory that creates <see cref="AWSCredentials"/> from the named profile
    /// specified in the <c>AWS_PROFILE</c> environment variable.
    /// Intended for local development; in AWS environments use the IAM role attached to the compute resource.
    /// </summary>
    public class AwsCredentialsFactory
    {
        /// <summary>
        /// Creates <see cref="AWSCredentials"/> from the profile named in the <c>AWS_PROFILE</c>
        /// environment variable. Throws if the variable is not set or the profile is not found.
        /// </summary>
        public static AWSCredentials Create()
        {
            var profileName = Environment.GetEnvironmentVariable("AWS_PROFILE");

            if (profileName == null) throw new NullReferenceException(nameof(profileName));

            var chain = new CredentialProfileStoreChain();

            chain.TryGetAWSCredentials(profileName, out AWSCredentials credentials);

            return credentials;
        }
    }
} 