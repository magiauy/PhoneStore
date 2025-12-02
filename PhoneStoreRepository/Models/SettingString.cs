using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStoreRepository.Models
{
    /// <summary>
    /// Model for system settings stored as string key-value pairs
    /// Table: setting_string
    /// </summary>
    [Table("setting_string")]
    public class SettingString
    {
        // Private backing fields
        private int _id;
        private string _code = string.Empty;
        private string _value = string.Empty;
        private string _type = string.Empty;

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(255)]
        public string Code
        {
            get => _code;
            set => _code = value ?? string.Empty;
        }

        [MaxLength(255)]
        public string Value
        {
            get => _value;
            set => _value = value ?? string.Empty;
        }

        [MaxLength(45)]
        public string Type
        {
            get => _type;
            set => _type = value ?? string.Empty;
        }

        // Constructors
        public SettingString()
        {
            _id = 0;
            _code = string.Empty;
            _value = string.Empty;
            _type = string.Empty;
        }

        public SettingString(string code, string value, string type = "string")
        {
            _id = 0;
            _code = code ?? string.Empty;
            _value = value ?? string.Empty;
            _type = type ?? "string";
        }
    }
}
