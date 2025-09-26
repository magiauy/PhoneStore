# 🌐 WinUI 3 — `x:Uid` Localization Guide (with Reuse Strategy)

> Hướng dẫn đầy đủ về **`x:Uid`** trong WinUI 3 (Windows App SDK) + chiến lược **tái sử dụng chuỗi** để giảm trùng lặp & dễ bảo trì.
>
> Áp dụng cho namespace `Microsoft.UI.*` và cấu trúc tài nguyên `.resw` theo chuẩn **`Strings/<language>/Resources.resw`**.

---

## 1) `x:Uid` là gì?

`x:Uid` là **khóa ánh xạ** giữa một phần tử XAML và **chuỗi/giá trị cục bộ hóa** trong file `.resw`. Khi runtime tải cây XAML, hệ thống sẽ tra **các thuộc tính có thể nội địa hóa** của phần tử và **ghi đè** giá trị XAML sẵn có bằng giá trị lấy từ `.resw` nếu tồn tại.

**Quy ước khóa trong `.resw`:**

```
<Uid>.<Property>
```

Ví dụ: `LoginBtn.Content`, `LoginBtn.AccessKey`, `LoginBtn.ToolTipService.ToolTip`.

> ℹ️ `x:Uid` **không** thay thế `x:Name`. `x:Name` để tham chiếu element; `x:Uid` để liên kết chuỗi cục bộ hóa.

---

## 2) Cấu trúc thư mục & đặt tên khóa

* **Đường dẫn mặc định**: `Strings/en-US/Resources.resw`, `Strings/vi-VN/Resources.resw`, …
* **Tên Uid**: đặt theo **vai trò/ý nghĩa** hơn là hình thức UI (ví dụ `LoginBtn`, `NavHomeItem`, `DlgConfirm`) để **dễ tái sử dụng**.
* **Quy tắc “dấu chấm”** cho thuộc tính, kể cả **attached property**:

  * `MyText.Text`
  * `MyBtn.Content`
  * `MyBtn.ToolTipService.ToolTip`
  * `MyInput.Header`, `MyInput.Description`, `MyInput.PlaceholderText`
  * `NavItem.AccessKey`
  * `TitleTextBlock.Text`

---

## 3) Thuộc tính thường được nội địa hóa

**Văn bản hiển thị**

* `TextBlock.Text`, `TextBox.Header`, `TextBox.PlaceholderText`, `RichEditBox.Header/Description`
* `Button.Content`, `AppBarButton.Label`
* `NavigationViewItem.Content`, `MenuFlyoutItem.Text`, `TeachingTip.Title/Subtitle`
* `ContentDialog.Title`, `ContentDialog.PrimaryButtonText/SecondaryButtonText/CloseButtonText`
* `InfoBar.Title/Message`

**Trợ năng & bàn phím**

* `AutomationProperties.Name`, `AutomationProperties.HelpText`
* `AccessKey`

**Tooltip**

* `ToolTipService.ToolTip`

> Không phải thuộc tính nào cũng cục bộ hóa hợp lý (ví dụ: số, thời gian — nên định dạng bằng `CultureInfo` hoặc qua ViewModel).

---

## 4) Cơ chế ghi đè & thứ tự ưu tiên

* Nếu **cùng lúc** đặt giá trị trực tiếp trong XAML **và** có khóa `x:Uid.Property` trong `.resw`, **giá trị từ `.resw` sẽ ghi đè** khi tải XAML.
* Nếu **không có khóa** tương ứng, giữ nguyên giá trị trong XAML.

> Khuyến nghị: với các phần tử có `x:Uid`, **không set trực tiếp** thuộc tính văn bản trong XAML để tránh mơ hồ.

---

## 5) Ví dụ cơ bản

**XAML**

```xml
<Button x:Uid="LoginBtn"/>
```

**Resources.resw (vi-VN)**

```
LoginBtn.Content = Đăng nhập
LoginBtn.AccessKey = L
LoginBtn.ToolTipService.ToolTip = Nhấn để đăng nhập
```

**XAML — nội địa hóa ContentDialog**

```xml
<ContentDialog x:Uid="ConfirmDlg" />
```

