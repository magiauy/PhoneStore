using System;
using System.Security.Cryptography;
using System.Text;

namespace PhoneStoreAdmin.Utils
{
    /// <summary>
    /// Utility class for password hashing and verification using SHA256
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Hash a plain text password using SHA256
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Base64 encoded hash string</returns>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password), "Password cannot be null or empty");

            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// Verify a plain text password against a hash
        /// </summary>
        /// <param name="password">Plain text password to verify</param>
        /// <param name="hash">Hash to compare against</param>
        /// <returns>True if password matches hash, false otherwise</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            var computedHash = HashPassword(password);
            return computedHash.Equals(hash, StringComparison.Ordinal);
        }
    }
}
