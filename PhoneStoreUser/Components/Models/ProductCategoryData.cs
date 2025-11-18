namespace PhoneStoreUser.Components.Models;

public class ProductCategoryData
{
    public static List<ProductCategory> GetSampleProductCategories() => new()
    {
        new ProductCategory(Id: 1, Name: "Smartphones", ParentId: null, Note: "All smartphone devices"),
        new ProductCategory(Id: 2, Name: "Accessories", ParentId: null, Note: "Supporting gadgets and add-ons"),
        new ProductCategory(Id: 3, Name: "Android Phones", ParentId: 1, Note: "Android-based smartphones"),
        new ProductCategory(Id: 4, Name: "Wireless Audio", ParentId: 2, Note: "Bluetooth earphones and speakers"),
        new ProductCategory(Id: 5, Name: "Charging Solutions", ParentId: 2, Note: "Chargers, cables and power accessories")
    };
}
