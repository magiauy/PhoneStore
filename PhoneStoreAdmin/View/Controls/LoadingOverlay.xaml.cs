using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class LoadingOverlay : UserControl
    {
        public LoadingOverlay()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Show loading overlay with optional custom message
        /// </summary>
        /// <param name="message">Loading message to display</param>
        public void Show(string message = "Đang tải...")
        {
            LoadingText.Text = message;
            LoadingRing.IsActive = true;
            RootGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hide loading overlay
        /// </summary>
        public void Hide()
        {
            LoadingRing.IsActive = false;
            RootGrid.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Check if loading overlay is currently visible
        /// </summary>
        public bool IsVisible => RootGrid.Visibility == Visibility.Visible;
    }
}
