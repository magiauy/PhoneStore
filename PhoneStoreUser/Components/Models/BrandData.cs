namespace PhoneStoreUser.Components.Models;

public class BrandData
{
    public static List<Brand> GetSampleBrands() => new()
    {
        new Brand(Id: 1, Name: "Apple"),
        new Brand(Id: 2, Name: "Samsung"),
        new Brand(Id: 3, Name: "Xiaomi"),
        new Brand(Id: 4, Name: "Honor"),
        new Brand(Id: 5, Name: "Oppo"),
        new Brand(Id: 6, Name: "Tecno")
    };
}
