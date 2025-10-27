using System;

namespace PhoneStoreAdmin.Models
{
    /// <summary>
    /// Filter criteria for account queries
    /// </summary>
    public class AccountFilterCriteria
    {
        /// <summary>
        /// Account status filter: "All", "Activated", "Deactivated"
        /// </summary>
        public string Status { get; set; } = "All";

        /// <summary>
        /// Account type filter: "All", "Employee", "Customer"
        /// </summary>
        public string AccountType { get; set; } = "All";

        /// <summary>
        /// Filter accounts created from this date (inclusive)
        /// </summary>
        public DateTime? CreatedFrom { get; set; }

        /// <summary>
        /// Filter accounts created to this date (inclusive)
        /// </summary>
        public DateTime? CreatedTo { get; set; }

        /// <summary>
        /// Filter accounts with last login from this date (inclusive)
        /// </summary>
        public DateTime? LastLoginFrom { get; set; }

        /// <summary>
        /// Filter accounts with last login to this date (inclusive)
        /// </summary>
        public DateTime? LastLoginTo { get; set; }

        /// <summary>
        /// Filter accounts that have never logged in
        /// </summary>
        public bool NeverLoggedIn { get; set; }

        /// <summary>
        /// Check if any filter is active
        /// </summary>
        /// <returns>True if at least one filter is set</returns>
        public bool HasAnyFilter()
        {
            return Status != "All"
                || AccountType != "All"
                || CreatedFrom.HasValue
                || CreatedTo.HasValue
                || LastLoginFrom.HasValue
                || LastLoginTo.HasValue
                || NeverLoggedIn;
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
                AccountType,
                CreatedFrom?.ToString("yyyyMMdd") ?? "",
                CreatedTo?.ToString("yyyyMMdd") ?? "",
                LastLoginFrom?.ToString("yyyyMMdd") ?? "",
                LastLoginTo?.ToString("yyyyMMdd") ?? "",
                NeverLoggedIn ? "1" : "0"
            };
            return string.Join("_", parts);
        }
    }
}
