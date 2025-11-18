namespace PhoneStoreUser.Components.Models;

public class ProductAttributeValueData
{
    public static List<ProductAttributeValue> GetSampleProductAttributeValues() => new()
    {
        new ProductAttributeValue(Id: 1, ProductId: 1, AttributeId: 1, OptionId: 1, ValueText: "Xanh Dương"),
        new ProductAttributeValue(Id: 2, ProductId: 1, AttributeId: 2, OptionId: 17, ValueText: "128GB"),
        new ProductAttributeValue(Id: 3, ProductId: 1, AttributeId: 4, OptionId: 29, ValueText: "Chính hãng VN/A"),
        new ProductAttributeValue(Id: 4, ProductId: 2, AttributeId: 1, OptionId: 1, ValueText: "Xanh"),
        new ProductAttributeValue(Id: 5, ProductId: 2, AttributeId: 2, OptionId: 17, ValueText: "128GB"),
        new ProductAttributeValue(Id: 6, ProductId: 2, AttributeId: 4, OptionId: 29, ValueText: "Chính hãng VN/A"),
        new ProductAttributeValue(Id: 7, ProductId: 3, AttributeId: 1, OptionId: 1, ValueText: "Đen"),
        new ProductAttributeValue(Id: 8, ProductId: 3, AttributeId: 2, OptionId: 17, ValueText: "128GB"),
        new ProductAttributeValue(Id: 9, ProductId: 3, AttributeId: 4, OptionId: 29, ValueText: "Chính hãng VN/A"),
        new ProductAttributeValue(Id: 10, ProductId: 4, AttributeId: 1, OptionId: 1, ValueText: "Tím"),
        new ProductAttributeValue(Id: 11, ProductId: 4, AttributeId: 2, OptionId: 17, ValueText: "128GB"),
        new ProductAttributeValue(Id: 12, ProductId: 4, AttributeId: 4, OptionId: 29, ValueText: "Chính hãng VN/A"),
        new ProductAttributeValue(Id: 13, ProductId: 5, AttributeId: 1, OptionId: 1, ValueText: "Đỏ"),
        new ProductAttributeValue(Id: 14, ProductId: 5, AttributeId: 2, OptionId: 17, ValueText: "128GB"),
        new ProductAttributeValue(Id: 15, ProductId: 5, AttributeId: 4, OptionId: 29, ValueText: "Chính hãng VN/A")
    };
}
