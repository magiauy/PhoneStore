# 🎨 Fluent Design System for **WinUI 3** — Practical Guide & Deep Dive

> Mục tiêu: biến nguyên lý của **Fluent Design System** (Windows 11) thành các thực hành **cụ thể cho WinUI 3**. Tài liệu này tập trung vào **màu sắc (tokens)**, **bề mặt (Mica/Acrylic)**, **chuyển động**, **hình dạng & stroke**, **typography**, **iconography**, **a11y**, và **cách triển khai** qua `ResourceDictionary`, `ThemeResource`, `SystemBackdrop`, `Composition`, `VisualStateManager`.

> Phạm vi: WinUI 3 (Windows App SDK), namespace `Microsoft.UI.*`.

---

## 0) Triết lý Fluent (áp dụng vào WinUI)

* **Màu & chủ đề**: dùng **theme tokens** (Light/Dark/HC), **accent** động theo hệ thống; tránh hard-code màu.
* **Bề mặt & chiều sâu**: dùng **Mica** cho nền cấp app, **Acrylic** cho vùng nổi/điều hướng phụ (khi phù hợp), **ThemeShadow/DropShadow** để tạo chiều sâu.
* **Hình dạng & stroke**: góc bo **thống nhất**, stroke tương phản đủ; dùng tokens stroke mặc định.
* **Chuyển động có mục đích**: dùng **Transitions**/Composition (không lạm dụng); tôn trọng **Reduce motion**.
* **Typography**: ưu tiên **Segoe UI Variable** (tự động trong WinUI), hierarchy gọn; hỗ trợ **TextScaleFactor**.
* **A11y**: đảm bảo **contrast**, focus rõ ràng, **keyboard nav** đầy đủ, **High Contrast** ok.

---

## 1) Color System & Tokens trong WinUI 3

### 1.1 Dùng ThemeResource thay vì màu cứng

* Các brush/tokens hệ thống thường dùng (Windows 11):

  * **Text**: `TextFillColorPrimaryBrush`, `TextFillColorSecondaryBrush`, `TextFillColorDisabledBrush`
  * **Fill**: `SolidBackgroundFillColorBaseBrush`, `SubtleFillColorSecondaryBrush`, `ControlStrongFillColorDefaultBrush`, `ControlFillColorDefaultBrush`
  * **Stroke**: `ControlStrokeColorDefaultBrush`, `CardStrokeColorDefaultBrush`, `DividerStrokeColorDefaultBrush`
  * **Accent**: `AccentFillColorDefaultBrush`, `AccentTextFillColorPrimaryBrush`
* Ưu tiên `{ThemeResource ...}` để tự đổi theo Light/Dark/HC.

### 1.2 Tạo palette app (alias tokens)

```xaml
<Application.Resources>
  <ResourceDictionary>
    <!-- Alias tokens của app trỏ về tokens hệ thống -->
    <SolidColorBrush x:Key="App/Fill/Surface" Color="{ThemeResource SolidBackgroundFillColorBase}"/>
    <SolidColorBrush x:Key="App/Text/Primary" Color="{ThemeResource TextFillColorPrimary}"/>
    <SolidColorBrush x:Key="App/Accent/Fill" Color="{ThemeResource AccentFillColorDefault}"/>
  </ResourceDictionary>
</Application.Resources>
```

> Tạo lớp **alias** giúp đổi toàn bộ scheme về sau chỉ bằng sửa một chỗ.

### 1.3 Accent color

* Tôn trọng accent của hệ thống (Windows Settings). Tránh ép màu thương hiệu ở mọi nơi; chỉ dùng cho điểm nhấn (buttons, sliders, links).

---

## 2) Surfaces: **Mica** & **Acrylic** trong WinUI 3

### 2.1 Mica (nền ứng dụng)

**XAML**

```xaml
<Window ...>
  <Window.SystemBackdrop>
    <MicaBackdrop Kind="Base" />
  </Window.SystemBackdrop>
  <!-- Content here -->
</Window>
```

**C# fallback (khi Mica không khả dụng)**

```csharp
if (this.SystemBackdrop == null)
{
    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Black);
}
```

* **Kind**: `Base` (đa số), `BaseAlt` cho nền phụ/độ tương phản khác.
* Giữ **content nền đơn giản** (không đặt hình phức tạp) để Mica phát huy chiều sâu.

### 2.2 Acrylic (panel nổi/command surfaces)

