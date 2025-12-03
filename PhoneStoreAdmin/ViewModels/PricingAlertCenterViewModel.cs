using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PhoneStoreAdmin.ViewModels
{
    /// <summary>
    /// ViewModel for Pricing Alert Center Page
    /// Binds alerts list and selected alert detail
    /// Requirements: 5.1, 5.2
    /// </summary>
    public class PricingAlertCenterViewModel : INotifyPropertyChanged
    {
        private PricingAlertItemViewModel? _selectedAlert;
        private bool _isLoading;
        private bool _hasAlerts;

        public ObservableCollection<PricingAlertItemViewModel> Alerts { get; set; } = new();

        public PricingAlertItemViewModel? SelectedAlert
        {
            get => _selectedAlert;
            set
            {
                _selectedAlert = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelection));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool HasAlerts
        {
            get => _hasAlerts;
            set { _hasAlerts = value; OnPropertyChanged(); }
        }

        public bool HasSelection => _selectedAlert != null;

        /// <summary>
        /// Load alerts from service data
        /// </summary>
        public void LoadAlerts(IReadOnlyList<PricingAlertViewModel> alerts)
        {
            Alerts.Clear();
            foreach (var alert in alerts)
            {
                Alerts.Add(new PricingAlertItemViewModel(alert));
            }
            HasAlerts = Alerts.Count > 0;
        }

        /// <summary>
        /// Remove alert from list after resolution
        /// </summary>
        public void RemoveAlert(int alertId)
        {
            for (int i = Alerts.Count - 1; i >= 0; i--)
            {
                if (Alerts[i].AlertId == alertId)
                {
                    Alerts.RemoveAt(i);
                    break;
                }
            }
            HasAlerts = Alerts.Count > 0;
            
            if (SelectedAlert?.AlertId == alertId)
            {
                SelectedAlert = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ViewModel for individual alert item in the list
    /// Provides display formatting for DataGrid binding
    /// </summary>
    public class PricingAlertItemViewModel : INotifyPropertyChanged
    {
        public int AlertId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public decimal VariancePercent { get; set; }
        public decimal CostFifo { get; set; }
        public decimal CostNifo { get; set; }
        public int CurrentStock { get; set; }
        public decimal PotentialLoss { get; set; }
        public AlertStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Display properties for DataGrid binding
        public string VarianceDisplay => $"{VariancePercent:F2}%";
        public string CostFifoDisplay => $"{CostFifo:N0}đ";
        public string CostNifoDisplay => $"{CostNifo:N0}đ";
        public string PotentialLossDisplay => $"-{PotentialLoss:N0}đ";
        public string CreatedAtDisplay => CreatedAt.ToString("dd/MM/yyyy");

        public string StatusDisplay => Status switch
        {
            AlertStatus.PENDING => "Chờ xử lý",
            AlertStatus.RESOLVED_HOLD => "Đã giữ giá",
            AlertStatus.RESOLVED_CLEARANCE => "Đã xả hàng",
            _ => "Không xác định"
        };

        public PricingAlertItemViewModel() { }

        public PricingAlertItemViewModel(PricingAlertViewModel alert)
        {
            AlertId = alert.AlertId;
            ProductId = alert.ProductId;
            ProductName = alert.ProductName;
            ProductSku = alert.ProductSku;
            VariancePercent = alert.VariancePercent;
            CostFifo = alert.CostFifo;
            CostNifo = alert.CostNifo;
            CurrentStock = alert.CurrentStock;
            PotentialLoss = alert.PotentialLoss;
            Status = alert.Status;
            CreatedAt = alert.CreatedAt;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
