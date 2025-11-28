using System.Collections.Generic;
using PhoneStoreRepository.Models;

namespace PhoneStore.Services.ViewModels
{
    public class InvoiceLineSerialRequest
    {
        public InvoiceLine Line { get; set; } = null!;
        public List<string> SerialNumbers { get; set; } = new();
    }
}
