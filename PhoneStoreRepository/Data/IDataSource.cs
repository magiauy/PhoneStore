using MySqlConnector;

namespace PhoneStoreRepository.Data
{
    /// <summary>
    /// Interface for database connection management
    /// </summary>
    public interface IDataSource
    {
        /// <summary>
        /// Get a new database connection
        /// </summary>
        MySqlConnection GetConnection();

        /// <summary>
        /// Get a new connection with a transaction started
        /// </summary>
        (MySqlConnection connection, MySqlTransaction transaction) BeginTransaction();
    }
}
