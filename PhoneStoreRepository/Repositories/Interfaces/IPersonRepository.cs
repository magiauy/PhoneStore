using PhoneStoreRepository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IPersonRepository : IRepository<Person>
    {
        Person GetByEmail(string email);
        Person GetByPhone(string phone);

        /// <summary>
        /// Get person by account ID async
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Person if found, null otherwise</returns>
        Task<Person?> GetByAccountIdAsync(int accountId);

        /// <summary>
        /// Get person by email async
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Person if found, null otherwise</returns>
        Task<Person?> GetByEmailAsync(string email);

        /// <summary>
        /// Get person by phone async
        /// </summary>
        /// <param name="phone">Phone number</param>
        /// <returns>Person if found, null otherwise</returns>
        Task<Person?> GetByPhoneAsync(string phone);

        /// <summary>
        /// Get person by ID async
        /// </summary>
        /// <param name="id">Person ID</param>
        /// <returns>Person if found, null otherwise</returns>
        Task<Person?> GetByIdAsync(int id);

        /// <summary>
        /// Add person async
        /// </summary>
        /// <param name="entity">Person to add</param>
        /// <returns>Added person if successful, null otherwise</returns>
        
        Task<Person?> AddAsync(Person entity);

        /// <summary>
        /// Update person async
        /// </summary>
        /// <param name="entity">Person to update</param>
        Task UpdateAsync(Person entity);

        /// <summary>
        /// Delete person async
        /// </summary>
        /// <param name="id">Person ID to delete</param>
        Task DeleteAsync(int id);
    }
}
