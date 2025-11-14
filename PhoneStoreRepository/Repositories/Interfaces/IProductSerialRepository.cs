using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IProductSerialRepository : IRepository<ProductSerial>
    {
        ProductSerial GetBySerialNumber(string serialNumber);
        ProductSerial GetByImei1(string imei1);
        ProductSerial GetByImei2(string imei2);
        IEnumerable<ProductSerial> GetByProductId(int productId);
        IEnumerable<ProductSerial> GetByStatus(PhoneStoreRepository.Models.Enums.SerialStatus status);
        IDictionary<int, int> GetCountsByProductIds(IEnumerable<int> productIds);
    }
}
