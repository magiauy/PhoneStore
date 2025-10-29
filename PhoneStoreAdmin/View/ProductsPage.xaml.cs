using Microsoft.UI.Xaml.Controls;

namespace PhoneStoreAdmin.View
{
    public sealed partial class ProductsPage : Page
    {
        public ProductsPage()
        {
            this.InitializeComponent();
        }

        private void ProductDetailDeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var dlg = new ContentDialog
            {
                Title = "Delete Product",
                Content = "Are you sure you want to delete this product? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };
            dlg.ShowAsync().Completed = (info, status) => { /* do nothing for now */ };
        }

        private void AddButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ShowAddProductDialog();
        }

        private void ShowAddProductDialog()
        {
            var panel = new StackPanel { Spacing = 12 };
            var nameBox = new TextBox { PlaceholderText = "Product name" };
            var brandBox = new TextBox { PlaceholderText = "Brand" };
            var priceBox = new TextBox { PlaceholderText = "Price" };
            var availableBox = new TextBox { PlaceholderText = "Available quantity" };
            panel.Children.Add(new TextBlock { Text = "Product Name" });
            panel.Children.Add(nameBox);
            panel.Children.Add(new TextBlock { Text = "Brand" });
            panel.Children.Add(brandBox);
            panel.Children.Add(new TextBlock { Text = "Price" });
            panel.Children.Add(priceBox);
            panel.Children.Add(new TextBlock { Text = "Available" });
            panel.Children.Add(availableBox);

            var dlg = new ContentDialog
            {
                Title = "Add Product",
                Content = panel,
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };
            dlg.ShowAsync().Completed = (info, status) => { /* do nothing for now */ };

        }
        // Handles Edit button click for product cards
        private void EditProductButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var button = sender as Microsoft.UI.Xaml.Controls.Button;
            if (button == null) return;

            // Traverse up to the Grid containing product info
            var grid = FindParent<Microsoft.UI.Xaml.Controls.Grid>(button);
            if (grid == null) return;

            // Find the name/brand/price StackPanels by their column index
            Microsoft.UI.Xaml.Controls.StackPanel namePanel = null;
            Microsoft.UI.Xaml.Controls.StackPanel pricePanel = null;
            foreach (var child in grid.Children)
            {
                if (child is Microsoft.UI.Xaml.Controls.StackPanel sp)
                {
                    var col = Microsoft.UI.Xaml.Controls.Grid.GetColumn(sp);
                    if (col == 2) namePanel = sp;
                    if (col == 3) pricePanel = sp;
                }
            }
            if (namePanel == null || pricePanel == null) return;

            var nameText = namePanel.Children[0] as Microsoft.UI.Xaml.Controls.TextBlock;
            var brandText = namePanel.Children[1] as Microsoft.UI.Xaml.Controls.TextBlock;
            // Available is the third child in namePanel
            var availableText = namePanel.Children.Count > 2 ? namePanel.Children[2] as Microsoft.UI.Xaml.Controls.TextBlock : null;
            var priceText = pricePanel.Children[0] as Microsoft.UI.Xaml.Controls.TextBlock;

            if (nameText == null || brandText == null || priceText == null || availableText == null) return;

            ProductDetailName.Text = nameText.Text;
            ProductDetailBrand.Text = brandText.Text;
            ProductDetailPrice.Text = priceText.Text;
            ProductDetailAvailable.Text = availableText.Text;

            ProductNameInput.Text = nameText.Text;
            ProductBrandInput.Text = brandText.Text;
            ProductPriceInput.Text = priceText.Text.Replace("$", "");
            // Parse available quantity from text like "Available: 12"
            var availableValue = availableText.Text;
            if (availableValue.StartsWith("Available:"))
            {
                var parts = availableValue.Split(':');
                if (parts.Length == 2)
                {
                    ProductAvailableInput.Text = parts[1].Trim();
                }
                else
                {
                    ProductAvailableInput.Text = "";
                }
            }
            else
            {
                ProductAvailableInput.Text = "";
            }

            ProductDetailPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            ProductDetailPlaceholder.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        // Helper to find parent of a given type
        private T FindParent<T>(object child) where T : class
        {
            var depObj = child as Microsoft.UI.Xaml.DependencyObject;
            while (depObj != null)
            {
                var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(depObj);
                if (parent is T tParent)
                    return tParent;
                depObj = parent;
            }
            return null;
        }
    }
}