# Hướng Dẫn Tạo Simple Page
---

## 🏗️ Cấu Trúc Layout Mục Tiêu

```
┌─────────────────────────────────────────────────┐
│ Header Section                                  │
│ ┌─────────────────┐  ┌──────────────────┐     │
│ │ Title + Desc    │  │  [+ Thêm mới]   │     │
│ └─────────────────┘  └──────────────────┘     │
└─────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────┐
│ Table Card                                      │
│ ├─ Search & Filter Toolbar                     │
│ ├─ Table Header                                │
│ ├─ Table Rows (ListView)                       │
│ └─ Pagination Footer                           │
└─────────────────────────────────────────────────┘
```

---

## 📝 Các Bước Thực Hiện

### Bước 1: Tạo File XAML Mới

Tạo file `YourPage.xaml` trong thư mục `View/`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="PhoneStoreAdmin.View.YourPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Background="{StaticResource BrushBackground}">
    
    <!-- Phần Resources và Grid chính sẽ thêm ở bước sau -->
</Page>
```

---

### Bước 2: Thêm Page Resources

Copy các styles cần thiết vào `<Page.Resources>`:

```xml
<Page.Resources>
    <!-- Shadow -->
    <ThemeShadow x:Name="SharedShadow" />

    <!-- Card Style -->
    <Style x:Key="CardStyle" TargetType="Border">
        <Setter Property="Background">
            <Setter.Value>
                <SolidColorBrush Color="{StaticResource ColorSurface}" Opacity="0.95"/>
            </Setter.Value>
        </Setter>
        <Setter Property="BorderBrush" Value="{StaticResource BrushBorder}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="12"/>
    </Style>

    <!-- Primary Button Style -->
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource BrushPrimary}"/>
        <Setter Property="Foreground" Value="{StaticResource BrushOnPrimary}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="CornerRadius" Value="8"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Padding" Value="20,14"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Grid>
                        <Border x:Name="RootBorder"
                                Background="{TemplateBinding Background}"
                                CornerRadius="{TemplateBinding CornerRadius}">
                            <ContentPresenter
                                HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                                VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
                                Content="{TemplateBinding Content}"
                                ContentTemplate="{TemplateBinding ContentTemplate}"
                                Margin="{TemplateBinding Padding}"
                                Foreground="{TemplateBinding Foreground}"/>
                        </Border>
                        <VisualStateManager.VisualStateGroups>
                            <VisualStateGroup x:Name="CommonStates">
                                <VisualState x:Name="Normal"/>
                                <VisualState x:Name="PointerOver">
                                    <Storyboard>
                                        <ObjectAnimationUsingKeyFrames Storyboard.TargetName="RootBorder"
                                                                      Storyboard.TargetProperty="Background">
                                            <DiscreteObjectKeyFrame KeyTime="0"
                                                                   Value="{StaticResource BrushPrimaryHover}"/>
                                        </ObjectAnimationUsingKeyFrames>
                                    </Storyboard>
                                </VisualState>
                                <VisualState x:Name="Pressed">
                                    <Storyboard>
                                        <ObjectAnimationUsingKeyFrames Storyboard.TargetName="RootBorder"
                                                                      Storyboard.TargetProperty="Background">
                                            <DiscreteObjectKeyFrame KeyTime="0"
                                                                   Value="{StaticResource BrushPrimaryPressed}"/>
                                        </ObjectAnimationUsingKeyFrames>
                                    </Storyboard>
                                </VisualState>
                                <VisualState x:Name="Disabled">
                                    <Storyboard>
                                        <DoubleAnimation Storyboard.TargetName="RootBorder"
                                                        Storyboard.TargetProperty="Opacity"
                                                        To="0.6"
                                                        Duration="0:0:0"/>
                                    </Storyboard>
                                </VisualState>
                            </VisualStateGroup>
                        </VisualStateManager.VisualStateGroups>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Secondary Button Style -->
    <Style x:Key="SecondaryButtonStyle" TargetType="Button">
        <Setter Property="Background">
            <Setter.Value>
                <SolidColorBrush Color="{StaticResource ColorSurface}"/>
            </Setter.Value>
        </Setter>
        <Setter Property="BorderBrush" Value="{StaticResource BrushBorder}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="8"/>
        <Setter Property="FontWeight" Value="Medium"/>
    </Style>
