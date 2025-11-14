using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PhoneStoreRepository.Models;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class RoleMultiSelectorDialog : ContentDialog
    {
        private readonly IRoleService _roleService;
        private List<Role> _allRoles = new();
        private List<Role> _filteredRoles = new();
        private readonly List<int> _initialSelectedRoleIds;
        private readonly Dictionary<int, CheckBox> _roleCheckboxes = new();

        // Search debounce
        private Timer? _searchTimer;
        private const int SearchDelayMs = 300;

        public List<int> SelectedRoleIds { get; private set; } = new();

        public RoleMultiSelectorDialog(IRoleService roleService, List<int>? currentSelectedIds = null)
        {
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _initialSelectedRoleIds = currentSelectedIds ?? new List<int>();
            SelectedRoleIds = new List<int>(_initialSelectedRoleIds);

            this.InitializeComponent();
            this.Loaded += RoleMultiSelectorDialog_Loaded;
        }

        private async void RoleMultiSelectorDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadRolesAsync();
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                ShowLoading(true);

                var roles = await _roleService.GetAllRolesAsync();
                if (roles == null || roles.Count == 0)
                {
                    ShowLoading(false);
                    ShowEmpty(true);
                    return;
                }

                _allRoles = roles;
                _filteredRoles = new List<Role>(_allRoles);
                
                RenderRolesList();
                ShowLoading(false);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load roles: {ex.Message}", ex);
                ShowLoading(false);
                ShowEmpty(true);
            }
        }

        private void RenderRolesList()
        {
            RolesContainer.Children.Clear();
            _roleCheckboxes.Clear();

            if (_filteredRoles.Count == 0)
            {
                ShowEmpty(true);
                return;
            }

            ShowEmpty(false);

            foreach (var role in _filteredRoles)
            {
                var card = CreateRoleCard(role);
                RolesContainer.Children.Add(card);
            }

            UpdateSelectedCount();
        }

      private Border CreateRoleCard(Role role)
        {
            bool isSelected = SelectedRoleIds.Contains(role.Id);

            // --- Texts ---
            var nameText = new TextBlock
            {
                Text = role.Name,
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["BrushPrimary"]
            };

            var descText = new TextBlock
            {
                Text = role.Description ?? "Không có mô tả",
                FontSize = 14,
                Foreground = (Brush)Application.Current.Resources["BrushSecondary"],
                TextWrapping = TextWrapping.Wrap
            };

            var weightText = new TextBlock
            {
                Text = $"Trọng số: {role.Weight}",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["BrushOnPrimaryContainer"]
            };

            var weightBadge = new Border
            {
                Background = (Brush)Application.Current.Resources["BrushPrimaryContainer"],
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 6, 0, 0),
                Child = weightText
            };

            // --- Stack content ---
            var contentStack = new StackPanel { Spacing = 4 };
            contentStack.Children.Add(nameText);
            contentStack.Children.Add(descText);
            contentStack.Children.Add(weightBadge);

            // --- Optional: small check icon ---
            var checkIcon = new FontIcon
            {
                Glyph = "\uE73E", // CheckMark
                FontSize = 18,
                Foreground = (Brush)Application.Current.Resources["BrushPrimary"],
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
            };

            // --- Grid: content + icon overlay ---
            var grid = new Grid();
            grid.Children.Add(contentStack);
            grid.Children.Add(checkIcon);

            // --- Card border ---
            var card = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 8),
                Child = grid,
                Tag = role.Id  // Store role ID for sorting
            };

            // --- Cập nhật màu hiển thị ---
            UpdateRoleCardVisual(card, isSelected);

            // --- Click để chọn/bỏ chọn ---
            card.Tapped += (s, e) =>
            {
                if (SelectedRoleIds.Contains(role.Id))
                {
                    SelectedRoleIds.Remove(role.Id);
                    checkIcon.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SelectedRoleIds.Add(role.Id);
                    checkIcon.Visibility = Visibility.Visible;
                }

                UpdateRoleCardVisual(card, SelectedRoleIds.Contains(role.Id));
                ResortRoleCards();
                UpdateSelectedCountText();
            };

            return card;
        }

        private void UpdateRoleCardVisual(Border card, bool isSelected)
        {
            if (isSelected)
            {
                card.Background = (Brush)Application.Current.Resources["BrushPrimaryContainer"];
                card.BorderBrush = (Brush)Application.Current.Resources["BrushPrimary"];
            }
            else
            {
                card.Background = (Brush)Application.Current.Resources["BrushSurface"];
                card.BorderBrush = (Brush)Application.Current.Resources["BrushBorder"];
            }
        }

        private void ResortRoleCards()
        {
            var cards = RolesContainer.Children.OfType<Border>().ToList();
            var selected = cards.Where(c =>
            {
                if (c.Tag is int roleId)
                {
                    return SelectedRoleIds.Contains(roleId);
                }
                return false;
            }).ToList();
            var unselected = cards.Except(selected).ToList();

            RolesContainer.Children.Clear();
            foreach (var c in selected) RolesContainer.Children.Add(c);
            foreach (var c in unselected) RolesContainer.Children.Add(c);
        }

        private void UpdateSelectedCountText()
        {
            SelectedCountText.Text = $"{SelectedRoleIds.Count}";
        }


        private void RoleCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkbox && checkbox.Tag is int roleId)
            {
                if (checkbox.IsChecked == true)
                {
                    if (!SelectedRoleIds.Contains(roleId))
                    {
                        SelectedRoleIds.Add(roleId);
                    }
                }
                else
                {
                    SelectedRoleIds.Remove(roleId);
                }

                UpdateSelectedCount();
            }
        }

        private void UpdateSelectedCount()
        {
            SelectedCountText.Text = SelectedRoleIds.Count.ToString();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Debounce search
            _searchTimer?.Dispose();
            _searchTimer = new Timer(_ =>
            {
                DispatcherQueue.TryEnqueue(() => PerformSearch(SearchBox.Text));
            }, null, SearchDelayMs, Timeout.Infinite);
        }

        private void PerformSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredRoles = new List<Role>(_allRoles);
            }
            else
            {
                _filteredRoles = _allRoles.Where(r =>
                    r.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (r.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            RenderRolesList();
        }

        private void ShowLoading(bool show)
        {
            LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            RolesScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowEmpty(bool show)
        {
            EmptyPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            RolesScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Return selected role IDs
            Logger.Info($"Selected {SelectedRoleIds.Count} roles: [{string.Join(", ", SelectedRoleIds)}]");
        }
    }
}
