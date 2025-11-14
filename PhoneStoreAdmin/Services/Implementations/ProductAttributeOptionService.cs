using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreRepository.Utils;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class ProductAttributeOptionService : IProductAttributeOptionService
    {
        private readonly IProductAttributeOptionRepository _repository;
        private readonly ConcurrentDictionary<int, IReadOnlyList<ProductAttributeOption>> _optionCache = new();

        public ProductAttributeOptionService(IProductAttributeOptionRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public IReadOnlyList<ProductAttributeOption> GetOptionsForAttribute(int attributeId)
        {
            if (attributeId <= 0)
            {
                return Array.Empty<ProductAttributeOption>();
            }

            return _optionCache.GetOrAdd(attributeId, id =>
            {
                try
                {
                    return _repository.GetByAttributeId(id)
                        .Where(option => option.IsActive)
                        .OrderBy(option => option.SortOrder)
                        .ThenBy(option => option.DisplayValue)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load options for attribute {id}", ex);
                    return Array.Empty<ProductAttributeOption>();
                }
            });
        }

        public IDictionary<int, IReadOnlyList<ProductAttributeOption>> GetOptionsForAttributes(IEnumerable<int> attributeIds)
        {
            var ids = attributeIds?.Where(id => id > 0).Distinct().ToList();
            if (ids == null || ids.Count == 0)
            {
                return new Dictionary<int, IReadOnlyList<ProductAttributeOption>>();
            }

            var result = new Dictionary<int, IReadOnlyList<ProductAttributeOption>>();
            var missing = new List<int>();

            foreach (var id in ids)
            {
                if (_optionCache.TryGetValue(id, out var cached))
                {
                    result[id] = cached;
                }
                else
                {
                    missing.Add(id);
                }
            }

            if (missing.Count > 0)
            {
                try
                {
                    var fetched = _repository.GetByAttributeIds(missing);
                    foreach (var pair in fetched)
                    {
                        var ordered = pair.Value
                            .Where(option => option.IsActive)
                            .OrderBy(option => option.SortOrder)
                            .ThenBy(option => option.DisplayValue)
                            .ToList();

                        _optionCache[pair.Key] = ordered;
                        result[pair.Key] = ordered;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to bulk load attribute options", ex);
                }
            }

            foreach (var id in ids)
            {
                if (!result.ContainsKey(id))
                {
                    result[id] = Array.Empty<ProductAttributeOption>();
                }
            }

            return result;
        }

        public ProductAttributeOption? GetOptionById(int optionId)
        {
            if (optionId <= 0)
            {
                return null;
            }

            try
            {
                var option = _repository.GetById(optionId);
                if (option != null)
                {
                    _optionCache.AddOrUpdate(option.AttributeId, new List<ProductAttributeOption> { option }, (_, existing) =>
                    {
                        if (existing.Any(o => o.Id == option.Id))
                        {
                            return existing;
                        }

                        var merged = existing.ToList();
                        merged.Add(option);
                        return merged
                            .Where(o => o.IsActive)
                            .OrderBy(o => o.SortOrder)
                            .ThenBy(o => o.DisplayValue)
                            .ToList();
                    });
                }

                return option;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get attribute option {optionId}", ex);
                return null;
            }
        }

        public ProductAttributeOption? FindByDisplay(int attributeId, string displayValue)
        {
            if (attributeId <= 0 || string.IsNullOrWhiteSpace(displayValue))
            {
                return null;
            }

            try
            {
                var cached = GetOptionsForAttribute(attributeId);
                var match = cached.FirstOrDefault(o => string.Equals(o.DisplayValue, displayValue, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }

                var option = _repository.FindByAttributeAndDisplay(attributeId, displayValue);
                if (option != null)
                {
                    _optionCache.AddOrUpdate(attributeId, new List<ProductAttributeOption> { option }, (_, existing) =>
                    {
                        if (existing.Any(o => o.Id == option.Id))
                        {
                            return existing;
                        }

                        var merged = existing.ToList();
                        merged.Add(option);
                        return merged
                            .Where(o => o.IsActive)
                            .OrderBy(o => o.SortOrder)
                            .ThenBy(o => o.DisplayValue)
                            .ToList();
                    });
                }

                return option;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to find option '{displayValue}' for attribute {attributeId}", ex);
                return null;
            }
        }

        public void EnsureOption(int attributeId, string displayValue, string? normalizedValue, int sortOrder, bool isActive = true)
        {
            if (attributeId <= 0 || string.IsNullOrWhiteSpace(displayValue))
            {
                return;
            }

            try
            {
                var existing = FindByDisplay(attributeId, displayValue);
                if (existing != null)
                {
                    return;
                }

                var option = new ProductAttributeOption(attributeId, displayValue)
                {
                    NormalizedValue = normalizedValue,
                    SortOrder = sortOrder,
                    IsActive = isActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _repository.Insert(option);
                _optionCache.AddOrUpdate(attributeId, new List<ProductAttributeOption> { option }, (_, current) =>
                {
                    var merged = current.ToList();
                    merged.Add(option);
                    return merged
                        .Where(o => o.IsActive)
                        .OrderBy(o => o.SortOrder)
                        .ThenBy(o => o.DisplayValue)
                        .ToList();
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to ensure option '{displayValue}' for attribute {attributeId}", ex);
            }
        }

        public IReadOnlyList<ProductAttributeOption> GetEditableOptionsForAttribute(int attributeId)
        {
            if (attributeId <= 0)
            {
                return Array.Empty<ProductAttributeOption>();
            }

            try
            {
                return _repository.GetByAttributeId(attributeId)
                    .OrderBy(option => option.SortOrder)
                    .ThenBy(option => option.DisplayValue)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load editable options for attribute {attributeId}", ex);
                return Array.Empty<ProductAttributeOption>();
            }
        }

        public ProductAttributeOption CreateOption(ProductAttributeOption option)
        {
            if (option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            try
            {
                _repository.Insert(option);
                _optionCache.TryRemove(option.AttributeId, out _);
                return option;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create option for attribute {option.AttributeId}", ex);
                throw;
            }
        }

        public void UpdateOption(ProductAttributeOption option)
        {
            if (option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            try
            {
                _repository.Update(option);
                _optionCache.TryRemove(option.AttributeId, out _);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update option {option.Id}", ex);
                throw;
            }
        }

        public void DeleteOption(int optionId)
        {
            if (optionId <= 0)
            {
                return;
            }

            try
            {
                var option = _repository.GetById(optionId);
                _repository.Delete(optionId);
                _optionCache.TryRemove(option.AttributeId, out _);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete option {optionId}", ex);
                throw;
            }
        }
    }
}
