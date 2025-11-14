using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class PurchaseOrderLineRepository : IPurchaseOrderLineRepository
    {
        private readonly DataSource _dataSource;

        public PurchaseOrderLineRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public IEnumerable<PurchaseOrderLine> GetAll()
        {
            var result = new List<PurchaseOrderLine>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM purchase_order_lines", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public PurchaseOrderLine GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM purchase_order_lines WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"PurchaseOrderLine with ID {id} not found.");
        }

        public ICollection<PurchaseOrderLine> GetByPurchaseOrderId(int purchaseOrderId)
        {
            var result = new List<PurchaseOrderLine>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM purchase_order_lines WHERE purchase_order_id = @purchaseOrderId", connection);
            command.Parameters.AddWithValue("@purchaseOrderId", purchaseOrderId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public void Insert(PurchaseOrderLine entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO purchase_order_lines (purchase_order_id, product_id, quantity, unit_cost, total_cost) 
                  VALUES (@purchaseOrderId, @productId, @quantity, @unitCost, @totalCost)",
                connection);
            command.Parameters.AddWithValue("@purchaseOrderId", entity.PurchaseOrderId);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@quantity", entity.Quantity);
            command.Parameters.AddWithValue("@unitCost", entity.UnitCost);
            command.Parameters.AddWithValue("@totalCost", entity.TotalCost);
            command.ExecuteNonQuery();
            entity.Id = GetLastInsertedId(connection);
        }

        public void Insert(PurchaseOrderLine entity, MySqlConnection connection, MySqlTransaction transaction)
        {
            using var command = new MySqlCommand(
                @"INSERT INTO purchase_order_lines (purchase_order_id, product_id, quantity, unit_cost, total_cost) 
                  VALUES (@purchaseOrderId, @productId, @quantity, @unitCost, @totalCost)",
                connection, transaction);
            command.Parameters.AddWithValue("@purchaseOrderId", entity.PurchaseOrderId);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@quantity", entity.Quantity);
            command.Parameters.AddWithValue("@unitCost", entity.UnitCost);
            command.Parameters.AddWithValue("@totalCost", entity.TotalCost);
            command.ExecuteNonQuery();

            // Get last inserted ID with transaction
            using var idCommand = new MySqlCommand("SELECT LAST_INSERT_ID()", connection, transaction);
            entity.Id = Convert.ToInt32(idCommand.ExecuteScalar());
        }

        public void Update(PurchaseOrderLine entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE purchase_order_lines 
                  SET purchase_order_id = @purchaseOrderId, product_id = @productId, quantity = @quantity, 
                      unit_cost = @unitCost, total_cost = @totalCost
                  WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@purchaseOrderId", entity.PurchaseOrderId);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@quantity", entity.Quantity);
            command.Parameters.AddWithValue("@unitCost", entity.UnitCost);
            command.Parameters.AddWithValue("@totalCost", entity.TotalCost);
            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"PurchaseOrderLine with ID {entity.Id} not found for update.");
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM purchase_order_lines WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"PurchaseOrderLine with ID {id} not found for deletion.");
        }

        public void DeleteByPurchaseOrderId(int purchaseOrderId)
        {
            using var connection = _dataSource.GetConnection();
            
            // First, delete all product_serials that reference the purchase_order_lines
            using (var deleteSerials = new MySqlCommand(
                @"DELETE ps FROM product_serials ps
                  INNER JOIN purchase_order_lines pol ON ps.purchase_order_line_id = pol.id
                  WHERE pol.purchase_order_id = @purchaseOrderId", 
                connection))
            {
                deleteSerials.Parameters.AddWithValue("@purchaseOrderId", purchaseOrderId);
                deleteSerials.ExecuteNonQuery();
            }
            
            // Then delete the purchase_order_lines
            using (var deleteLines = new MySqlCommand(
                "DELETE FROM purchase_order_lines WHERE purchase_order_id = @purchaseOrderId", 
                connection))
            {
                deleteLines.Parameters.AddWithValue("@purchaseOrderId", purchaseOrderId);
                deleteLines.ExecuteNonQuery();
            }
        }

        public void DeleteByPurchaseOrderId(int purchaseOrderId, MySqlConnection connection, MySqlTransaction transaction)
        {
            // First, delete all product_serials that reference the purchase_order_lines
            using (var deleteSerials = new MySqlCommand(
                @"DELETE ps FROM product_serials ps
INNER JOIN purchase_order_lines pol ON ps.purchase_order_line_id = pol.id
    WHERE pol.purchase_order_id = @purchaseOrderId", 
                connection, transaction))
            {
                deleteSerials.Parameters.AddWithValue("@purchaseOrderId", purchaseOrderId);
                deleteSerials.ExecuteNonQuery();
            }
            
            // Then delete the purchase_order_lines
            using (var deleteLines = new MySqlCommand(
                "DELETE FROM purchase_order_lines WHERE purchase_order_id = @purchaseOrderId", 
                connection, transaction))
            {
                deleteLines.Parameters.AddWithValue("@purchaseOrderId", purchaseOrderId);
                deleteLines.ExecuteNonQuery();
            }
        }

        private int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }
 
        private int GetLastInsertedId(MySqlConnection connection, MySqlTransaction transaction)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection, transaction);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static PurchaseOrderLine MapFromReader(MySqlDataReader reader)
        {
            return new PurchaseOrderLine
            {
                Id = reader.GetInt32("id"),
                PurchaseOrderId = reader.GetInt32("purchase_order_id"),
                ProductId = reader.GetInt32("product_id"),
                Quantity = reader.GetInt32("quantity"),
                UnitCost = reader.GetDecimal("unit_cost"),
                TotalCost = reader.GetDecimal("total_cost")
            };
        }
    }
}