</Page.Resources>
```

---

### Bước 3: Tạo Grid Layout Chính

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>  <!-- Header -->
        <RowDefinition Height="*"/>     <!-- Table -->
    </Grid.RowDefinitions>

    <!-- Background Gradient -->
    <Grid Grid.RowSpan="2">
        <Rectangle>
            <Rectangle.Fill>
                <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                    <GradientStop Color="{StaticResource ColorBackground}" Offset="0"/>
                    <GradientStop Color="#FFF0F2FF" Offset="0.5"/>
                    <GradientStop Color="#FFE8ECFF" Offset="1"/>
                </LinearGradientBrush>
            </Rectangle.Fill>
        </Rectangle>

        <!-- Decorative Elements -->
        <Grid>
            <Ellipse Width="400" Height="400" 
                    HorizontalAlignment="Left" VerticalAlignment="Top"
                    Margin="-200,-100,0,0"
                    Opacity="0.1">
                <Ellipse.Fill>
                    <RadialGradientBrush>
                        <GradientStop Color="{StaticResource ColorPrimary}" Offset="0"/>
                        <GradientStop Color="{StaticResource ColorPrimaryLight}" Offset="1"/>
                    </RadialGradientBrush>
                </Ellipse.Fill>
            </Ellipse>

            <Ellipse Width="300" Height="300" 
                    HorizontalAlignment="Right" VerticalAlignment="Bottom"
                    Margin="0,0,-150,-150"
                    Opacity="0.1">
                <Ellipse.Fill>
                    <RadialGradientBrush>
                        <GradientStop Color="{StaticResource ColorAccent}" Offset="0"/>
                        <GradientStop Color="Transparent" Offset="1"/>
                    </RadialGradientBrush>
                </Ellipse.Fill>
            </Ellipse>
        </Grid>
    </Grid>

    <!-- Thêm Header và Table ở các bước tiếp theo -->
</Grid>
```

---

### Bước 4: Thêm Header Section

```xml
<!-- Header Section với Button "Thêm mới" -->
<Border Grid.Row="0" 
        Margin="32,32,32,24"
        Style="{StaticResource CardStyle}"
        Shadow="{StaticResource SharedShadow}">
    <Grid Padding="32,24">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- Title & Description -->
        <StackPanel Grid.Column="0"
                   Orientation="Vertical"
                   Spacing="8">
            <TextBlock x:Uid="YourPage_Title"
                      Text="Tiêu Đề Trang"
                      FontSize="36"
                      FontWeight="Bold">
                <TextBlock.Foreground>
                    <LinearGradientBrush StartPoint="0,0" EndPoint="1,0">
                        <GradientStop Color="{StaticResource ColorPrimary}" Offset="0"/>
                        <GradientStop Color="{StaticResource ColorAccent}" Offset="1"/>
                    </LinearGradientBrush>
                </TextBlock.Foreground>
            </TextBlock>
            <TextBlock x:Uid="YourPage_Description"
                      Text="Mô tả chức năng của trang"
                      FontSize="16"
                      Foreground="{StaticResource BrushTextSecondary}"/>
        </StackPanel>

        <!-- Add Button -->
        <Button x:Name="AddButton"
                Grid.Column="1"
                Padding="20,14"
                VerticalAlignment="Center"
                Click="AddButton_Click"
                Shadow="{StaticResource SharedShadow}"
                Style="{StaticResource PrimaryButtonStyle}">
            <StackPanel Orientation="Horizontal" Spacing="10">
                <FontIcon FontSize="18" Glyph="&#xE710;"/>
                <TextBlock x:Uid="YourPage_AddButton"
                          Text="Thêm mới"
                          FontSize="15"/>
            </StackPanel>
        </Button>
    </Grid>
</Border>
```

---

### Bước 5: Thêm Table Section (Toàn Bộ)

