# 🧩 WinUI 3 — `x:Uid` dùng trong **code‑behind** (ResourceLoader / ResourceManager)

> Bổ sung cho doc `x:Uid` trước đó: Cách **lấy chuỗi trong code‑behind** và gán lại vào UI. **Lưu ý quan trọng**: **khi tham chiếu key trong code** (và `ms-resource:` URI), **dùng dấu “/”** thay vì dấu “.” của khoá `x:Uid.Property` trong `.resw`.
>
> Ví dụ: `LoginBtn.Content` (trong `.resw`) ⇒ **`LoginBtn/Content`** (trong code).

---

## 1) Hai cách lấy chuỗi trong code

### 1.1 `ResourceLoader` (dễ dùng)

```csharp
using Microsoft.Windows.ApplicationModel.Resources; // Windows App SDK

var loader = new ResourceLoader(); // mặc định map "Resources"
string ok = loader.GetString("Common/OK");
string content = loader.GetString("LoginBtn/Content");
string tooltip = loader.GetString("LoginBtn/ToolTipService.ToolTip");
```

* **Ưu điểm**: Gọn, lấy chuỗi theo **ResourceContext hiện tại** (ngôn ngữ, scale, theme...).
* **Lưu ý**: Key dùng **"/"**; với attached property giữ nguyên tên đầy đủ sau dấu "/".

### 1.2 `ResourceManager` (chủ động hơn, nhiều thao tác)

```csharp
using Microsoft.Windows.ApplicationModel.Resources;

var rm = new ResourceManager();
var map = rm.MainResourceMap.GetSubtree("Resources");
string ok = map.GetValue("Common/OK").ValueAsString;
string content = map.GetValue("LoginBtn/Content").ValueAsString;
```

* Có thể tạo **ResourceContext** tuỳ biến (ví dụ ép ngôn ngữ):

```csharp
var ctx = new ResourceContext();
ctx.QualifierValues["Language"] = "vi-VN"; // ép ngôn ngữ
string okVi = map.GetValue("Common/OK", ctx).ValueAsString;
```

---

## 2) Gán chuỗi vào UI trong code

```csharp
LoginButton.Content = loader.GetString("LoginBtn/Content");
LoginButton.AccessKey = loader.GetString("LoginBtn/AccessKey");
ToolTipService.SetToolTip(LoginButton, loader.GetString("LoginBtn/ToolTipService.ToolTip"));
```

**Gợi ý**: Dùng extension method để gán nhanh bộ thuộc tính phổ biến:

```csharp
public static class LocExtensions
{
    public static void ApplyUid(this Button btn, ResourceLoader loader, string uid)
    {
        btn.Content = loader.GetString($"{uid}/Content");
        btn.AccessKey = loader.GetString($"{uid}/AccessKey");
        var tip = loader.GetString($"{uid}/ToolTipService.ToolTip");
        if (!string.IsNullOrEmpty(tip)) ToolTipService.SetToolTip(btn, tip);
    }
}
// dùng
LoginButton.ApplyUid(new ResourceLoader(), "LoginBtn");
```

---

## 3) Kết hợp `x:Uid` + format động trong code

* Chuỗi mẫu trong `.resw` (ví dụ): `HelloUser = Xin chào, {0}`
* Lấy chuỗi + **`String.Format`**:

```csharp
var helloFmt = loader.GetString("HelloUser");
GreetingText.Text = string.Format(helloFmt, ViewModel.UserName);
```

> Ưu tiên: Để **mẫu** (`HelloUser`) trong `.resw` để dịch giả chủ động trật tự tham số; param động đi qua `string.Format` ở code hoặc `{x:Bind sys:String.Format(...)}` ở XAML.

---

## 4) `ms-resource:` URI trong XAML (tái sử dụng key)

```xml
<Button Content="ms-resource:Common/OK"/>
```

* Cùng nguyên tắc **dấu “/”**.
* Dùng tốt cho **chuỗi vắn tắt** tái sử dụng nhiều nơi mà **không** cần `x:Uid` riêng.

---

## 5) Mẹo & best‑practice

* **Quy tắc dấu “/” trong code**: nhớ chuyển `Uid.Property` → `Uid/Property`.
* `x:Uid` nên dùng cho **container** có nhiều thuộc tính cần dịch (Content, ToolTip, AccessKey, Automation...).
* Chuỗi **dùng chung** (OK/Cancel/Save...) → dùng **`ms-resource:`** hoặc `ResourceLoader.GetString("Common/OK")` để **tái sử dụng** một key.
* Có tham số → để **mẫu** trong `.resw` rồi format ở code/XAML.
* Thư mục **`Strings/<lang>/Resources.resw`**: giữ cấu trúc đồng nhất giữa ngôn ngữ; có thể viết script so sánh thiếu/thừa.

---

## 6) Snippet tổng hợp

```csharp
using Microsoft.Windows.ApplicationModel.Resources;

var loader = new ResourceLoader();
// Gán nhanh
LoginButton.Content = loader.GetString("LoginBtn/Content");
LoginButton.AccessKey = loader.GetString("LoginBtn/AccessKey");
ToolTipService.SetToolTip(LoginButton, loader.GetString("LoginBtn/ToolTipService.ToolTip"));

// Lấy chuỗi dùng chung + format
var helloFmt = loader.GetString("HelloUser");
GreetingText.Text = string.Format(helloFmt, ViewModel.UserName);

// Ép ngôn ngữ (nếu cần) với ResourceManager
var rm = new ResourceManager();
var map = rm.MainResourceMap.GetSubtree("Resources");
var ctx = new ResourceContext();
ctx.QualifierValues["Language"] = "en-US";
string okEn = map.GetValue("Common/OK", ctx).ValueAsString;
```

---

### TL;DR

* Trong `.resw`: `Uid.Property`.
* Trong **code** & `ms-resource:`: **`Uid/Property`**.
* Tái sử dụng: đưa chuỗi chung vào `Common/...` và gọi `loader.GetString("Common/OK")` hoặc `ms-resource:Common/OK`.
