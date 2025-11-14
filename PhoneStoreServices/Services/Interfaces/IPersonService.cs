using PhoneStoreRepository.Models;
using System.Threading.Tasks;

namespace PhoneStore.Services.Interfaces
{
    public interface IPersonService
    {
        /// <summary>
        /// Get person by ID
        /// </summary>
        /// <param name="personId">Person ID</param>
        /// <returns>Person if found, null otherwise</returns>
        Task<Person?> GetPersonByIdAsync(int personId);

        /// <summary>
        /// Get person by account ID
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Person if found, null otherwise</returns>
        Task<Person?> GetPersonByAccountIdAsync(int accountId);

        /// <summary>
        /// Get person by email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Person if found, null otherwise</returns>
        Task<Person?> GetPersonByEmailAsync(string email);

        /// <summary>
        /// Get person by phone number
        /// </summary>
        /// <param name="phone">Phone number</param>
        /// <returns>Person if found, null otherwise</returns>
        Task<Person?> GetPersonByPhoneAsync(string phone);

        /// <summary>
        /// Create a new person
        /// </summary>
        /// <param name="person">Person to create</param>
        /// <returns>Created person if successful, null otherwise</returns>
        Task<Person?> CreatePersonAsync(Person person);

        /// <summary>
        /// Update person information
        /// </summary>
        /// <param name="person">Person to update</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdatePersonAsync(Person person);
    }
}