```xml
<!-- Table Card -->
<Border Grid.Row="1" 
        Margin="32,0,32,32"
        Style="{StaticResource CardStyle}"
        Shadow="{StaticResource SharedShadow}"
        Padding="0">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Toolbar -->
            <RowDefinition Height="Auto"/>  <!-- Table Header -->
            <RowDefinition Height="*"/>     <!-- Table Content -->
            <RowDefinition Height="Auto"/>  <!-- Pagination -->
        </Grid.RowDefinitions>

        <!-- Row 0: Toolbar (Search + Filter + Refresh) -->
        <Border Grid.Row="0" 
                BorderBrush="{StaticResource BrushBorder}"
                BorderThickness="0,0,0,1"
                Padding="24,16"
                CornerRadius="12,12,0,0">
            <Border.Background>
                <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                    <GradientStop Color="#FFFAFBFF" Offset="0"/>
                    <GradientStop Color="#FFF5F7FF" Offset="1"/>
                </LinearGradientBrush>
            </Border.Background>
            
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>  <!-- SearchBox -->
                    <ColumnDefinition Width="*"/>     <!-- Spacer -->
                    <ColumnDefinition Width="Auto"/>  <!-- Buttons -->
                </Grid.ColumnDefinitions>

                <!-- Search Box -->
                <AutoSuggestBox x:Name="SearchBox"
                               x:Uid="YourPage_SearchBox"
                               PlaceholderText="Tìm kiếm..."
                               Grid.Column="0"
                               QueryIcon="Find"
                               Background="{StaticResource BrushBackground}"
                               BorderBrush="{StaticResource BrushBorder}"
                               BorderThickness="1"
                               CornerRadius="8"
                               Padding="12,8"
                               FontSize="14"
                               Width="400"
                               VerticalContentAlignment="Center"
                               TextChanged="SearchBox_TextChanged"/>

                <!-- Filter & Refresh Buttons -->
                <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="8">
                    <Button x:Name="FilterButton"
                            x:Uid="YourPage_FilterButton"
                            Style="{StaticResource SecondaryButtonStyle}"
                            Padding="10,8"
                            Click="FilterButton_Click">
                        <FontIcon Glyph="&#xE16E;" FontSize="16" 
                                 Foreground="{StaticResource BrushText}"/>
                    </Button>

                    <Button x:Name="RefreshButton"
                            x:Uid="YourPage_RefreshButton"
                            Style="{StaticResource SecondaryButtonStyle}"
                            Padding="10,8"
                            Click="RefreshButton_Click">
                        <FontIcon Glyph="&#xE72C;" FontSize="16" 
                                 Foreground="{StaticResource BrushText}"/>
                    </Button>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Row 1: Table Header -->
        <Border Grid.Row="1" 
                BorderBrush="{StaticResource BrushBorder}"
                BorderThickness="0,0,0,1"
                Padding="32,20">
            <Border.Background>
                <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                    <GradientStop Color="#FFFAFBFF" Offset="0"/>
                    <GradientStop Color="#FFF5F7FF" Offset="1"/>
                </LinearGradientBrush>
            </Border.Background>

            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="100"/>      <!-- ID -->
                    <ColumnDefinition Width="*" MinWidth="150"/>  <!-- Column 1 -->
                    <ColumnDefinition Width="*" MinWidth="150"/>  <!-- Column 2 -->
                    <ColumnDefinition Width="*" MinWidth="150"/>  <!-- Column 3 -->
                    <ColumnDefinition Width="180"/>      <!-- Status -->
                    <ColumnDefinition Width="80"/>       <!-- Actions -->
                </Grid.ColumnDefinitions>

                <TextBlock x:Uid="YourPage_Header_ID" 
                          Text="ID"
                          Grid.Column="0" 
                          Foreground="{StaticResource BrushTextSecondary}" 
                          FontWeight="SemiBold"
                          FontSize="13"
                          Padding="28,0,0,0"
                          VerticalAlignment="Center"/>
                
                <TextBlock x:Uid="YourPage_Header_Col1" 
                          Text="Cột 1"
                          Grid.Column="1" 
                          Foreground="{StaticResource BrushTextSecondary}" 
                          FontWeight="SemiBold"
                          FontSize="13"
                          VerticalAlignment="Center"/>
                
                <TextBlock x:Uid="YourPage_Header_Col2" 
                          Text="Cột 2"
                          Grid.Column="2" 
                          Foreground="{StaticResource BrushTextSecondary}" 
                          FontWeight="SemiBold"
                          FontSize="13"
                          VerticalAlignment="Center"/>
                
                <TextBlock x:Uid="YourPage_Header_Col3" 
                          Text="Cột 3"
                          Grid.Column="3" 
                          Foreground="{StaticResource BrushTextSecondary}" 
                          FontWeight="SemiBold"
                          FontSize="13"
                          VerticalAlignment="Center"/>
                
                <TextBlock x:Uid="YourPage_Header_Status" 
                          Text="Trạng thái"
                          Grid.Column="4" 
                          Foreground="{StaticResource BrushTextSecondary}" 
                          FontWeight="SemiBold"
                          FontSize="13"
                          VerticalAlignment="Center"/>
                
                <TextBlock x:Uid="YourPage_Header_Actions" 
                          Text="Thao tác"
                          Grid.Column="5" 
                          Foreground="{StaticResource BrushTextSecondary}" 
                          FontWeight="SemiBold"
                          FontSize="13"
                          VerticalAlignment="Center"
                          HorizontalAlignment="Center"/>
            </Grid>
        </Border>

        <!-- Row 2: Table Content (ListView) -->
        <ScrollViewer Grid.Row="2" 
                     VerticalScrollBarVisibility="Auto"
                     HorizontalScrollBarVisibility="Hidden">
            <ListView x:Name="DataListView"
                     Background="Transparent"
                     SelectionMode="None"
                     ItemsSource="{x:Bind Items, Mode=OneWay}">
                <ListView.ItemContainerStyle>
                    <Style TargetType="ListViewItem">
                        <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
                        <Setter Property="Padding" Value="0"/>
                        <Setter Property="Margin" Value="0"/>
                        <Setter Property="Background" Value="Transparent"/>
                        <Setter Property="Template">
                            <Setter.Value>
                                <ControlTemplate TargetType="ListViewItem">
                                    <Border x:Name="ItemBorder"
                                           Background="{TemplateBinding Background}"
                                           BorderBrush="{StaticResource BrushBorder}"
                                           BorderThickness="0,0,0,1"
                                           Padding="32,20">
                                        <Grid>
                                            <Rectangle x:Name="HoverBackground" 
                                                      Fill="Transparent"
                                                      Opacity="0"/>
                                            <ContentPresenter/>
                                        </Grid>
                                        <VisualStateManager.VisualStateGroups>
                                            <VisualStateGroup x:Name="CommonStates">
                                                <VisualState x:Name="Normal">
                                                    <Storyboard>
                                                        <DoubleAnimation Storyboard.TargetName="HoverBackground"
                                                                       Storyboard.TargetProperty="Opacity"
                                                                       To="0"
                                                                       Duration="0:0:0.2"/>
                                                    </Storyboard>
                                                </VisualState>
                                                <VisualState x:Name="PointerOver">
                                                    <Storyboard>
                                                        <DoubleAnimation Storyboard.TargetName="HoverBackground"
                                                                       Storyboard.TargetProperty="Opacity"
                                                                       To="0.03"
                                                                       Duration="0:0:0.2"/>
                                                        <ObjectAnimationUsingKeyFrames Storyboard.TargetName="HoverBackground"
                                                                                     Storyboard.TargetProperty="Fill">
                                                            <DiscreteObjectKeyFrame KeyTime="0" Value="{StaticResource BrushPrimary}"/>
                                                        </ObjectAnimationUsingKeyFrames>
                                                    </Storyboard>
                                                </VisualState>
                                            </VisualStateGroup>
                                        </VisualStateManager.VisualStateGroups>
                                    </Border>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                </ListView.ItemContainerStyle>

                <ListView.ItemTemplate>
                    <DataTemplate>
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="100"/>
                                <ColumnDefinition Width="*" MinWidth="150"/>
                                <ColumnDefinition Width="*" MinWidth="150"/>
                                <ColumnDefinition Width="*" MinWidth="150"/>
                                <ColumnDefinition Width="180"/>
                                <ColumnDefinition Width="80"/>
                            </Grid.ColumnDefinitions>

                            <!-- ID Badge -->
                            <Border Grid.Column="0" 
                                   Background="{StaticResource BrushPrimaryLight}"
                                   Width="70" Height="32"
                                   CornerRadius="8"
                                   HorizontalAlignment="Left">
                                <TextBlock Text="{Binding Id}"
                                          VerticalAlignment="Center"
                                          HorizontalAlignment="Center"
                                          FontWeight="Bold"
                                          FontSize="14"
                                          Foreground="{StaticResource BrushPrimary}"/>
                            </Border>

                            <!-- Column 1 -->
                            <TextBlock Grid.Column="1" 
                                      Text="{Binding Column1}"
                                      VerticalAlignment="Center"
                                      FontWeight="SemiBold"
                                      FontSize="14"
                                      Foreground="{StaticResource BrushText}"/>

                            <!-- Column 2 -->
                            <TextBlock Grid.Column="2" 
                                      Text="{Binding Column2}"
                                      VerticalAlignment="Center"
                                      FontSize="14"
                                      Foreground="{StaticResource BrushText}"/>

                            <!-- Column 3 -->
                            <TextBlock Grid.Column="3" 
                                      Text="{Binding Column3}"
                                      VerticalAlignment="Center"
                                      FontSize="14"
                                      Foreground="{StaticResource BrushText}"/>

                            <!-- Status Badge -->
                            <Border Grid.Column="4" 
                                   VerticalAlignment="Center"
                                   HorizontalAlignment="Left"
                                   Padding="14,8"
                                   CornerRadius="16"
                                   Background="{Binding StatusColor}">
                                <TextBlock Text="{Binding StatusText}"
                                          Foreground="White"
                                          FontSize="12"
                                          FontWeight="SemiBold"/>
                            </Border>

                            <!-- Actions Button -->
                            <Button Grid.Column="5"
                                    Background="Transparent"
                                    BorderThickness="0"
                                    VerticalAlignment="Center"
                                    HorizontalAlignment="Center"
                                    Width="36" Height="36"
                                    CornerRadius="8"
                                    Click="ActionsButton_Click"
                                    Tag="{Binding}">
                                <FlyoutBase.AttachedFlyout>
                                    <MenuFlyout>
                                        <MenuFlyoutItem x:Uid="YourPage_EditAction"
                                                       Text="Chỉnh sửa"
                                                       Tag="{Binding}"
                                                       Click="EditMenuItem_Click">
                                            <MenuFlyoutItem.Icon>
                                                <FontIcon Glyph="&#xE70F;"/>
                                            </MenuFlyoutItem.Icon>
                                        </MenuFlyoutItem>
                                        <MenuFlyoutItem x:Uid="YourPage_DeleteAction"
                                                       Text="Xóa"
                                                       Tag="{Binding}"
                                                       Click="DeleteMenuItem_Click">
                                            <MenuFlyoutItem.Icon>
                                                <FontIcon Glyph="&#xE74D;"/>
                                            </MenuFlyoutItem.Icon>
                                        </MenuFlyoutItem>
                                    </MenuFlyout>
                                </FlyoutBase.AttachedFlyout>
                                <FontIcon Glyph="&#xE712;"
                                         FontSize="16"
                                         Foreground="{StaticResource BrushTextSecondary}"/>
                            </Button>
                        </Grid>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </ScrollViewer>

        <!-- Empty State -->
        <Grid x:Name="EmptyStatePanel"
              Grid.Row="2"
              Padding="60"
              Visibility="Collapsed">
            <StackPanel HorizontalAlignment="Center"
                       VerticalAlignment="Center"
                       Spacing="24">
                <Border Width="80" Height="80"
                       Background="{StaticResource BrushPrimaryLight}"
                       CornerRadius="40">
                    <FontIcon FontSize="32"
                             Foreground="{StaticResource BrushPrimary}"
                             Glyph="&#xE77B;"/>
                </Border>
                <StackPanel HorizontalAlignment="Center" Spacing="8">
                    <TextBlock x:Uid="YourPage_EmptyTitle"
                              Text="Không tìm thấy dữ liệu"
                              HorizontalAlignment="Center"
                              FontSize="24"
                              FontWeight="SemiBold"
                              Foreground="{StaticResource BrushText}"/>
                    <TextBlock x:Uid="YourPage_EmptyMessage"
                              Text="Thử điều chỉnh bộ lọc hoặc thêm mới"
                              HorizontalAlignment="Center"
                              FontSize="16"
                              Foreground="{StaticResource BrushTextSecondary}"/>
                </StackPanel>
            </StackPanel>
        </Grid>

        <!-- Row 3: Pagination Footer -->
        <Border Grid.Row="3" 
                BorderBrush="{StaticResource BrushBorder}"
                BorderThickness="0,1,0,0"
                Padding="32,20"
                CornerRadius="0,0,12,12">
            <Border.Background>
                <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                    <GradientStop Color="#FFF5F7FF" Offset="0"/>
                    <GradientStop Color="#FFFAFBFF" Offset="1"/>
                </LinearGradientBrush>
            </Border.Background>

            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- Record Count -->
                <StackPanel Grid.Column="0" 
                           Orientation="Horizontal"
                           Spacing="16"
                           VerticalAlignment="Center">
                    <Border Background="{StaticResource BrushPrimaryLight}"
                           CornerRadius="16"
                           Padding="16,8">
                        <TextBlock x:Name="RecordCountText"
                                  Text="Hiển thị 0 bản ghi"
                                  Foreground="{StaticResource BrushPrimary}"
                                  FontSize="14"
                                  FontWeight="SemiBold"/>
                    </Border>
                </StackPanel>

                <!-- Pagination Controls -->
                <StackPanel Grid.Column="1" 
                           Orientation="Horizontal" 
                           Spacing="12">
                    <Button x:Name="PreviousPageButton"
                           Style="{StaticResource SecondaryButtonStyle}"
                           Width="40" Height="40"
                           Padding="0"
                           IsEnabled="False"
                           Click="PreviousPageButton_Click">
                        <FontIcon Glyph="&#xE760;" FontSize="16"/>
                    </Button>

                    <Border Background="{StaticResource BrushPrimaryLight}"
                           CornerRadius="20"
                           Padding="20,10"
                           VerticalAlignment="Center">
                        <TextBlock x:Name="PageInfoText" 
                                  Text="Trang 1 / 1"
                                  Foreground="{StaticResource BrushPrimary}"
                                  FontSize="14"
                                  FontWeight="Bold"/>
                    </Border>

                    <Button x:Name="NextPageButton"
                           Style="{StaticResource SecondaryButtonStyle}"
                           Width="40" Height="40"
                           Padding="0"
                           IsEnabled="False"
                           Click="NextPageButton_Click">
                        <FontIcon Glyph="&#xE761;" FontSize="16"/>
                    </Button>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Border>
```

