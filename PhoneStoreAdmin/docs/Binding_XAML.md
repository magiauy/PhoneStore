# 🔗 WinUI 3 — Binding Deep Dive (x\:Name • x\:Uid • x\:Bind • Binding) 

> Bản cập nhật bổ sung mục **định dạng chuỗi (StringFormat)** theo đúng cách dùng trong **WinUI 3**. Bao gồm ví dụ thực thi ngay, mẹo xử lý `CultureInfo`, và converter thay thế khi dùng `{Binding}`.

---

## 0) Tổng quan

* **`{x:Bind}` (Compiled Binding)**: nhanh, an toàn kiểu. Mặc định **Mode=OneTime**; hỗ trợ binding tới property/field/method, event handler, element (qua `x:Name`). **Định dạng chuỗi thực hiện trực tiếp trong biểu thức** (`ToString(...)`, `String.Format(...)`) hoặc qua hàm do bạn viết (function binding).
* **`{Binding}` (Runtime/DataContext)**: linh hoạt, dựa trên `DataContext`, mặc định **Mode=OneWay**. **Không có thuộc tính `StringFormat`** như WPF; hãy dùng **converter** hoặc định dạng sẵn từ ViewModel.
* **`x:Name`**: đặt tên phần tử để tham chiếu trong `{x:Bind}`/`ElementName` hoặc từ code-behind.
* **`x:Uid`**: khóa localization; ánh xạ tới `.resw` với quy ước `Uid.Property`.
* **`TemplateBinding`**: trong `ControlTemplate`, nhẹ hơn `Binding RelativeSource=TemplatedParent`.

---

## 1) `x:Name` & tham chiếu phần tử

```xml
<Grid x:Name="Root">
  <Slider x:Name="sld" Minimum="0" Maximum="100"/>
  <!-- x:Bind có thể tham chiếu trực tiếp tên phần tử -->
  <TextBlock Text="{x:Bind sld.Value}"/>
  <!-- Với {Binding} dùng ElementName -->
  <TextBlock Text="{Binding ElementName=sld, Path=Value}"/>
</Grid>
```

---

## 2) `x:Uid` (Localization)

```xml
<Button x:Uid="LoginBtn"/>
```

`Resources.resw`:

* `LoginBtn.Content` = "Đăng nhập"
* `LoginBtn.AccessKey` = "L"
* `LoginBtn.ToolTipService.ToolTip` = "Nhấn để đăng nhập"

---

## 3) `{x:Bind}` — Compiled Binding

### 3.1 Cơ bản

```xml
<Page x:DataType="vm:LoginViewModel">
  <StackPanel>
    <TextBlock Text="{x:Bind UserName}"/>
    <TextBox Text="{x:Bind UserName, Mode=TwoWay}"/>
    <Button Click="{x:Bind OnLoginClicked}">Login</Button>
  </StackPanel>
</Page>
```

### 3.2 **Định dạng chuỗi với `{x:Bind}`** (khuyến nghị)

> WinUI 3 không có `StringFormat` như WPF. Với `{x:Bind}`, hãy định dạng trong biểu thức:

**Cách A – `ToString(...)`**

```xml
<Page xmlns:globalization="using:System.Globalization" x:DataType="vm:OrderVM">
  <TextBlock Text="{x:Bind Total.ToString('C', globalization:CultureInfo.CurrentCulture)}"/>
</Page>
```

**Cách B – `sys:String.Format(...)`**

```xml
<Page
  xmlns:sys="using:System"
  xmlns:globalization="using:System.Globalization"
  x:DataType="vm:StatsVM">
  <!-- Một tham số -->
  <TextBlock Text="{x:Bind sys:String.Format(x:Null, 'Tổng: {0:C}', Total)}"/>
  <!-- Nhiều tham số -->
  <TextBlock Text="{x:Bind sys:String.Format(x:Null, '{0} / {1}', Current, Maximum)}"/>
  <!-- Chỉ định Culture cụ thể -->
  <TextBlock Text="{x:Bind sys:String.Format(globalization:CultureInfo.GetCultureInfo('vi-VN'), '{0:dd/MM/yyyy}', OrderDate)}"/>
</Page>
```

*Mẹo*: Thêm `x:Null` ở tham số đầu để chọn đúng overload `String.Format(IFormatProvider, ...)` khi không cần culture tuỳ chỉnh.

**Cách C – Function Binding (gọn & an toàn kiểu)**

```xml
<TextBlock Text="{x:Bind FormatPrice(Price)}"/>
```

```csharp
public string FormatPrice(double v) => v.ToString("C");
```

### 3.3 `x:Bind` trong `DataTemplate`

```xml
<ListView ItemsSource="{x:Bind ViewModel.Items}">
  <ListView.ItemTemplate>
    <DataTemplate x:DataType="models:Product">
      <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="{x:Bind Name}"/>
        <TextBlock Text="{x:Bind Price.ToString('C')}"/>
      </StackPanel>
    </DataTemplate>
  </ListView.ItemTemplate>
</ListView>
```

### 3.4 Trường hợp đặc biệt: `PasswordBox`

* `PasswordBox.Password` **không phải DependencyProperty** ⇒ `{Binding}` không TwoWay được.
* Dùng `{x:Bind}` TwoWay:

