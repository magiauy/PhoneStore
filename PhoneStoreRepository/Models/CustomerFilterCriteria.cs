using System;

namespace PhoneStoreRepository.Models
{
    /// <summary>
    /// Filter criteria for customer queries
    /// </summary>
    public class CustomerFilterCriteria
    {
        /// <summary>
        /// Customer status filter: "All", "Active", "Inactive"
        /// </summary>
        public string Status { get; set; } = "All";

        /// <summary>
        /// Filter customers created from this date (inclusive)
        /// </summary>
        public DateTime? CreatedFrom { get; set; }

        /// <summary>
        /// Filter customers created to this date (inclusive)
        /// </summary>
        public DateTime? CreatedTo { get; set; }

        /// <summary>
        /// Filter by specific phone prefix (e.g., "09", "03")
        /// </summary>
        public string? PhonePrefix { get; set; }

        /// <summary>
        /// Filter by city/province in address
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Filter customers with email
        /// </summary>
        public bool? HasEmail { get; set; }

        /// <summary>
        /// Filter customers with address
        /// </summary>
        public bool? HasAddress { get; set; }

        /// <summary>
        /// Check if any filter is active
        /// </summary>
        /// <returns>True if at least one filter is set</returns>
        public bool HasAnyFilter()
        {
            return Status != "All"
                || CreatedFrom.HasValue
                || CreatedTo.HasValue
                || !string.IsNullOrWhiteSpace(PhonePrefix)
                || !string.IsNullOrWhiteSpace(City)
                || HasEmail.HasValue
                || HasAddress.HasValue;
        }

        /// <summary>
        /// Generate a hash code for caching purposes
        /// </summary>
        /// <returns>Hash string representing current filter state</returns>
        public string GetCacheKey()
        {
            var parts = new[]
            {
                Status,
                CreatedFrom?.ToString("yyyyMMdd") ?? "",
                CreatedTo?.ToString("yyyyMMdd") ?? "",
                PhonePrefix ?? "",
                City ?? "",
                HasEmail.HasValue ? (HasEmail.Value ? "1" : "0") : "",
                HasAddress.HasValue ? (HasAddress.Value ? "1" : "0") : ""
            };
            return string.Join("_", parts);
        }
    }
}
