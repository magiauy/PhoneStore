using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using MySqlConnector;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class PromotionCodeRepository : IPromotionCodeRepository
    {
        private readonly DataSource _dataSource;

        public PromotionCodeRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        #region Basic CRUD

        public PromotionCode GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM promotion_codes WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"PromotionCode with ID {id} not found.");
        }

        public IEnumerable<PromotionCode> GetAll()
        {
            var promotionCodes = new List<PromotionCode>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM promotion_codes", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                promotionCodes.Add(MapFromReader(reader));
            }
            return promotionCodes;
        }

        public void Insert(PromotionCode entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO promotion_codes (promotion_id, code, discount_amount, minimum_amount, usage_limit, used_count, is_active) 
                  VALUES (@promotionId, @code, @discountAmount, @minimumAmount, @usageLimit, @usedCount, @isActive)",
                connection);

            command.Parameters.AddWithValue("@promotionId", entity.PromotionId);
            command.Parameters.AddWithValue("@code", entity.Code);
            command.Parameters.AddWithValue("@discountAmount", entity.DiscountAmount);
            command.Parameters.AddWithValue("@minimumAmount", entity.MinimumAmount);
            command.Parameters.AddWithValue("@usageLimit", entity.UsageLimit ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@usedCount", entity.UsedCount);
            command.Parameters.AddWithValue("@isActive", entity.IsActive);

            command.ExecuteNonQuery();
        }

        public void Update(PromotionCode entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE promotion_codes 
                  SET promotion_id = @promotionId, code = @code, discount_amount = @discountAmount, 
                      minimum_amount = @minimumAmount, usage_limit = @usageLimit, used_count = @usedCount, is_active = @isActive 
                  WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@promotionId", entity.PromotionId);
            command.Parameters.AddWithValue("@code", entity.Code);
            command.Parameters.AddWithValue("@discountAmount", entity.DiscountAmount);
            command.Parameters.AddWithValue("@minimumAmount", entity.MinimumAmount);
            command.Parameters.AddWithValue("@usageLimit", entity.UsageLimit ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@usedCount", entity.UsedCount);
            command.Parameters.AddWithValue("@isActive", entity.IsActive);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"PromotionCode with ID {entity.Id} not found for update.");
            }
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM promotion_codes WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"PromotionCode with ID {id} not found for deletion.");
            }
        }

        #endregion

        #region Filtered Search

        private (string whereClause, List<MySqlParameter> parameters) BuildConditions(
            string? code,
            int? promotionId,
            bool? isActive)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(code))
            {
                conditions.Add("code LIKE @code");
                parameters.Add(new MySqlParameter("@code", $"%{code}%"));
            }

            if (promotionId.HasValue)
            {
                conditions.Add("promotion_id = @promotionId");
                parameters.Add(new MySqlParameter("@promotionId", promotionId.Value));
            }

            if (isActive.HasValue)
            {
                conditions.Add("is_active = @isActive");
                parameters.Add(new MySqlParameter("@isActive", isActive.Value));
            }

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            return (whereClause, parameters);
        }

        public int GetTotalRecords(
            string? code,
            int? promotionId,
            bool? isActive)
        {
            var (whereClause, parameters) = BuildConditions(code, promotionId, isActive);
            var sql = $"SELECT COUNT(id) FROM promotion_codes {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            foreach (var param in parameters)
                command.Parameters.Add(param);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public IEnumerable<PromotionCode> GetPromotionCodesFiltered(
            string? code,
            int? promotionId,
            bool? isActive,
            int page = 1,
            int pageSize = 20)
        {
            var promotionCodes = new List<PromotionCode>();
            var (whereClause, parameters) = BuildConditions(code, promotionId, isActive);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT * FROM promotion_codes {whereClause} ORDER BY id LIMIT @pageSize OFFSET @offset";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var param in parameters)
                command.Parameters.Add(param);

            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                promotionCodes.Add(MapFromReader(reader));
            }

            return promotionCodes;
        }

        public int GetTotalPages(
            string? code,
            int? promotionId,
            bool? isActive,
            int pageSize)
        {
            var totalRecords = GetTotalRecords(code, promotionId, isActive);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        #endregion

        #region Private Helper

        private static PromotionCode MapFromReader(MySqlDataReader reader)
        {
            return new PromotionCode
            {
                Id = reader.GetInt32("id"),
                PromotionId = reader.GetInt32("promotion_id"),
                Code = reader.GetString("code"),
                DiscountAmount = reader.GetDecimal("discount_amount"),
                MinimumAmount = reader.GetDecimal("minimum_amount"),
                UsageLimit = reader.IsDBNull(reader.GetOrdinal("usage_limit")) ? null : reader.GetInt32("usage_limit"),
                UsedCount = reader.GetInt32("used_count"),
                IsActive = reader.GetBoolean("is_active")
            };
        }

        private (string whereClause, List<MySqlParameter> parameters) BuildAdvancedConditions(
            string? code,
            int? promotionId,
            string? promotionName,
            bool? isActive,
            decimal? minDiscountAmount,
            decimal? maxDiscountAmount,
            decimal? minMinimumAmount,
            decimal? maxMinimumAmount,
            int? minUsageLimit,
            int? maxUsageLimit)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(code))
            {
                conditions.Add("pc.code LIKE @code");
                parameters.Add(new MySqlParameter("@code", $"%{code}%"));
            }

            if (promotionId.HasValue)
            {
                conditions.Add("pc.promotion_id = @promotionId");
                parameters.Add(new MySqlParameter("@promotionId", promotionId.Value));
            }

            if (!string.IsNullOrWhiteSpace(promotionName))
            {
                conditions.Add("p.name LIKE @promotionName");
                parameters.Add(new MySqlParameter("@promotionName", $"%{promotionName}%"));
            }

            if (isActive.HasValue)
            {
                conditions.Add("pc.is_active = @isActive");
                parameters.Add(new MySqlParameter("@isActive", isActive.Value));
            }

            if (minDiscountAmount.HasValue)
            {
                conditions.Add("pc.discount_amount >= @minDiscountAmount");
                parameters.Add(new MySqlParameter("@minDiscountAmount", minDiscountAmount.Value));
            }

            if (maxDiscountAmount.HasValue)
            {
                conditions.Add("pc.discount_amount <= @maxDiscountAmount");
                parameters.Add(new MySqlParameter("@maxDiscountAmount", maxDiscountAmount.Value));
            }

            if (minMinimumAmount.HasValue)
            {
                conditions.Add("pc.minimum_amount >= @minMinimumAmount");
                parameters.Add(new MySqlParameter("@minMinimumAmount", minMinimumAmount.Value));
            }

            if (maxMinimumAmount.HasValue)
            {
                conditions.Add("pc.minimum_amount <= @maxMinimumAmount");
                parameters.Add(new MySqlParameter("@maxMinimumAmount", maxMinimumAmount.Value));
            }

            if (minUsageLimit.HasValue)
            {
                conditions.Add("(pc.usage_limit IS NULL OR pc.usage_limit >= @minUsageLimit)");
                parameters.Add(new MySqlParameter("@minUsageLimit", minUsageLimit.Value));
            }

            if (maxUsageLimit.HasValue)
            {
                conditions.Add("(pc.usage_limit IS NOT NULL AND pc.usage_limit <= @maxUsageLimit)");
                parameters.Add(new MySqlParameter("@maxUsageLimit", maxUsageLimit.Value));
            }

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            return (whereClause, parameters);
        }

        public IEnumerable<PromotionCode> GetPromotionCodesWithAdvancedFilter(
            string? code,
            int? promotionId,
            string? promotionName,
            bool? isActive,
            decimal? minDiscountAmount,
            decimal? maxDiscountAmount,
            decimal? minMinimumAmount,
            decimal? maxMinimumAmount,
            int? minUsageLimit,
            int? maxUsageLimit,
            int page = 1,
            int pageSize = 20)
        {
            var promotionCodes = new List<PromotionCode>();
            var (whereClause, parameters) = BuildAdvancedConditions(
                code, promotionId, promotionName, isActive,
                minDiscountAmount, maxDiscountAmount,
                minMinimumAmount, maxMinimumAmount,
                minUsageLimit, maxUsageLimit);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT pc.* FROM promotion_codes pc
                        LEFT JOIN promotions p ON pc.promotion_id = p.id 
                        {whereClause} 
                        ORDER BY pc.id 
                        LIMIT @pageSize OFFSET @offset";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var param in parameters)
                command.Parameters.Add(param);

            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                promotionCodes.Add(MapFromReader(reader));
            }

            return promotionCodes;
        }

        public int GetAdvancedFilterTotalRecords(
            string? code,
            int? promotionId,
            string? promotionName,
            bool? isActive,
            decimal? minDiscountAmount,
            decimal? maxDiscountAmount,
            decimal? minMinimumAmount,
            decimal? maxMinimumAmount,
            int? minUsageLimit,
            int? maxUsageLimit)
        {
            var (whereClause, parameters) = BuildAdvancedConditions(
                code, promotionId, promotionName, isActive,
                minDiscountAmount, maxDiscountAmount,
                minMinimumAmount, maxMinimumAmount,
                minUsageLimit, maxUsageLimit);

            var sql = $@"SELECT COUNT(pc.id) FROM promotion_codes pc
                        LEFT JOIN promotions p ON pc.promotion_id = p.id 
                        {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            foreach (var param in parameters)
                command.Parameters.Add(param);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int GetAdvancedFilterTotalPages(
            string? code,
            int? promotionId,
            string? promotionName,
            bool? isActive,
            decimal? minDiscountAmount,
            decimal? maxDiscountAmount,
            decimal? minMinimumAmount,
            decimal? maxMinimumAmount,
            int? minUsageLimit,
            int? maxUsageLimit,
            int pageSize)
        {
            var totalRecords = GetAdvancedFilterTotalRecords(
                code, promotionId, promotionName, isActive,
                minDiscountAmount, maxDiscountAmount,
                minMinimumAmount, maxMinimumAmount,
                minUsageLimit, maxUsageLimit);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        #endregion
    }
}