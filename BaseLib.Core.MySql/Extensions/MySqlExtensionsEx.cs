

namespace MySql.Data.MySqlClient
{
    /// <summary>Extension methods for <see cref="MySqlConnection"/> and <see cref="MySqlException"/>.</summary>
    public static class MySqlExtensionsEx
    {
        private readonly static string[] transientMessages = new string[]{
            "Reading from the stream",
            "Fatal error",
            "Timeout expired",
            "Unable to connect",
            "Deadlock found"
        };
        
        /// <summary>Sets the MySQL session <c>wait_timeout</c> variable for the given connection.</summary>
        /// <param name="connection">An open <see cref="MySqlConnection"/>.</param>
        /// <param name="timeout">Timeout value in seconds.</param>
        public static void SetWaitTimeout(this MySqlConnection connection, int timeout)
        {
            using( var command = new MySqlCommand($"SET session wait_timeout={timeout};", connection))
            {
                command.ExecuteNonQuery();
            }
        }

        /// <summary>Reads the current MySQL session <c>wait_timeout</c> value from the given connection.</summary>
        /// <param name="connection">An open <see cref="MySqlConnection"/>.</param>
        /// <returns>The current wait timeout in seconds, or <c>0</c> if the value cannot be read.</returns>
        public static int GetWaitTimeout(this MySqlConnection connection)
        {
            using( var command = new MySqlCommand("SHOW VARIABLES LIKE 'wait_timeout';", connection))
            using( var reader = command.ExecuteReader())
            {
                if( reader.Read())
                {
                    return reader.GetInt32(1);
                }
                return 0;
            }
        }

        /// <summary>Returns <see langword="true"/> when the exception message indicates a transient MySQL error that may succeed on retry (e.g. connection drops, deadlocks, timeouts).</summary>
        /// <param name="ex">The MySQL exception to inspect.</param>
        public static bool IsTransient(this MySqlException ex)
        {
            var message = ex.Message;
            bool isTransient =
                transientMessages.Any(s => message.Contains(s, StringComparison.InvariantCultureIgnoreCase));
            return isTransient;
        }

        /// <summary>Returns <see langword="true"/> when the exception message indicates a duplicate-key violation.</summary>
        /// <param name="ex">The MySQL exception to inspect.</param>
        public static bool IsDuplicate(this MySqlException ex)
        {
            return ex.Message.Contains("duplicate");
        }
    }
}