using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;

namespace BaseLib.Core.Services.AmazonCloud
{
    /// <summary>
    /// <see cref="ICoreServiceStateStore"/> implementation that persists suspended service state
    /// as JSON objects in an S3 bucket. Each operation is stored as
    /// <c>{folderName}/{operationId}.json</c>. Type information is preserved alongside each value
    /// so the full field graph can be reconstructed on resume.
    /// </summary>
    public class S3CoreServiceStateStore : ICoreServiceStateStore
    {
        private readonly IAmazonS3 s3;
        private readonly string bucketName;
        private readonly string folderName;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>Initializes the state store.</summary>
        /// <param name="s3">S3 client.</param>
        /// <param name="bucketName">Name of the S3 bucket used for state storage.</param>
        /// <param name="folderName">S3 key prefix (folder). Defaults to <c>state</c>.</param>
        public S3CoreServiceStateStore(IAmazonS3 s3, string bucketName, string folderName = "state")
        {
            this.s3 = s3;
            this.bucketName = bucketName;
            this.folderName = folderName;
        }

        public async Task<IDictionary<string, object?>> ReadAsync(string operationId)
        {
            // Fast path for invalid inputs
            var keyName = $"{folderName}/{operationId}.json";

            // Retrieve the object from S3
            var (exists, response) = await this.s3.GetObjectIfExistsAsync(new GetObjectRequest
            {
                BucketName = this.bucketName,
                Key = keyName
            });

            // Throws if not exists or response is null
            if (!exists || response == null)
            {
                throw new InvalidOperationException("Failed to retrieve state from store");
            }

            // Deserialize the response with typed value
            var typedState = JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(
                response.ResponseStream,
                jsonOptions
            ) ?? throw new InvalidOperationException("Failed to deserialize state from store");


            // Convert all typed values back to their original types
            return typedState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.GetValue()
            );
        }

        public async Task WriteAsync(string operationId, IDictionary<string, object?> state)
        {
            // Create a dictionary with typed values
            var typedState = new Dictionary<string, TypedValue>(state.Count);

            // Store each value with its type information
            foreach (var kvp in state)
            {
                typedState[kvp.Key] = new TypedValue(kvp.Value);
            }

            // Serialize the entire typed state dictionary to a single JSON string
            string serializedState = JsonSerializer.Serialize(typedState, jsonOptions);

            // Store the serialized state in S3
            var keyName = $"{folderName}/{operationId}.json";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedState)))
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    InputStream = stream,
                    ContentType = "application/json"
                };

                await s3.PutObjectAsync(putRequest);
            }
        }

        /// <summary>
        /// Represents a value with its type information to ensure proper deserialization
        /// </summary>
        private struct TypedValue
        {
            // Public properties for serialization
            public string? TypeName { get; set; }
            public string? ValueJson { get; set; }

            // Constructor to create from an object
            public TypedValue(object? value)
            {
                if (value == null)
                {
                    TypeName = null;
                    ValueJson = null;
                    return;
                }

                // Store the type's assembly qualified name for precise reconstruction
                TypeName = value.GetType().AssemblyQualifiedName;

                // Serialize the value using the shared options
                ValueJson = JsonSerializer.Serialize(value, jsonOptions);
            }

            public readonly object? GetValue()
            {
                // Fast path for null values
                if (TypeName == null || ValueJson == null)
                {
                    return null;
                }

                try
                {
                    // Get the type from the stored type name
                    var type = Type.GetType(TypeName);
                    if (type == null)
                    {
                        return null;
                    }

                    // Deserialize using the original type
                    return JsonSerializer.Deserialize(ValueJson, type, jsonOptions);
                }
                catch (Exception)
                {
                    // If deserialization fails, return null
                    return null;
                }
            }
        }

    }
}