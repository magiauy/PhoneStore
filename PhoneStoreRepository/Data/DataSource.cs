using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace PhoneStoreRepository.Data
{
    public class DataSource
    {
        private readonly string _connectionString;

        public DataSource(IConfiguration configuration)
        {
            var dbConfig = configuration.GetSection("Database");
            var host = dbConfig["Host"];
            var port = dbConfig["Port"];
            var database = dbConfig["Database"];
            var user = dbConfig["User"];
            var password = dbConfig["Password"];

            _connectionString = $"Server={host};Port={port};Database={database};User ID={user};Password={password};SslMode=Preferred;";
        }

        public MySqlConnection GetConnection()
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Get a new connection with a transaction started
        /// </summary>
        public (MySqlConnection connection, MySqlTransaction transaction) BeginTransaction()
        {
            var connection = GetConnection();
            var transaction = connection.BeginTransaction();
            return (connection, transaction);
        }
    }
}
