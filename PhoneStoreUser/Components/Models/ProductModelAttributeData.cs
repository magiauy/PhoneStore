namespace PhoneStoreUser.Components.Models;

public class ProductModelAttributeData
{
    public static List<ProductModelAttribute> GetSampleProductModelAttributes() => new()
    {
        new ProductModelAttribute(ProductModelId: 1, ProductAttributeId: 1),
        new ProductModelAttribute(ProductModelId: 1, ProductAttributeId: 2),
        new ProductModelAttribute(ProductModelId: 1, ProductAttributeId: 4),
        new ProductModelAttribute(ProductModelId: 2, ProductAttributeId: 1),
        new ProductModelAttribute(ProductModelId: 2, ProductAttributeId: 2),
        new ProductModelAttribute(ProductModelId: 2, ProductAttributeId: 3),
        new ProductModelAttribute(ProductModelId: 2, ProductAttributeId: 4),
        new ProductModelAttribute(ProductModelId: 3, ProductAttributeId: 1),
        new ProductModelAttribute(ProductModelId: 3, ProductAttributeId: 2),
        new ProductModelAttribute(ProductModelId: 3, ProductAttributeId: 3),
        new ProductModelAttribute(ProductModelId: 3, ProductAttributeId: 4),
        new ProductModelAttribute(ProductModelId: 4, ProductAttributeId: 1),
        new ProductModelAttribute(ProductModelId: 4, ProductAttributeId: 2),
        new ProductModelAttribute(ProductModelId: 4, ProductAttributeId: 3),
        new ProductModelAttribute(ProductModelId: 4, ProductAttributeId: 4)
    };
}