* Dùng có chừng mực cho **pane**, **context** hoặc overlay. Không dùng làm nền toàn app (ưu tiên Mica).

---

## 3) Elevation & Shadow

### 3.1 ThemeShadow / DropShadow (Composition)

**ThemeShadow** – đơn giản, theo theme:

```xaml
<Grid>
  <Grid.Shadow>
    <ThemeShadow />
  </Grid.Shadow>
</Grid>
```

**Composition DropShadow** – kiểm soát chi tiết:

```csharp
var compositor = Microsoft.UI.Composition.CompositorHelper.CompositorForElement(myGrid);
var sprite = compositor.CreateSpriteVisual();
var ds = compositor.CreateDropShadow();
 ds.BlurRadius = 16f;
 ds.Opacity = 0.3f;
sprite.Shadow = ds;
ElementCompositionPreview.SetElementChildVisual(myGrid, sprite);
```

* Dùng **shadow** tiết kiệm; tập trung vào thành phần tương tác nổi (cards, flyouts).

---

## 4) Shape, Stroke & Spacing

* **CornerRadius**: duy trì nhất quán (ví dụ **4** cho control, **8** cho surface/container). Tạo resource dùng chung:

```xaml
<CornerRadius x:Key="App/Corner/Control">4</CornerRadius>
<CornerRadius x:Key="App/Corner/Container">8</CornerRadius>
```

* **Stroke**: dùng `ControlStrokeColorDefaultBrush`/`CardStrokeColorDefaultBrush` thay vì màu cứng.
* **Spacing**: grid 4/8/12/16; nhất quán giữa các màn hình.

---

## 5) Typography

* WinUI 3 mặc định dùng **Segoe UI Variable** trên Windows 11.
* Tạo **text styles** bằng `Style` + tokens màu:

```xaml
<Style TargetType="TextBlock" x:Key="Text/Title">
  <Setter Property="FontSize" Value="20"/>
  <Setter Property="FontWeight" Value="SemiBold"/>
  <Setter Property="Foreground" Value="{ThemeResource TextFillColorPrimaryBrush}"/>
</Style>
```

* Hỗ trợ **TextScaleFactor** (Settings → Accessibility → Text Size). Tránh set `FontSize` tuyệt đối ở mọi nơi; ưu tiên thang semantic.

---

## 6) Motion (Transitions & Composition)

### 6.1 Theme transitions

```xaml
<Grid>
  <Grid.Transitions>
    <TransitionCollection>
      <EntranceThemeTransition IsStaggeringEnabled="True"/>
    </TransitionCollection>
  </Grid.Transitions>
</Grid>
```

* **Entrance/Exit**, **Reposition**, **AddDeleteThemeTransition**.

### 6.2 Connected animations

```csharp
var svc = Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService.GetForCurrentView();
svc.PrepareToAnimate("image", MyImage);
// Navigate...
```

* Dẫn dắt mắt người dùng khi chuyển ngữ cảnh.

### 6.3 Respect motion settings

* Kiểm tra **Reduce motion** và giảm/loại bỏ animations nếu người dùng yêu cầu.

---

## 7) Iconography

* Dùng **Fluent icons**: `SymbolIcon`, `FontIcon` (Segoe Fluent Icons), `ImageIcon`, `PathIcon`.
* Đảm bảo kích cỡ, hit target **>= 40x40 epx** cho touch.

---

## 8) Accessibility (A11y)

* **Contrast**: tối thiểu **4.5:1** cho body text; dùng theme tokens để tự thích nghi.
* **Focus**: giữ **focus visuals** mặc định; tránh tắt outlines.
* **Keyboard nav**: bảo đảm **Tab/Shift+Tab** bao phủ mọi điều khiển tương tác.
* **High Contrast**: test với HC themes; tránh hard-code màu; dùng `{ThemeResource}`.
* **Text scaling**: test ở 125%–200%.

---

## 9) Navigation & Layout patterns (Fluent-native)

* **NavigationView**: top vs left pane tùy mật độ chức năng; giữ tiêu đề rõ ràng, nhóm logical.
* **Content/Dialog**: dùng `ContentDialog` cho confirm/decision; `TeachingTip` cho guidance; `InfoBar` cho trạng thái (Success/Warning/Error/Info).
* **Cards**: kết hợp **Stroke + Shadow nhẹ + CornerRadius**; tránh border quá đậm.

---

## 10) Theming Architecture cho WinUI 3