---

### Bước 6: Tạo Code-Behind File

Tạo file `YourPage.xaml.cs`:

```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace PhoneStoreAdmin.View
{
    public sealed partial class YourPage : Page
    {
        // Data collection
        public ObservableCollection<YourDataModel> Items { get; set; }
        
        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;

        public YourPage()
        {
            this.InitializeComponent();
            Items = new ObservableCollection<YourDataModel>();
            LoadData();
        }

        private async void LoadData()
        {
            // TODO: Implement data loading logic
            // Items.Clear();
            // var data = await YourRepository.GetAllAsync();
            // foreach (var item in data)
            // {
            //     Items.Add(item);
            // }
            UpdateUI();
        }

        private void UpdateUI()
        {
            // Update record count
            RecordCountText.Text = $"Hiển thị {Items.Count} bản ghi";
            
            // Update pagination
            PageInfoText.Text = $"Trang {_currentPage} / {_totalPages}";
            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            
            // Show/hide empty state
            if (Items.Count == 0)
            {
                DataListView.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
            else
            {
                DataListView.Visibility = Visibility.Visible;
                EmptyStatePanel.Visibility = Visibility.Collapsed;
            }
        }

        // Header Button
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open add dialog or navigate to add page
        }

        // Toolbar Actions
        private void SearchBox_TextChanged(AutoSuggestBox sender, 
            AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchText = sender.Text.ToLower();
                // TODO: Implement search filter
                // Filter Items based on searchText
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show filter dialog
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        // Table Actions
        private void ActionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                FlyoutBase.ShowAttachedFlyout(button);
            }
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && 
                menuItem.Tag is YourDataModel item)
            {
                // TODO: Open edit dialog or navigate to edit page
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && 
                menuItem.Tag is YourDataModel item)
            {
                // TODO: Show confirmation dialog and delete
            }
        }

        // Pagination
        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadData();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadData();
            }
        }
    }

    // Data Model (placeholder - thay thế bằng model thực tế)
    public class YourDataModel
    {
        public int Id { get; set; }
        public string Column1 { get; set; } = string.Empty;
        public string Column2 { get; set; } = string.Empty;
        public string Column3 { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public Microsoft.UI.Xaml.Media.Brush StatusColor { get; set; }
    }
}
```

