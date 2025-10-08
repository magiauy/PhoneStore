using MySqlConnector;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class BrandRepository(DataSource dataSource) : IBrandRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Brand GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM brands WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Brands with ID {id} not found.");
        }

        public IEnumerable<Brand> GetAll()
        {
            var brands = new List<Brand>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM brands", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                brands.Add(MapFromReader(reader));
            }
            return brands;
        }

        public void Insert(Brand entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Brand entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Brand GetByName(string name)
        {
            throw new NotImplementedException();
        }

        #region Private Helper

        private static Brand MapFromReader(MySqlDataReader reader)
        {
            return new Brand
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
            };
        }

        #endregion
    }
}
