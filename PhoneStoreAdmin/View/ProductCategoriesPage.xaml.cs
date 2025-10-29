using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services;
using System.Collections.ObjectModel;

namespace PhoneStoreAdmin.View
{
    public sealed partial class ProductCategoriesPage : Page
    {
        private readonly IProductCategoryRepository _categoryRepo;
        public ObservableCollection<ProductCategory> Categories { get; set; } = new();

        public ProductCategoriesPage()
        {
            this.InitializeComponent();
            _categoryRepo = ServiceContainer.GetService<IProductCategoryRepository>();
            LoadCategories();
        }

        private void LoadCategories()
        {
            Categories.Clear();
            foreach (var cat in _categoryRepo.GetAll())
                Categories.Add(cat);
            CategoriesListView.ItemsSource = Categories;
        }
    }
}
