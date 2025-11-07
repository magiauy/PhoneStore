using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Utils;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class PromotionCodeRepository : IPromotionCodeRepository
    {
        private readonly DataSource _dataSource;

        public PromotionCodeRepository(DataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        #region Sync Methods

        public virtual PromotionCode? GetById(int id)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id, code, description, discount_amount, discount_percent, start_date, end_date, usage_limit, used_count, is_active 
                                    FROM promotion_codes
                                    WHERE id = @id LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapPromotionCode(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get promotion code by ID {id}", ex);
            }

            return null;
        }

        public virtual PromotionCode? GetByCode(string code)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id, code, description, discount_amount, discount_percent, start_date, end_date, usage_limit, used_count, is_active 
                                    FROM promotion_codes
                                    WHERE code = @code LIMIT 1;";
                cmd.Parameters.AddWithValue("@code", code);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapPromotionCode(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get promotion code by code: {code}", ex);
            }

            return null;
        }

        public virtual IEnumerable<PromotionCode> GetAll()
        {
            var list = new List<PromotionCode>();
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id, promotion_id,code, discount_amount,minimum_amount,usage_limit,used_count,is_active 
                                    FROM promotion_codes;";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapPromotionCode(reader));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all promotion codes", ex);
            }

            return list;
        }

        public virtual void Insert(PromotionCode entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            using var conn = _dataSource.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO promotion_codes 
                    (promotion_id, code, discount_amount, minimum_amount, usage_limit, used_count, is_active)
                VALUES 
                    (@promotionId, @code, @discountAmount, @minimumAmount, @usageLimit, @usedCount, @isActive);
            ";

            cmd.Parameters.AddWithValue("@promotionId", entity.PromotionId);
            cmd.Parameters.AddWithValue("@code", entity.Code);
            cmd.Parameters.AddWithValue("@discountAmount", entity.DiscountAmount);
            cmd.Parameters.AddWithValue("@minimumAmount", entity.MinimumAmount);
            cmd.Parameters.AddWithValue("@usageLimit", entity.UsageLimit);
            cmd.Parameters.AddWithValue("@usedCount", entity.UsedCount);
            cmd.Parameters.AddWithValue("@isActive", entity.IsActive);

            cmd.ExecuteNonQuery();
        }

        public virtual void Update(PromotionCode entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            using var conn = _dataSource.GetConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                UPDATE promotion_codes 
                SET 
                    promotion_id = @promotionId,
                    code = @code,
                    discount_amount = @discountAmount,
                    minimum_amount = @minimumAmount,
                    usage_limit = @usageLimit,
                    used_count = @usedCount,
                    is_active = @isActive
                WHERE id = @id;
            ";

            cmd.Parameters.AddWithValue("@id", entity.Id);
            cmd.Parameters.AddWithValue("@promotionId", entity.PromotionId);
            cmd.Parameters.AddWithValue("@code", entity.Code);
            cmd.Parameters.AddWithValue("@discountAmount", entity.DiscountAmount);
            cmd.Parameters.AddWithValue("@minimumAmount", entity.MinimumAmount);
            cmd.Parameters.AddWithValue("@usageLimit", entity.UsageLimit);
            cmd.Parameters.AddWithValue("@usedCount", entity.UsedCount);
            cmd.Parameters.AddWithValue("@isActive", entity.IsActive);

            cmd.ExecuteNonQuery();
        }


        public virtual void Delete(int id)
        {
            using var conn = _dataSource.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM promotion_codes WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public virtual bool IsValid(string code)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT COUNT(1)
                                    FROM promotion_codes
                                    WHERE code = @code 
                                      AND is_active = 1
                                      AND NOW() BETWEEN start_date AND end_date
                                      AND used_count < usage_limit;";

                cmd.Parameters.AddWithValue("@code", code);
                var result = Convert.ToInt32(cmd.ExecuteScalar());
                return result > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating promotion code: {code}", ex);
                return false;
            }
        }

        public virtual bool IncrementUsedCount(int id)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE promotion_codes
                                    SET used_count = used_count + 1
                                    WHERE id = @id;";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to increment used count for ID: {id}", ex);
                return false;
            }
        }

        #endregion

        #region Helpers

        private PromotionCode MapPromotionCode(MySqlDataReader reader)
        {
            return new PromotionCode
            {
                Id = reader.GetInt32("id"),
                PromotionId = reader.GetInt32("promotion_id"),
                Code = reader.GetString("code"),
                DiscountAmount = reader.IsDBNull("discount_amount") ? 0 : reader.GetDecimal("discount_amount"),
                MinimumAmount = reader.IsDBNull("minimum_amount") ? 0 : reader.GetDecimal("minimum_amount"),
                UsageLimit = reader.IsDBNull("usage_limit") ? 0 : reader.GetInt32("usage_limit"),
                UsedCount = reader.IsDBNull("used_count") ? 0 : reader.GetInt32("used_count"),
                IsActive = reader.GetBoolean("is_active")
            };
        }


        #endregion

        #region Async Methods

        public async Task<PromotionCode?> GetByIdAsync(int id)
        {
            return await Task.FromResult(GetById(id));
        }

        public async Task<PromotionCode?> GetByCodeAsync(string code)
        {
            return await Task.FromResult(GetByCode(code));
        }

        public async Task<IEnumerable<PromotionCode>> GetAllAsync()
        {
            return await Task.FromResult(GetAll());
        }

        public async Task<PromotionCode?> AddAsync(PromotionCode entity)
        {
            Insert(entity);
            return await Task.FromResult(entity);
        }

        public async Task UpdateAsync(PromotionCode entity)
        {
            Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            Delete(id);
            await Task.CompletedTask;
        }

        public async Task<bool> IsValidAsync(string code)
        {
            return await Task.FromResult(IsValid(code));
        }

        public async Task<bool> IncrementUsedCountAsync(int id)
        {
            return await Task.FromResult(IncrementUsedCount(id));
        }

        #endregion
    }
}
