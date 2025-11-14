using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStore.Services.Implementations
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionService(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            try
            {
                Logger.Info("Getting all permissions");
                var permissions = await _permissionRepository.GetAllAsync();
                return permissions ?? new List<Permission>();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all permissions", ex);
                return new List<Permission>();
            }
        }

        public async Task<Permission?> GetPermissionByIdAsync(int permissionId)
        {
            try
            {
                Logger.Info($"Getting permission by ID: {permissionId}");
                return await _permissionRepository.GetByIdAsync(permissionId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get permission by ID: {permissionId}", ex);
                return null;
            }
        }

        public async Task<Permission?> GetPermissionByCodeAsync(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return null;

                Logger.Info($"Getting permission by code: {code}");
                return await _permissionRepository.GetByCodeAsync(code);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get permission by code: {code}", ex);
                return null;
            }
        }

        public async Task<Permission> CreatePermissionAsync(Permission permission)
        {
            try
            {
                if (permission == null)
                    throw new ArgumentNullException(nameof(permission));

                Logger.Info($"Creating new permission: {permission.Code}");
                var result = await _permissionRepository.AddAsync(permission);

                if (result != null)
                {
                    Logger.Info($"Successfully created permission: {permission.Code} (ID: {result.Id})");
                    return result;
                }
                else
                {
                    Logger.Warning($"Failed to create permission: {permission.Code}");
                    throw new InvalidOperationException($"Failed to create permission: {permission.Code}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create permission: {permission.Code}", ex);
                throw;
            }
        }

        public async Task<bool> UpdatePermissionAsync(Permission permission)
        {
            try
            {
                if (permission == null)
                    throw new ArgumentNullException(nameof(permission));

                Logger.Info($"Updating permission: {permission.Code} (ID: {permission.Id})");
                await _permissionRepository.UpdateAsync(permission);
                Logger.Info($"Successfully updated permission: {permission.Code} (ID: {permission.Id})");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update permission: {permission.Code} (ID: {permission.Id})", ex);
                return false;
            }
        }

        public async Task<bool> DeletePermissionAsync(int permissionId)
        {
            try
            {
                Logger.Info($"Deleting permission with ID: {permissionId}");
                await _permissionRepository.DeleteAsync(permissionId);
                Logger.Info($"Successfully deleted permission with ID: {permissionId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete permission with ID: {permissionId}", ex);
                return false;
            }
        }
    }
}