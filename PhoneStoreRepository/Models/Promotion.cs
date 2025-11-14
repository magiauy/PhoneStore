using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreRepository.Models
{
    public class Promotion
    {
        // Private backing fields
        private int _id;
        private string _name = string.Empty;
        private string? _description;
        private DateTime _startDate;
        private DateTime _endDate;
        private bool _isActive = true;
        private ICollection<PromotionCode> _promotionCodes = new List<PromotionCode>();

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(120)]
        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        [MaxLength(255)]
        public string? Description
        {
            get => _description;
            set => _description = value;
        }

        [Required]
        public DateTime StartDate
        {
            get => _startDate;
            set => _startDate = value;
        }

        [Required]
        public DateTime EndDate
        {
            get => _endDate;
            set => _endDate = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        // Navigation property
        public ICollection<PromotionCode> PromotionCodes
        {
            get => _promotionCodes;
            set => _promotionCodes = value ?? new List<PromotionCode>();
        }

        // Constructors
        public Promotion()
        {
            _id = 0;
            _name = string.Empty;
            _description = null;
            _startDate = DateTime.Now;
            _endDate = DateTime.Now.AddDays(30);
            _isActive = true;
            _promotionCodes = new List<PromotionCode>();
        }

        public Promotion(string name, DateTime startDate, DateTime endDate)
        {
            _id = 0;
            _name = name ?? string.Empty;
            _description = null;
            _startDate = startDate;
            _endDate = endDate;
            _isActive = true;
            _promotionCodes = new List<PromotionCode>();
        }
    }
}
