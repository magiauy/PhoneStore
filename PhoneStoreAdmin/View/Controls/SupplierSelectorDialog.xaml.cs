using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using PhoneStoreRepository.Models;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class SupplierSelectorDialog : ContentDialog
    {
        private readonly ISupplierService _supplierService;
        private List<Supplier> _allSuppliers = new List<Supplier>();
        private List<Supplier> _searchFilteredSuppliers = new List<Supplier>();
        private ObservableCollection<Supplier> _displayedSuppliers = new ObservableCollection<Supplier>();
        private CancellationTokenSource? _searchCts;
        private Supplier? _selectedSupplier;
        private Border? _currentSelectedCard;
        
        // Pagination & Infinite Scroll
        private const int PAGE_SIZE = 100;
        private int _currentLoadedCount = 0;
        private bool _isLoadingMore = false;
        private bool _hasMoreData = true;

        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
                UpdateSelectionUI();
            }
        }

        public string TotalSuppliersText => _searchFilteredSuppliers.Count.ToString();

        public SupplierSelectorDialog(ISupplierService supplierService, Supplier? currentSelection = null)
        {
            _supplierService = supplierService;
            _selectedSupplier = currentSelection;
            
            this.InitializeComponent();
            
            // Disable Primary button if no selection
            this.IsPrimaryButtonEnabled = currentSelection != null;
            
            this.Loaded += SupplierSelectorDialog_Loaded;
            this.Closing += SupplierSelectorDialog_Closing;
            
            // Add keyboard accelerators
            this.KeyDown += SupplierSelectorDialog_KeyDown;
        }

        private void SupplierSelectorDialog_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // Handle Escape key to close dialog
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                this.Hide();
                e.Handled = true;
            }
        }

        private async void SupplierSelectorDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus on search box for immediate typing
            SearchBox?.Focus(FocusState.Programmatic);
            
            await LoadSuppliersAsync();
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                ShowLoading(true);

                // Bind ItemsSource trước
                SuppliersRepeater.ItemsSource = _displayedSuppliers;

                // Load initial metadata only (không load hết data)
                await Task.Run(() =>
                {
                    _allSuppliers = _supplierService.GetAll()
                        .Where(s => s.Id > 0) // Filter out placeholder
                        .ToList();
                });

                System.Diagnostics.Debug.WriteLine($"Loaded {_allSuppliers.Count} suppliers from service");

                _searchFilteredSuppliers = _allSuppliers;
                
                // Load first page only
                await LoadNextPageAsync();
                
                System.Diagnostics.Debug.WriteLine($"Displayed {_displayedSuppliers.Count} suppliers in UI");
                
                ShowLoading(false);

                // Setup scroll detection for infinite loading
                SuppliersScrollViewer.ViewChanged += SuppliersScrollViewer_ViewChanged;

                // Update selection UI after loading
                if (_selectedSupplier != null)
                {
                    await Task.Delay(100); // Wait for UI to render
                    UpdateSelectionUI();
                }
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                ShowEmptyState(true);
                System.Diagnostics.Debug.WriteLine($"Error loading suppliers: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task LoadNextPageAsync()
        {
            if (_isLoadingMore || !_hasMoreData) return;

            _isLoadingMore = true;

            await Task.Run(() =>
            {
                var nextBatch = _searchFilteredSuppliers
                    .Skip(_currentLoadedCount)
                    .Take(PAGE_SIZE)
                    .ToList();

                if (nextBatch.Count == 0)
                {
                    _hasMoreData = false;
                    return;
                }

                // Add to UI collection on UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    foreach (var supplier in nextBatch)
                    {
                        _displayedSuppliers.Add(supplier);
                    }

                    _currentLoadedCount += nextBatch.Count;
                    _hasMoreData = _currentLoadedCount < _searchFilteredSuppliers.Count;
                });
            });

            _isLoadingMore = false;
        }

        private async void SuppliersScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            if (_isLoadingMore || !_hasMoreData) return;

            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;

            // Check if scrolled near bottom (80% threshold)
            var verticalOffset = scrollViewer.VerticalOffset;
            var maxVerticalOffset = scrollViewer.ScrollableHeight;

            if (maxVerticalOffset > 0 && verticalOffset >= maxVerticalOffset * 0.8)
            {
                await LoadNextPageAsync();
            }
        }

        private async void UpdateFilteredSuppliers(string searchText)
        {
            // Reset pagination
            _currentLoadedCount = 0;
            _hasMoreData = true;
            _displayedSuppliers.Clear();

            await Task.Run(() =>
            {
                _searchFilteredSuppliers = string.IsNullOrWhiteSpace(searchText)
                    ? _allSuppliers
                    : _allSuppliers.Where(s =>
                        (s.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.Phone?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.Email?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.Address?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
            });

            // Load first page
            await LoadNextPageAsync();

            SuppliersRepeater.ItemsSource = _displayedSuppliers;

            // Update UI states
            ShowEmptyState(_searchFilteredSuppliers.Count == 0 && !string.IsNullOrWhiteSpace(searchText));
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Cancel previous search
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // Debounce: wait 300ms before searching
                await Task.Delay(300, token);

                if (!token.IsCancellationRequested)
                {
                    var searchText = SearchBox.Text?.Trim() ?? string.Empty;
                    UpdateFilteredSuppliers(searchText);
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when user types quickly
            }
        }

        private void SupplierCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                // Hover animation: scale up slightly
                var scaleTransform = border.RenderTransform as ScaleTransform;
                if (scaleTransform != null)
                {
                    var storyboard = new Storyboard();
                    
                    var scaleXAnim = new DoubleAnimation
                    {
                        To = 1.01,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(scaleXAnim, scaleTransform);
                    Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");
                    
                    var scaleYAnim = new DoubleAnimation
                    {
                        To = 1.01,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(scaleYAnim, scaleTransform);
                    Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");
                    
                    storyboard.Children.Add(scaleXAnim);
                    storyboard.Children.Add(scaleYAnim);
                    storyboard.Begin();
                }

                // Change border color
                border.BorderBrush = (Brush)Application.Current.Resources["BrushPrimary"];
            }
        }

        private void SupplierCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                // Reset scale animation
                var scaleTransform = border.RenderTransform as ScaleTransform;
                if (scaleTransform != null)
                {
                    var storyboard = new Storyboard();
                    
                    var scaleXAnim = new DoubleAnimation
                    {
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(scaleXAnim, scaleTransform);
                    Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");
                    
                    var scaleYAnim = new DoubleAnimation
                    {
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(scaleYAnim, scaleTransform);
                    Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");
                    
                    storyboard.Children.Add(scaleXAnim);
                    storyboard.Children.Add(scaleYAnim);
                    storyboard.Begin();
                }

                // Reset border color (unless it's selected)
                if (border != _currentSelectedCard)
                {
                    border.BorderBrush = (Brush)Application.Current.Resources["BrushBorder"];
                }
            }
        }

        private void SupplierCard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //Log DataContext type for debugging
            var senderType = sender?.GetType().Name ?? "<null>";
            var orig = e.OriginalSource;
            var origType = orig?.GetType().Name ?? "<null>";
            var senderData = (sender as FrameworkElement)?.DataContext?.GetType().Name ?? "<no DataContext>";
            Logger.Info($"Tapped sender={senderType} originalSource={origType} sender.DataContext={senderData}");
            
            if (sender is Border border && border.DataContext is Supplier supplier)
            {
                Logger.Info($"Supplier card tapped: {supplier.Name}");
                // Clear previous selection
                if (_currentSelectedCard != null && _currentSelectedCard != border)
                {
                    ResetCardVisual(_currentSelectedCard);
                }

                _selectedSupplier = supplier;
                _currentSelectedCard = border;

                // Apply selection visual immediately
                ApplySelectionVisual(border);

                // Enable primary button
                IsPrimaryButtonEnabled = true;
            }
        }

        private void ResetCardVisual(Border card)
        {
            card.BorderBrush = (Brush)Application.Current.Resources["BrushBorder"];
            card.Background = (Brush)Application.Current.Resources["BrushSurface"];
            
            // Hide checkmark
            var grid = card.Child as Grid;
            if (grid != null && grid.Children.Count > 2)
            {
                var checkmark = grid.Children[2] as FontIcon;
                if (checkmark != null && checkmark.Glyph == "\uE73E")
                {
                    checkmark.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ApplySelectionVisual(Border card)
        {
            card.BorderBrush = (Brush)Application.Current.Resources["BrushPrimary"];
            
            var primaryBrush = (SolidColorBrush)Application.Current.Resources["BrushPrimary"];
            card.Background = new SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(20, 
                    primaryBrush.Color.R,
                    primaryBrush.Color.G,
                    primaryBrush.Color.B
                )
            );

            // Show checkmark
            var grid = card.Child as Grid;
            if (grid != null && grid.Children.Count > 2)
            {
                var checkmark = grid.Children[2] as FontIcon;
                if (checkmark != null && checkmark.Glyph == "\uE73E")
                {
                    checkmark.Visibility = Visibility.Visible;
                }
            }
        }

        private void UpdateSelectionUI()
        {
            // Find and highlight the selected supplier card in loaded items
            if (_selectedSupplier != null && SuppliersRepeater.ItemsSourceView != null)
            {
                for (int i = 0; i < SuppliersRepeater.ItemsSourceView.Count; i++)
                {
                    var container = SuppliersRepeater.TryGetElement(i);
                    if (container is Border card && card.DataContext is Supplier supplier)
                    {
                        if (supplier.Id == _selectedSupplier.Id)
                        {
                            _currentSelectedCard = card;
                            ApplySelectionVisual(card);
                            break;
                        }
                    }
                }
            }

            // Update primary button state
            IsPrimaryButtonEnabled = _selectedSupplier != null;
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelectedCard != null)
            {
                ResetCardVisual(_currentSelectedCard);
            }
            
            _selectedSupplier = null;
            _currentSelectedCard = null;
            IsPrimaryButtonEnabled = false;
        }

        private void ShowLoading(bool show)
        {
            LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            SuppliersScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowEmptyState(bool show)
        {
            EmptyStatePanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            SuppliersScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SupplierSelectorDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            // Clear all internal data when dialog is closing
            ClearDialogData();
        }

        private void ClearDialogData()
        {
            // Cancel any ongoing search
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;

            // Clear collections
            _displayedSuppliers.Clear();
            _searchFilteredSuppliers.Clear();
            _allSuppliers.Clear();

            // Reset pagination state
            _currentLoadedCount = 0;
            _isLoadingMore = false;
            _hasMoreData = true;

            // Clear selection state
            _currentSelectedCard = null;
            // Note: _selectedSupplier is kept to return the result

            // Unsubscribe from scroll events to prevent memory leaks
            if (SuppliersScrollViewer != null)
            {
                SuppliersScrollViewer.ViewChanged -= SuppliersScrollViewer_ViewChanged;
            }

            Logger.Info("Dialog data cleared");
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Selection is already stored in _selectedSupplier
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // User cancelled - no action needed
        }
    }
}