### 10.1 Khung ResourceDictionary

```xaml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="/Themes/Colors.xaml"/>
      <ResourceDictionary Source="/Themes/Typography.xaml"/>
      <ResourceDictionary Source="/Themes/Components.xaml"/>
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

### 10.2 Components (ví dụ Button)

```xaml
<Style TargetType="Button" x:Key="App/Button/Primary" BasedOn="{StaticResource DefaultButtonStyle}">
  <Setter Property="Background" Value="{ThemeResource AccentFillColorDefaultBrush}"/>
  <Setter Property="Foreground" Value="{ThemeResource TextOnAccentFillColorPrimaryBrush}"/>
  <Setter Property="CornerRadius" Value="{StaticResource App/Corner/Control}"/>
</Style>
```

### 10.3 Theme switch

```csharp
// App-level
Application.Current.RequestedTheme = ApplicationTheme.Dark; // hoặc Light
```

> Cho phép người dùng chọn Light/Dark; mặc định nên **Follow system**.

---

## 11) Mica + NavigationView mẫu (Copy–paste)

```xaml
<Window
  x:Class="App.MainWindow"
  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:mui="using:Microsoft.UI.Xaml.Controls">

  <Window.SystemBackdrop>
    <MicaBackdrop Kind="Base"/>
  </Window.SystemBackdrop>

  <Grid Background="{ThemeResource SolidBackgroundFillColorBaseBrush}">
    <mui:NavigationView x:Name="Nav" PaneDisplayMode="LeftCompact" IsSettingsVisible="True">
      <mui:NavigationView.MenuItems>
        <mui:NavigationViewItem Icon="Home" Content="Home"/>
        <mui:NavigationViewItem Icon="Shop" Content="Store"/>
      </mui:NavigationView.MenuItems>
      <Frame x:Name="RootFrame"/>
    </mui:NavigationView>
  </Grid>
</Window>
```

---

## 12) Card style mẫu (Stroke + Shadow + Radius)

```xaml
<Grid>
  <Grid.Shadow><ThemeShadow/></Grid.Shadow>
  <Grid Background="{ThemeResource ControlFillColorDefaultBrush}"
        CornerRadius="{StaticResource App/Corner/Container}"
        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
        BorderThickness="1"
        Padding="16">
    <StackPanel Spacing="8">
      <TextBlock Style="{StaticResource Text/Title}" Text="Card title"/>
      <TextBlock Text="Card body text area."/>
      <Button Style="{StaticResource App/Button/Primary}" Content="Action"/>
    </StackPanel>
  </Grid>
</Grid>
```

---

## 13) Motion pattern mẫu (Entrance + Element)\*\*

```xaml
<StackPanel>
  <StackPanel.Transitions>
    <TransitionCollection>
      <EntranceThemeTransition IsStaggeringEnabled="True"/>
    </TransitionCollection>
  </StackPanel.Transitions>

  <TextBlock Text="Title"/>
  <Button Content="Primary"/>
  <Button Content="Secondary"/>
</StackPanel>
```

---

## 14) Kiểm thử & Pitfalls

* **Hard-coded color** → phá vỡ Dark/HC, bỏ qua accent.
* **Bỏ Focus visuals** → giảm khả năng tiếp cận.
* **Shadow quá mạnh** → lệch phong cách Fluent; chỉ dùng khi tăng affordance.
* **Acrylic lạm dụng** → mất tương phản, gây nhiễu; ưu tiên Mica cho nền app.
* **Font size tuyệt đối tràn lan** → không tương thích TextScaleFactor.
* **Không dùng ThemeResource** → UI không thích nghi hệ thống.

---

## 15) Checklist áp dụng Fluent cho dự án WinUI 3

* [ ] Mica cho `Window.SystemBackdrop` + nền `SolidBackgroundFillColorBaseBrush`
* [ ] Alias tokens `App/...` trỏ về ThemeResource hệ thống
* [ ] CornerRadius & Stroke tokens chuẩn hoá
* [ ] Style text/heading thống nhất (Title/Body/Caption)
* [ ] Primary/Secondary Button styles dựa trên Accent tokens
* [ ] Motion: EntranceThemeTransition + tôn trọng Reduce motion
* [ ] A11y: Contrast 4.5:1+, focus rõ, keyboard nav đầy đủ, test High Contrast
* [ ] Kiểm thử Light/Dark/HC & TextScaleFactor

---

