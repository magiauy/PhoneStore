using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class AccountRepository (DataSource dataSource) : IAccountRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Account GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Account> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Account entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Account entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Account GetByUsername(string username)
        {
            throw new NotImplementedException();
        }

        public async Task<Account?> GetByIdAsync(int id)
        {
            // TODO: Implement actual database query
            return await Task.FromResult<Account?>(null);
        }

        public async Task<Account?> GetByUsernameAsync(string username)
        {
            // TODO: Implement actual database query
            return await Task.FromResult<Account?>(null);
        }

        public async Task<Account?> GetWithRolesAsync(int accountId)
        {
            // TODO: Implement actual database query with roles
            return await Task.FromResult<Account?>(null);
        }

        public async Task<List<Role>?> GetRolesByAccountIdAsync(int accountId)
        {
            // TODO: Implement actual database query for roles
            return await Task.FromResult<List<Role>?>(null);
        }

        public async Task<List<Permission>?> GetPermissionsByAccountIdAsync(int accountId)
        {
            // TODO: Implement actual database query for permissions
            return await Task.FromResult<List<Permission>?>(null);
        }

        public async Task<bool> IsActiveAsync(int accountId)
        {
            // TODO: Implement actual database query
            return await Task.FromResult(false);
        }

        public async Task<bool> UpdateLastLoginAsync(int accountId)
        {
            // TODO: Implement actual database update
            return await Task.FromResult(false);
        }

        public async Task<Account?> AddAsync(Account entity)
        {
            // TODO: Implement actual database insert
            return await Task.FromResult<Account?>(null);
        }

        public async Task UpdateAsync(Account entity)
        {
            // TODO: Implement actual database update
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            // TODO: Implement actual database delete
            await Task.CompletedTask;
        }
    }
}
