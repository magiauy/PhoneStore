using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Repositories.Interfaces;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class ProductModelAttributeRepository : IProductModelAttributeRepository
    {
        private readonly DataSource _dataSource;

        public ProductModelAttributeRepository(DataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        public IEnumerable<int> GetAttributeIdsByModel(int modelId)
        {
            var attributeIds = new List<int>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                "SELECT attribute_id FROM product_model_attributes WHERE model_id = @modelId ORDER BY attribute_id",
                connection);
            command.Parameters.AddWithValue("@modelId", modelId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                attributeIds.Add(reader.GetInt32("attribute_id"));
            }

            return attributeIds;
        }

        public void ReplaceAttributesForModel(int modelId, IEnumerable<int> attributeIds)
        {
            using var connection = _dataSource.GetConnection();
            using var transaction = connection.BeginTransaction();

            using (var deleteCommand = new MySqlCommand(
                       "DELETE FROM product_model_attributes WHERE model_id = @modelId",
                       connection, transaction))
            {
                deleteCommand.Parameters.AddWithValue("@modelId", modelId);
                deleteCommand.ExecuteNonQuery();
            }

            var distinctAttributeIds = attributeIds?.Distinct().ToList();
            if (distinctAttributeIds != null && distinctAttributeIds.Count > 0)
            {
                using var insertCommand = new MySqlCommand(
                    "INSERT INTO product_model_attributes (model_id, attribute_id) VALUES (@modelId, @attributeId)",
                    connection, transaction);
                insertCommand.Parameters.Add("@modelId", MySqlDbType.Int32).Value = modelId;
                insertCommand.Parameters.Add("@attributeId", MySqlDbType.Int32);

                foreach (var attributeId in distinctAttributeIds)
                {
                    insertCommand.Parameters["@attributeId"].Value = attributeId;
                    insertCommand.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }
    }
}