---

## 🎯 Checklist Triển Khai

### ✅ XAML Structure
- [ ] Page declaration với namespace
- [ ] Page.Resources với styles (CardStyle, PrimaryButtonStyle, SecondaryButtonStyle)
- [ ] Grid layout chính (2 rows: Header, Table)
- [ ] Background gradient + decorative elements
- [ ] Header section với button "Thêm"
- [ ] Table card với 4 phần:
  - [ ] Toolbar (Search + Filter + Refresh)
  - [ ] Table Header (Grid columns)
  - [ ] Table Content (ListView)
  - [ ] Pagination Footer
- [ ] Empty state panel

### ✅ Code-Behind
- [ ] ObservableCollection<YourDataModel> cho data binding
- [ ] LoadData() method
- [ ] UpdateUI() method (record count, pagination, empty state)
- [ ] AddButton_Click handler
- [ ] SearchBox_TextChanged handler
- [ ] FilterButton_Click handler
- [ ] RefreshButton_Click handler
- [ ] ActionsButton_Click handler
- [ ] EditMenuItem_Click handler
- [ ] DeleteMenuItem_Click handler
- [ ] PreviousPageButton_Click handler
- [ ] NextPageButton_Click handler

### ✅ Localization (Resources/Strings)
- [ ] YourPage_Title
- [ ] YourPage_Description
- [ ] YourPage_AddButton
- [ ] YourPage_SearchBox
- [ ] YourPage_FilterButton
- [ ] YourPage_RefreshButton
- [ ] YourPage_Header_ID
- [ ] YourPage_Header_Col1
- [ ] YourPage_Header_Col2
- [ ] YourPage_Header_Col3
- [ ] YourPage_Header_Status
- [ ] YourPage_Header_Actions
- [ ] YourPage_EditAction
- [ ] YourPage_DeleteAction
- [ ] YourPage_EmptyTitle
- [ ] YourPage_EmptyMessage

