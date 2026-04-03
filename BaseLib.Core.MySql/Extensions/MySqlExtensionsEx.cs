

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
        
        public static void SetWaitTimeout(this MySqlConnection connection, int timeout)
        {
            using( var command = new MySqlCommand($"SET session wait_timeout={timeout};", connection))
            {
                command.ExecuteNonQuery();
            }
        }

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

        public static bool IsTransient(this MySqlException ex)
        {
            var message = ex.Message;
            bool isTransient =
                transientMessages.Any(s => message.Contains(s, StringComparison.InvariantCultureIgnoreCase));
            return isTransient;
        }

        public static bool IsDuplicate(this MySqlException ex)
        {
            return ex.Message.Contains("duplicate");
        }
    }
}