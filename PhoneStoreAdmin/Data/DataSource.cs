using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;

namespace PhoneStoreAdmin.Data
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
            Console.WriteLine(_connectionString);
        }

        public MySqlConnection GetConnection()
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
