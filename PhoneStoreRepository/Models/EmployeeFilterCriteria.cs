using System;
using System.Collections.Generic;

namespace PhoneStoreRepository.Models
{
    /// <summary>
    /// Filter criteria for employee queries.
    /// </summary>
    public class EmployeeFilterCriteria
    {
        /// <summary>
        /// Employee status filter. Valid values: "All", "Active", "Inactive".
        /// </summary>
        public string Status { get; set; } = "All";

        /// <summary>
        /// Filter employees hired from this date (inclusive).
        /// </summary>
        public DateTime? HireDateFrom { get; set; }

        /// <summary>
        /// Filter employees hired up to this date (inclusive).
        /// </summary>
        public DateTime? HireDateTo { get; set; }

        /// <summary>
        /// Filter employees that have an email address.
        /// </summary>
        public bool? HasEmail { get; set; }

        /// <summary>
        /// Filter employees that have a phone number.
        /// </summary>
        public bool? HasPhone { get; set; }

        /// <summary>
        /// Determines whether any filter is currently active.
        /// </summary>
        public bool HasAnyFilter()
        {
            return Status != "All"
                || HireDateFrom.HasValue
                || HireDateTo.HasValue
                || HasEmail.HasValue
                || HasPhone.HasValue;
        }

        /// <summary>
        /// Generates a cache key representation for the current filter.
        /// </summary>
        public string GetCacheKey()
        {
            var parts = new List<string>
            {
                Status,
                HireDateFrom?.ToString("yyyyMMdd") ?? string.Empty,
                HireDateTo?.ToString("yyyyMMdd") ?? string.Empty,
                HasEmail.HasValue ? (HasEmail.Value ? "1" : "0") : string.Empty,
                HasPhone.HasValue ? (HasPhone.Value ? "1" : "0") : string.Empty
            };

            return string.Join("_", parts).ToLowerInvariant();
        }

        /// <summary>
        /// Creates a copy of the current filter criteria.
        /// </summary>
        public EmployeeFilterCriteria Clone()
        {
            return new EmployeeFilterCriteria
            {
                Status = Status,
                HireDateFrom = HireDateFrom,
                HireDateTo = HireDateTo,
                HasEmail = HasEmail,
                HasPhone = HasPhone
            };
        }

        /// <summary>
        /// Generates a user-friendly summary string of active filters.
        /// </summary>
        public string ToSummaryString()
        {
            if (!HasAnyFilter())
            {
                return "Không áp dụng bộ lọc";
            }

            var parts = new List<string>();

            if (Status == "Active")
            {
                parts.Add("Đang làm việc");
            }
            else if (Status == "Inactive")
            {
                parts.Add("Đã nghỉ việc");
            }

            if (HireDateFrom.HasValue || HireDateTo.HasValue)
            {
                var fromText = HireDateFrom?.ToString("dd/MM/yyyy") ?? "...";
                var toText = HireDateTo?.ToString("dd/MM/yyyy") ?? "...";
                parts.Add($"Ngày vào làm: {fromText} - {toText}");
            }

            if (HasEmail.HasValue)
            {
                parts.Add(HasEmail.Value ? "Có email" : "Không có email");
            }

            if (HasPhone.HasValue)
            {
                parts.Add(HasPhone.Value ? "Có số điện thoại" : "Không có số điện thoại");
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Compares cache keys with another criteria instance.
        /// </summary>
        public bool HasSameState(EmployeeFilterCriteria other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(GetCacheKey(), other.GetCacheKey(), StringComparison.Ordinal);
        }
    }
}