**Resources.resw**

```
ConfirmDlg.Title = Xác nhận
ConfirmDlg.PrimaryButtonText = Đồng ý
ConfirmDlg.CloseButtonText = Hủy
```

---

## 6) `x:Uid` với **attached properties** & thuộc tính lồng

Bạn có thể định nghĩa **attached property** qua dấu chấm trong khóa `.resw`:

```
SearchBox.ToolTipService.ToolTip = Nhập từ khóa cần tìm
```

Với **thuộc tính trong phần tử con**, bạn phải **gán `x:Uid` trực tiếp** trên phần tử con đó (không thể target qua Uid của cha):

```xml
<Button>
  <Button.Flyout>
    <MenuFlyout x:Uid="UserMenu">
      <MenuFlyoutItem x:Uid="Menu_Profile" />
      <MenuFlyoutItem x:Uid="Menu_SignOut" />
    </MenuFlyout>
  </Button.Flyout>
</Button>
```

```
UserMenu.Text = Tài khoản
Menu_Profile.Text = Hồ sơ
Menu_SignOut.Text = Đăng xuất
```

---

## 7) DataTemplate & ControlTemplate

* **DataTemplate**: đặt `x:Uid` trên **phần tử trong template** để nội địa hóa văn bản mặc định của template.
* **ControlTemplate**: tương tự; nhưng chỉ có tác dụng với **phần tử thực sự tồn tại** trong template. Không thể nội địa hóa qua `x:Uid` của control cha cho **named part** nếu nó không có `x:Uid` riêng.

---

## 8) Hạn chế & kết hợp với Binding

* `x:Uid` chỉ **đổ giá trị tĩnh**. Không có placeholder/format runtime.
* Nếu cần **chuỗi tham số** (ví dụ: "Xin chào, {0}"), có 2 hướng:

  1. Lấy chuỗi từ `.resw` bằng **ResourceManager/ResourceLoader** và **`String.Format`** trong code hoặc `{x:Bind sys:String.Format(...)}.`
  2. Dùng **converter** để format (khi dùng `{Binding}`).

Ví dụ (kết hợp ResourceManager + `x:Bind`):

```xml
<Page
  xmlns:sys="using:System"
  xmlns:globalization="using:System.Globalization">
  <TextBlock Text="{x:Bind sys:String.Format(x:Null, AppStrings.Get('HelloUser'), ViewModel.UserName)}"/>
</Page>
```

```csharp
public static class AppStrings
{
    private static readonly Microsoft.Windows.ApplicationModel.Resources.ResourceManager _rm = new();
    public static string Get(string key) => _rm.MainResourceMap.GetSubtree("Resources").GetValue(key).ValueAsString;
}
```

---

## 9) ✅ Chiến lược **tái sử dụng** (khuyến nghị thực tiễn)

**Mục tiêu**: giảm trùng lặp bản dịch, dễ bảo trì khi đổi thuật ngữ.

### 9.1 Tách **chuỗi dùng chung** khỏi `x:Uid`

* Dùng **một key chung** cho các câu/thuật ngữ hay lặp lại, ví dụ:

  * `Common/OK`, `Common/Cancel`, `Common/Retry`, `Common/Save`, `Common/Discard`
* **Không** nhân bản cùng chuỗi dưới 10 UIDs khác nhau. Thay vào đó, **tham chiếu** key chung bằng:

  * `ms-resource:` URI trực tiếp trong XAML (nếu phù hợp ngữ cảnh),
  * hoặc helper `ResourceManager`/`ResourceLoader` + `{x:Bind}` / code-behind.

**Ví dụ: ms-resource URI (áp dụng cho thuộc tính string)**

```xml
<Button Content="ms-resource:Common/OK"/>
```

> Ưu điểm: **tái sử dụng 1 key** cho nhiều nơi; không cần tạo 1 Uid riêng chỉ để chứa cùng chuỗi.

### 9.2 Tiêu chuẩn hóa **tiền tố** theo vùng chức năng

