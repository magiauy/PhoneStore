namespace PhoneStoreUser.Components.Models;

public class ProductModelAttributeData
{
    public static List<ProductModelAttribute> GetSampleProductModelAttributes() => new()
    {
        new ProductModelAttribute(ProductModelId: 1, ProductAttributeId: 1),
        new ProductModelAttribute(ProductModelId: 1, ProductAttributeId: 2),
        new ProductModelAttribute(ProductModelId: 1, ProductAttributeId: 4)
    };
}
