using System.ComponentModel.DataAnnotations;

namespace PhoneStoreUser.Components.ViewModels;

public class CreateOrderDto
{
    public int? CustomerId { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn ít nhất một sản phẩm")]
    [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một sản phẩm")]
    public List<CreateOrderLineDto> Lines { get; set; } = new();

    public string PaymentMethod { get; set; } = "cash"; // cash, bank
    
    public string? Note { get; set; }
}

public class CreateOrderLineDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsSerialTracked { get; set; }
    
    // List of serials entered by user. Count must match Quantity if IsSerialTracked is true.
    public List<string> SerialNumbers { get; set; } = new();
}