* `Nav/…` (Navigation), `Dlg/…` (Dialog), `Err/…` (Error), `Form/…` (Form labels), `Hint/…` (placeholders/tooltip), `A11y/…` (accessibility).
* Ví dụ: `Dlg/Confirm.Title`, `Dlg/Confirm.Ok`, `Dlg/Confirm.Cancel`.

### 9.3 `x:Uid` cho **khung** – `ms-resource:` cho **hạt**

* Dùng `x:Uid` cho **thành phần có nhiều thuộc tính** cần cục bộ hóa cùng lúc (Content, ToolTip, AccessKey, AutomationProperties.Name…).
* Dùng **`ms-resource:`** hoặc **helper** để gán **từng chuỗi lẻ** lặp lại trên nhiều nơi (label ngắn, nút OK/Cancel, …).

### 9.4 Định dạng & tham số

* Chuỗi có tham số (`{0}`, `{1}`) → lưu vào `.resw` rồi **format** bằng `String.Format` (qua mã hoặc `{x:Bind}`), để dịch giả kiểm soát trật tự token.

### 9.5 Review dịch & fallbacks

* Đặt **ngôn ngữ mặc định** (ví dụ en-US) đầy đủ trước, sau đó thêm `vi-VN`, v.v.
* Khi khóa thiếu, WinUI tự **fallback** theo chain ngôn ngữ hệ thống → hạn chế crash nhưng cần QA để phát hiện thiếu dịch.

---

## 10) Checklist triển khai

* [ ] Tạo `Strings/<lang>/Resources.resw` cho mọi ngôn ngữ target
* [ ] Quy ước đặt tên Uid và key (Common/…, Nav/…, Dlg/…)
* [ ] Dùng `x:Uid` cho container có nhiều thuộc tính cần cục bộ hóa
* [ ] Dùng `ms-resource:` hoặc helper để **tái sử dụng** chuỗi dùng chung
* [ ] Tránh set trực tiếp Content/Text khi đã có `x:Uid`
* [ ] Với chuỗi định dạng: lưu mẫu vào `.resw`, format bằng `{x:Bind sys:String.Format}` hoặc code
* [ ] Bao phủ **A11y**: `AutomationProperties.Name/HelpText` trong `.resw`
* [ ] Kiểm thử RTL (nếu có), dấu, khoảng trắng, ký tự đặc biệt

---

## 11) Ví dụ tổng hợp (copy–paste)

```xml
<Page
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:sys="using:System">

  <StackPanel Spacing="8">
    <!-- Dùng x:Uid cho nhiều thuộc tính -->
    <Button x:Uid="LoginBtn"/>

    <!-- Reuse string chung bằng ms-resource -->
    <Button Content="ms-resource:Common/OK"/>
    <Button Content="ms-resource:Common/Cancel"/>

    <!-- Menu lồng với Uid riêng từng item -->
    <Button>
      <Button.Flyout>
        <MenuFlyout x:Uid="UserMenu">
          <MenuFlyoutItem x:Uid="Menu_Profile"/>
          <MenuFlyoutItem x:Uid="Menu_SignOut"/>
        </MenuFlyout>
      </Button.Flyout>
    </Button>

    <!-- Chuỗi định dạng lấy từ .resw rồi format bằng x:Bind -->
    <TextBlock Text="{x:Bind sys:String.Format(x:Null, AppStrings.Get('HelloUser'), ViewModel.UserName)}"/>
  </StackPanel>
</Page>
```

**Resources.resw (vi-VN)**

```
LoginBtn.Content = Đăng nhập
LoginBtn.AccessKey = L
LoginBtn.ToolTipService.ToolTip = Nhấn để đăng nhập
UserMenu.Text = Tài khoản
Menu_Profile.Text = Hồ sơ
Menu_SignOut.Text = Đăng xuất
Common/OK = OK
Common/Cancel = Hủy
HelloUser = Xin chào, {0}
```

---

## 12) Gợi ý mở rộng

* Sinh script kiểm tra **khóa thiếu/khóa thừa** giữa các ngôn ngữ.
* Bộ quy tắc lint cho Uid & key (regex + CI).
* Mẫu `ResourceManager` singleton + extension method `GetLoc()` để gọi ngắn gọn trong ViewModel.