---

## 🔧 Tùy Chỉnh

### Thay Đổi Table Columns

1. **Cập nhật Grid.ColumnDefinitions** trong Table Header (Bước 5, Row 1):
   ```xml
   <Grid.ColumnDefinitions>
       <ColumnDefinition Width="100"/>      <!-- ID -->
       <ColumnDefinition Width="*" MinWidth="200"/>  <!-- Tên -->
       <ColumnDefinition Width="*" MinWidth="150"/>  <!-- Email -->
       <!-- Thêm columns khác -->
   </Grid.ColumnDefinitions>
   ```

2. **Cập nhật Header TextBlocks**:
   ```xml
   <TextBlock x:Uid="YourPage_Header_Name" 
              Text="Tên"
              Grid.Column="1"
              .../>
   ```

3. **Cập nhật ItemTemplate**:
   ```xml
   <TextBlock Grid.Column="1" 
              Text="{Binding Name}"
              .../>
   ```

4. **Cập nhật Data Model**:
   ```csharp
   public class YourDataModel
   {
       public int Id { get; set; }
       public string Name { get; set; }
       public string Email { get; set; }
       // ...
   }
   ```

### Thêm Filter Panel Mở Rộng

Chèn sau Toolbar (Grid.Row="0"), thêm FilterPanel như SupplierPage:

