using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreRepository.Utils;
using System;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class PersonService : IPersonService
    {
        private readonly IPersonRepository _personRepository;

        public PersonService(IPersonRepository personRepository)
        {
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
        }

        public async Task<Person?> GetPersonByIdAsync(int personId)
        {
            try
            {
                Logger.Info($"Getting person by ID: {personId}");
                return await _personRepository.GetByIdAsync(personId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get person by ID: {personId}", ex);
                return null;
            }
        }

        public async Task<Person?> GetPersonByAccountIdAsync(int accountId)
        {
            try
            {
                Logger.Info($"Getting person by account ID: {accountId}");
                return await _personRepository.GetByAccountIdAsync(accountId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get person by account ID: {accountId}", ex);
                return null;
            }
        }

        public async Task<Person?> GetPersonByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return null;

                Logger.Info($"Getting person by email: {email}");
                return await _personRepository.GetByEmailAsync(email);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get person by email: {email}", ex);
                return null;
            }
        }

        public async Task<Person?> GetPersonByPhoneAsync(string phone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return null;

                Logger.Info($"Getting person by phone: {phone}");
                return await _personRepository.GetByPhoneAsync(phone);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get person by phone: {phone}", ex);
                return null;
            }
        }

        public async Task<Person?> CreatePersonAsync(Person person)
        {
            try
            {
                if (person == null)
                    return null;

                Logger.Info($"Creating person: {person.FullName}");
                return await _personRepository.AddAsync(person);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create person: {person?.FullName}", ex);
                return null;
            }
        }

        public async Task<bool> UpdatePersonAsync(Person person)
        {
            try
            {
                if (person == null)
                    return false;

                Logger.Info($"Updating person: {person.FullName}");
                await _personRepository.UpdateAsync(person);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update person: {person?.FullName}", ex);
                return false;
            }
        }
    }
}