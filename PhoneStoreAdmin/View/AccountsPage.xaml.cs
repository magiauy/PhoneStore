using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PhoneStoreAdmin.View
{
    public sealed partial class AccountsPage : Page
    {
        // ObservableCollection để bind với ListView
        public ObservableCollection<AccountViewModel> Accounts { get; set; }
        
        // Danh sách gốc để thực hiện tìm kiếm
        private List<AccountViewModel> _allAccounts;
        
        // Phân trang
        private int _currentPage = 1;
        private int _itemsPerPage = 20;
        private int _totalPages = 1;

        public AccountsPage()
        {
            this.InitializeComponent();
            Accounts = new ObservableCollection<AccountViewModel>();
            _allAccounts = new List<AccountViewModel>();
            
            // Gán DataContext cho binding
            this.DataContext = this;
            
            // Load dữ liệu mẫu (sẽ thay thế bằng dữ liệu thật sau)
            LoadSampleData();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Refresh data when navigated to this page
            RefreshData();
        }

        #region Event Handlers

        private void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Mở dialog thêm tài khoản mới
            ShowNotImplementedMessage("Thêm tài khoản");
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                FilterAccounts(sender.Text);
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Mở dialog lọc nâng cao
            ShowNotImplementedMessage("Lọc nâng cao");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void AccountsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Not used anymore since SelectionMode="None"
        }

        private void ActionsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var account = button?.Tag as AccountViewModel;
            if (account != null)
            {
                // TODO: Show context menu or actions dialog
                ShowNotImplementedMessage($"Thao tác cho tài khoản: {account.Username}");
            }
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePagination();
                LoadPageData();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                UpdatePagination();
                LoadPageData();
            }
        }

        #endregion

        #region Private Methods

        private void LoadSampleData()
        {
            // Tạo dữ liệu mẫu cho demo với nhiều records để test phân trang
            _allAccounts = new List<AccountViewModel>
            {
                // Admin accounts
                new AccountViewModel
                {
                    Id = 1,
                    Username = "admin",
                    FullName = "Nguyễn Văn Admin",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-90),
                    LastLogin = DateTime.Now.AddHours(-1)
                },
                new AccountViewModel
                {
                    Id = 2,
                    Username = "superadmin",
                    FullName = "Trần Thị Siêu Admin",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-120),
                    LastLogin = DateTime.Now.AddHours(-3)
                },
                
                // Manager accounts
                new AccountViewModel
                {
                    Id = 3,
                    Username = "manager01",
                    FullName = "Lê Văn Quản Lý",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-80),
                    LastLogin = DateTime.Now.AddHours(-5)
                },
                new AccountViewModel
                {
                    Id = 4,
                    Username = "manager02",
                    FullName = "Phạm Thị Minh",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-75),
                    LastLogin = DateTime.Now.AddDays(-1)
                },
                new AccountViewModel
                {
                    Id = 5,
                    Username = "sales_manager",
                    FullName = "Hoàng Văn Bán Hàng",
                    PersonType = "Nhân viên",
                    IsActive = false,
                    CreatedAt = DateTime.Now.AddDays(-60),
                    LastLogin = DateTime.Now.AddDays(-15)
                },
                
                // Staff accounts
                new AccountViewModel
                {
                    Id = 6,
                    Username = "staff_nguyen",
                    FullName = "Nguyễn Văn Nhân Viên",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-50),
                    LastLogin = DateTime.Now.AddHours(-2)
                },
                new AccountViewModel
                {
                    Id = 7,
                    Username = "staff_tran",
                    FullName = "Trần Thị Lan",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-45),
                    LastLogin = DateTime.Now.AddHours(-8)
                },
                new AccountViewModel
                {
                    Id = 8,
                    Username = "staff_le",
                    FullName = "Lê Minh Tuấn",
                    PersonType = "Nhân viên",
                    IsActive = false,
                    CreatedAt = DateTime.Now.AddDays(-40),
                    LastLogin = DateTime.Now.AddDays(-7)
                },
                new AccountViewModel
                {
                    Id = 9,
                    Username = "staff_pham",
                    FullName = "Phạm Thị Hoa",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-35),
                    LastLogin = DateTime.Now.AddDays(-2)
                },
                new AccountViewModel
                {
                    Id = 10,
                    Username = "staff_hoang",
                    FullName = "Hoàng Văn Nam",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-30),
                    LastLogin = DateTime.Now.AddHours(-12)
                },
                
                // Customer accounts
                new AccountViewModel
                {
                    Id = 11,
                    Username = "customer01",
                    FullName = "Nguyễn Văn Khách",
                    PersonType = "Khách hàng",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-25),
                    LastLogin = DateTime.Now.AddHours(-4)
                },
                new AccountViewModel
                {
                    Id = 12,
                    Username = "customer02",
                    FullName = "Trần Thị Mua",
                    PersonType = "Khách hàng",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-22),
                    LastLogin = DateTime.Now.AddHours(-6)
                },
                new AccountViewModel
                {
                    Id = 13,
                    Username = "customer03",
                    FullName = "Lê Văn Đức",
                    PersonType = "Khách hàng",
                    IsActive = false,
                    CreatedAt = DateTime.Now.AddDays(-20),
                    LastLogin = DateTime.Now.AddDays(-10)
                },
                new AccountViewModel
                {
                    Id = 14,
                    Username = "vip_customer",
                    FullName = "Phạm Thị VIP",
                    PersonType = "Khách hàng",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-18),
                    LastLogin = DateTime.Now.AddHours(-1)
                },
                
                // Customer service accounts
                new AccountViewModel
                {
                    Id = 15,
                    Username = "cs_support01",
                    FullName = "Nguyễn Văn Hỗ Trợ",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-15),
                    LastLogin = DateTime.Now.AddMinutes(-30)
                },
                new AccountViewModel
                {
                    Id = 16,
                    Username = "cs_support02",
                    FullName = "Trần Thị Chăm Sóc",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-12),
                    LastLogin = DateTime.Now.AddHours(-2)
                },
                new AccountViewModel
                {
                    Id = 17,
                    Username = "cs_supervisor",
                    FullName = "Lê Văn Giám Sát",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-10),
                    LastLogin = DateTime.Now.AddHours(-3)
                },
                
                // Technical accounts
                new AccountViewModel
                {
                    Id = 18,
                    Username = "tech_support",
                    FullName = "Phạm Văn Kỹ Thuật",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-8),
                    LastLogin = DateTime.Now.AddHours(-5)
                },
                new AccountViewModel
                {
                    Id = 19,
                    Username = "developer",
                    FullName = "Hoàng Thị Dev",
                    PersonType = "Nhân viên",
                    IsActive = false,
                    CreatedAt = DateTime.Now.AddDays(-7),
                    LastLogin = DateTime.Now.AddDays(-3)
                },
                new AccountViewModel
                {
                    Id = 20,
                    Username = "system_admin",
                    FullName = "Nguyễn Văn Hệ Thống",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-5),
                    LastLogin = DateTime.Now.AddHours(-1)
                },
                
                // More customers
                new AccountViewModel
                {
                    Id = 21,
                    Username = "customer_long",
                    FullName = "Trần Văn Long",
                    PersonType = "Khách hàng",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-4),
                    LastLogin = DateTime.Now.AddHours(-7)
                },
                new AccountViewModel
                {
                    Id = 22,
                    Username = "customer_mai",
                    FullName = "Lê Thị Mai",
                    PersonType = "Khách hàng",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-3),
                    LastLogin = DateTime.Now.AddHours(-9)
                },
                new AccountViewModel
                {
                    Id = 23,
                    Username = "customer_duc",
                    FullName = "Phạm Minh Đức",
                    PersonType = "Khách hàng",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-2),
                    LastLogin = DateTime.Now.AddHours(-4)
                },
                
                // Mixed accounts
                new AccountViewModel
                {
                    Id = 24,
                    Username = "accountant01",
                    FullName = "Hoàng Thị Kế Toán",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-1),
                    LastLogin = DateTime.Now.AddHours(-6)
                },
                new AccountViewModel
                {
                    Id = 25,
                    Username = "finance_manager",
                    FullName = "Nguyễn Văn Tài Chính",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddHours(-12),
                    LastLogin = DateTime.Now.AddHours(-2)
                },
                
                // Inactive accounts
                new AccountViewModel
                {
                    Id = 26,
                    Username = "old_customer01",
                    FullName = "Trần Thị Cũ",
                    PersonType = "Khách hàng",
                    IsActive = false,
                    CreatedAt = DateTime.Now.AddDays(-180),
                    LastLogin = DateTime.Now.AddDays(-30)
                },
                new AccountViewModel
                {
                    Id = 27,
                    Username = "old_employee",
                    FullName = "Lê Văn Nghỉ Việc",
                    PersonType = "Nhân viên",
                    IsActive = false,
                    CreatedAt = DateTime.Now.AddDays(-200),
                    LastLogin = DateTime.Now.AddDays(-45)
                },
                new AccountViewModel
                {
                    Id = 28,
                    Username = "temp_customer",
                    FullName = "Phạm Thị Tạm Thời",
                    PersonType = "Khách hàng",
                    IsActive = false,
                    CreatedAt = DateTime.Now.AddDays(-100),
                    LastLogin = DateTime.Now.AddDays(-60)
                },
                
                // Recent accounts
                new AccountViewModel
                {
                    Id = 29,
                    Username = "new_customer",
                    FullName = "Hoàng Văn Mới",
                    PersonType = "Khách hàng",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddHours(-6),
                    LastLogin = DateTime.Now.AddHours(-1)
                },
                new AccountViewModel
                {
                    Id = 30,
                    Username = "trainee01",
                    FullName = "Nguyễn Thị Thực Tập",
                    PersonType = "Nhân viên",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddHours(-2),
                    LastLogin = null // Chưa đăng nhập lần nào
                }
            };

            UpdatePagination();
            LoadPageData();
        }

        private void FilterAccounts(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Hiển thị tất cả tài khoản
                UpdatePagination();
                LoadPageData();
            }
            else
            {
                // Lọc theo tên đăng nhập
                var filteredAccounts = _allAccounts
                    .Where(a => a.Username.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Accounts.Clear();
                foreach (var account in filteredAccounts)
                {
                    Accounts.Add(account);
                }

                UpdateRecordCount(filteredAccounts.Count, _allAccounts.Count);
            }
        }

        private void RefreshData()
        {
            // TODO: Load data from database
            // Hiện tại chỉ refresh dữ liệu mẫu
            LoadSampleData();
        }

        private void UpdatePagination()
        {
            _totalPages = (int)Math.Ceiling((double)_allAccounts.Count / _itemsPerPage);
            if (_totalPages == 0) _totalPages = 1;
            
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            
            PageInfoText.Text = $"Trang {_currentPage} / {_totalPages}";
        }

        private void LoadPageData()
        {
            var skip = (_currentPage - 1) * _itemsPerPage;
            var pageData = _allAccounts.Skip(skip).Take(_itemsPerPage).ToList();

            Accounts.Clear();
            foreach (var account in pageData)
            {
                Accounts.Add(account);
            }

            UpdateRecordCount(pageData.Count, _allAccounts.Count);
        }

        private void UpdateRecordCount(int displayed, int total)
        {
            RecordCountText.Text = $"Hiển thị {displayed} / {total} tài khoản";
        }

        private async void ShowNotImplementedMessage(string feature)
        {
            var dialog = new ContentDialog
            {
                Title = "Thông báo",
                Content = $"Tính năng '{feature}' chưa được triển khai.",
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        #endregion

        private void AddAccountButton_Holding(object sender, Microsoft.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            

        }
    }

    #region ViewModel Classes

    public class AccountViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PersonType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        
        // Properties for UI display
        public string StatusText => IsActive ? "Activate" : "Deactivated";
        public string StatusColor => IsActive ? "#28a745" : "#dc3545";
        public string StatusIcon => IsActive ? "\uE8BB" : "\uE711";
// E8BB = CheckMark (active), E711 = Block/Close (inactive)

        public string CreatedAtText => CreatedAt.ToString("dd/MM/yyyy");

        public string LastLoginText => LastLogin?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa đăng nhập";
        public string PersonTypeColor => PersonType == "Nhân viên" ? "#007bff" : "#17a2b8";
    }

    #endregion
}