```xml
<Border x:Name="FilterPanel"
        Grid.Row="1"
        BorderBrush="{StaticResource BrushBorder}"
        BorderThickness="0,0,0,1"
        Padding="24,20"
        Visibility="Collapsed">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        
        <!-- Filter fields -->
    </Grid>
</Border>
```

**Lưu ý**: Phải cập nhật Grid.RowDefinitions thêm 1 row cho FilterPanel.

### Thêm Actions Menu Items

```xml
<MenuFlyout>
    <MenuFlyoutItem Text="Chi tiết" Click="DetailMenuItem_Click">
        <MenuFlyoutItem.Icon>
            <FontIcon Glyph="&#xE8A7;"/>
        </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>
    
    <MenuFlyoutItem Text="Chỉnh sửa" Click="EditMenuItem_Click">
        <MenuFlyoutItem.Icon>
            <FontIcon Glyph="&#xE70F;"/>
        </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>
    
    <MenuFlyoutSeparator/>
    
    <MenuFlyoutItem Text="Xóa" Click="DeleteMenuItem_Click">
        <MenuFlyoutItem.Icon>
            <FontIcon Glyph="&#xE74D;"/>
        </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>
</MenuFlyout>
```

---

## 📚 Tham Khảo

- **SuppliersPage.xaml**: Header section, filter panel
- **AccountsPage.xaml**: Table structure, toolbar, pagination
- **Theme/Colors.xaml**: Color resources
- **copilot-instructions.md**: Project conventions