```xml
<PasswordBox Password="{x:Bind ViewModel.Password, Mode=TwoWay}" PasswordRevealMode="Peek"/>
```

---

## 4) `{Binding}` — Runtime/DataContext

```xml
<Page DataContext="{x:Bind ViewModel}">
  <TextBlock Text="{Binding Title}"/>
  <TextBox Text="{Binding Title, Mode=TwoWay}"/>
  <Border Background="{Binding RelativeSource={RelativeSource Self}, Path=Tag}"/>
</Page>
```

**Lưu ý định dạng**: `{Binding}` trong WinUI **không có** `StringFormat`. Dùng **converter** hoặc định dạng sẵn.

### 4.1 Thay thế `StringFormat` bằng Converter

**Converter tối giản**

```csharp
public sealed class StringFormatConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, string language)
    => value is null || parameter is null ? value : string.Format((string)parameter, value);
  public object ConvertBack(object value, Type targetType, object parameter, string language)
    => throw new NotImplementedException();
}
```

**Sử dụng**

```xml
<Page>
  <Page.Resources>
    <local:StringFormatConverter x:Key="StringFormatConverter"/>
  </Page.Resources>
  <TextBlock Text="{Binding Score,
                           Converter={StaticResource StringFormatConverter},
                           ConverterParameter='Điểm: {0:D}'}"/>
</Page>
```

---

## 5) `TemplateBinding` (ControlTemplate)

```xml
<ControlTemplate TargetType="Button">
  <Grid Background="{TemplateBinding Background}">
    <ContentPresenter Content="{TemplateBinding Content}"/>
  </Grid>
</ControlTemplate>
```

---

## 6) Resource Markup

* `{StaticResource Key}`: dùng 1 lần khi load
* `{ThemeResource Key}`: tự cập nhật khi đổi theme

```xml
<TextBlock Foreground="{ThemeResource TextFillColorPrimaryBrush}"/>
```

---

## 7) `x:Load` — tải điều kiện

```xml
<Grid x:Load="{x:Bind ViewModel.IsDetailsVisible}"/>
```

---

## 8) Mẹo & Pitfall

* Thiếu `x:DataType` ⇒ mất IntelliSense + không phát hiện lỗi compile cho `{x:Bind}`.
* Quên gắn `DataContext` ⇒ `{Binding}` không hiển thị.
* Không triển khai `INotifyPropertyChanged` ⇒ UI không cập nhật.
* Dùng `String.Format` trong `{x:Bind}` với nhiều tham số: thêm `x:Null` hoặc cung cấp `CultureInfo`.
* Dữ liệu ngày/tiền tệ: luôn chỉ định `CultureInfo` để nhất quán.

---

## 9) Bảng tham chiếu nhanh (cập nhật)

| Chủ đề            | `{x:Bind}`                                    | `{Binding}`                                  |
| ----------------- | --------------------------------------------- | -------------------------------------------- |
| Mode mặc định     | OneTime                                       | OneWay                                       |
| Định dạng chuỗi   | `ToString`, `String.Format`, function binding | **Không có `StringFormat`** → dùng converter |
| Element reference | trực tiếp qua `x:Name`                        | `ElementName`                                |
| RelativeSource    | (n/a)                                         | `Self`, `TemplatedParent`                    |
| Converter         | Hỗ trợ                                        | Hỗ trợ                                       |
| Fallback/Null     | Hỗ trợ                                        | Hỗ trợ                                       |
| Template          | –                                             | `TemplateBinding` (trong Template)           |
| Lazy Load         | `x:Load` (bind được)                          | `x:Load` (bind được)                         |

---

## 10) Ví dụ tổng hợp

```xml
<Page
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:sys="using:System"
  xmlns:globalization="using:System.Globalization"
  xmlns:vm="using:App.ViewModels"
  x:DataType="vm:SampleVM"
  DataContext="{x:Bind ViewModel}">

  <Grid Padding="16" RowDefinitions="Auto,*">
    <!-- x:Bind format bằng ToString -->
    <TextBlock Text="{x:Bind Amount.ToString('N2')}"/>

    <!-- x:Bind format bằng String.Format có Culture -->
    <TextBlock Text="{x:Bind sys:String.Format(globalization:CultureInfo.GetCultureInfo('vi-VN'), '{0:dd/MM/yyyy}', OrderDate)}"/>

    <!-- Binding + converter thay thế StringFormat -->
    <Page.Resources>
      <local:StringFormatConverter x:Key="Fmt"/>
    </Page.Resources>
    <TextBlock Text="{Binding Score, Converter={StaticResource Fmt}, ConverterParameter='Điểm: {0:D}'}"/>

    <!-- PasswordBox: chỉ x:Bind TwoWay -->
    <PasswordBox Password="{x:Bind Password, Mode=TwoWay}" PasswordRevealMode="Peek"/>
  </Grid>
</Page>
```

---

**Gợi ý tiếp theo**: Bạn muốn mình thêm bảng **mặc định Mode & UpdateSourceTrigger** cho các thuộc tính thường gặp (TextBox.Text, Slider.Value, ToggleSwitch.IsOn, NumberBox.Value, v.v.) không?
