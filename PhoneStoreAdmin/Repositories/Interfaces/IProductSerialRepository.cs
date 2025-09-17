using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IProductSerialRepository : IRepository<ProductSerial>
    {
        ProductSerial GetBySerialNumber(string serialNumber);
        ProductSerial GetByImei1(string imei1);
        ProductSerial GetByImei2(string imei2);
        IEnumerable<ProductSerial> GetByProductId(int productId);
        IEnumerable<ProductSerial> GetByStatus(PhoneStoreAdmin.Models.Enums.SerialStatus status);
    }
}