---

## ⚠️ Lưu Ý Quan Trọng

1. **Naming Convention**: 
   - Đặt tên Page theo nghiệp vụ (VD: `ProductsPage`, `OrdersPage`, `CustomersPage`)
   - x:Name elements theo pattern: `[Element][Purpose]` (VD: `AddButton`, `SearchBox`, `DataListView`)

2. **Localization**: 
   - **BẮT BUỘC** dùng `x:Uid` cho tất cả text hiển thị
   - Tạo string resources trong `Resources/Strings/en-US/` và `vi-VN/`
   - Format: `[PageName]_[ElementPurpose]`

3. **Data Binding**:
   - Dùng `x:Bind` thay vì `Binding` cho performance
   - `Mode=OneWay` cho readonly data
   - `ObservableCollection` cho dynamic updates
   - Implement `INotifyPropertyChanged` nếu cần two-way binding

4. **Styling**:
   - Tái sử dụng styles: `CardStyle`, `PrimaryButtonStyle`, `SecondaryButtonStyle`
   - Consistent spacing: **32px** margin ngoài, **24px** padding trong
   - CornerRadius: **12px** cho cards, **8px** cho buttons/inputs

5. **Empty State**:
   - Luôn có empty state khi không có dữ liệu
   - Toggle visibility giữa `DataListView` và `EmptyStatePanel`

6. **Accessibility**:
   - Thêm `ToolTipService.ToolTip` cho buttons không có text
   - Sử dụng semantic icons (`FontIcon`)
   - Proper focus management

7. **Performance**:
   - Virtualization trong ListView (mặc định)
   - Pagination thay vì load toàn bộ data
   - Async/await cho I/O operations

---

## 🚀 Kết Quả Mong Đợi

Sau khi hoàn thành, bạn sẽ có một page với:

✅ **Header** có button "Thêm mới" (giống SupplierPage)  
✅ **Single Table** với đầy đủ chức năng (không có tabs)  
✅ **Search & Filter** toolbar  
✅ **Data table** với hover effects  
✅ **Pagination** footer  
✅ **Empty state** khi không có dữ liệu  
✅ **Consistent design** theo project style guide  
❌ **KHÔNG CÓ** Tab Navigation  

---

## 📊 So Sánh với Hybrid Page

| Feature | Hybrid Page | Simple Page |
|---------|------------|-------------|
| **Tab Navigation** | ✅ Có (2+ tabs) | ❌ Không có |
| **Header với Button** | ✅ | ✅ |
| **Search & Filter** | ✅ | ✅ |
| **Data Table** | ✅ | ✅ |
| **Pagination** | ✅ | ✅ |
| **Complexity** | Cao (multiple views) | Thấp (single view) |
| **Use Case** | Nhiều loại dữ liệu liên quan | Một loại dữ liệu duy nhất |
| **Examples** | AccountsPage (Accounts + Employees) | SuppliersPage, ProductsPage |

---

**Tài liệu được tạo**: October 27, 2025  
**Phiên bản**: 1.0  
**Tác giả**: GitHub Copilot  
**Dựa trên**: SuppliersPage.xaml + AccountsPage.xaml (table only)
