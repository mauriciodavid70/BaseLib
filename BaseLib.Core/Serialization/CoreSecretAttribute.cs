namespace BaseLib.Core.Serialization
{
    /// <summary>
    /// Marks a request or response property as sensitive. When serialized with
    /// <c>CoreSecureJsonSerializer</c> or <c>SecurePolymorphicConverter</c>, the property value
    /// is encrypted using AES-256 with a key managed by <see cref="Security.IEncryptionKeyProvider"/>.
    /// Apply to fields such as passwords, tokens, or PII.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CoreSecretAttribute : Attribute
    {
    }
}