using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreRepository.Models;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class RoleSelectorDialog : ContentDialog
    {
        private readonly List<Role> _allRoles;
        private readonly Dictionary<int, CheckBox> _roleCheckBoxes;

        public List<Role> SelectedRoles { get; private set; }

        public RoleSelectorDialog(List<Role> availableRoles, List<Role>? preSelectedRoles = null)
        {
            this.InitializeComponent();
            _allRoles = availableRoles;
            _roleCheckBoxes = new Dictionary<int, CheckBox>();
            SelectedRoles = new List<Role>();

            LoadRoles(preSelectedRoles);
        }

        private void LoadRoles(List<Role>? preSelectedRoles)
        {
            RolesPanel.Children.Clear();
            _roleCheckBoxes.Clear();

            foreach (var role in _allRoles.OrderBy(r => r.Weight))
            {
                var checkBox = new CheckBox
                {
                    Content = role.Name,
                    Tag = role,
                    IsChecked = preSelectedRoles?.Any(r => r.Id == role.Id) ?? false,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                // Add description if available
                if (!string.IsNullOrWhiteSpace(role.Description))
                {
                    var stackPanel = new StackPanel();
                    
                    var nameText = new TextBlock
                    {
                        Text = role.Name,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold                    
                    };
                    
                    var descText = new TextBlock
                    {
                        Text = role.Description,
                        FontSize = 12,
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["BrushTextSecondary"],
                        Margin = new Thickness(0, 2, 0, 0)
                    };

                    stackPanel.Children.Add(nameText);
                    stackPanel.Children.Add(descText);
                    
                    checkBox.Content = stackPanel;
                }

                _roleCheckBoxes[role.Id] = checkBox;
                RolesPanel.Children.Add(checkBox);
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SelectedRoles.Clear();
            
            foreach (var kvp in _roleCheckBoxes)
            {
                if (kvp.Value.IsChecked == true)
                {
                    var role = kvp.Value.Tag as Role;
                    if (role != null)
                    {
                        SelectedRoles.Add(role);
                    }
                }
            }
        }
    }
}
