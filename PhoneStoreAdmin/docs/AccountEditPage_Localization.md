# AccountEditPage Localization Implementation

## 📋 Overview

This document outlines the localization implementation for `AccountEditPage.xaml` and `AccountEditPage.xaml.cs` using WinUI 3's x:Uid pattern for XAML and ResourceLoader for code-behind.

**Date**: 2025-10-14  
**Author**: GitHub Copilot  
**Version**: 1.0

---

## 🎯 What Was Done

### 1. **XAML Updates - Added x:Uid Attributes**

Added `x:Uid` attributes to all UI elements requiring localization:

#### Section Headers
- `AccountEdit_BasicInfoSection` - Basic Information section title
- `AccountEdit_SecuritySection` - Security section title  
- `AccountEdit_RolesSection` - Roles and Permissions section title

#### Form Fields
- `AccountEdit_UsernameTextBox` - Username input (Header + PlaceholderText)
- `AccountEdit_FullNameTextBox` - Full name input (Header + PlaceholderText)
- `AccountEdit_EmailTextBox` - Email input (Header + PlaceholderText)
- `AccountEdit_PhoneTextBox` - Phone input (Header + PlaceholderText)
- `AccountEdit_IsActiveToggle` - Active status toggle (Header + OnContent + OffContent)
- `AccountEdit_PasswordBox` - Password input (Header + PlaceholderText + Description)
- `AccountEdit_ConfirmPasswordBox` - Confirm password (Header + PlaceholderText)
- `AccountEdit_AssignedRolesLabel` - Roles label

#### Buttons (Content set in code-behind)
- `SaveButton` - Save changes button
- `CancelButton` - Cancel button
- `SelectRolesButton` - Select roles button

#### Dynamic Elements (Set in code-behind)
- `BreadcrumbHomeText` - Breadcrumb home link text
- `BreadcrumbCurrentText` - Current page breadcrumb
- `SubtitleTextBlock` - Page subtitle

---

### 2. **Code-Behind Updates**

#### Added ResourceLoader
```csharp
private readonly ResourceLoader _resourceLoader = new();
```

#### Initialize Localized Strings Method
```csharp
private void InitializeLocalizedStrings()
{
    // Set button contents
    SaveButton.Content = _resourceLoader.GetString("AccountEdit_SaveButton");
    CancelButton.Content = _resourceLoader.GetString("Common/Cancel");
    SelectRolesButton.Content = _resourceLoader.GetString("AccountEdit_SelectRolesButton");
}
```

#### Updated Methods with Localization

**UpdateHeaderTexts:**
```csharp
BreadcrumbHomeText.Text = _resourceLoader.GetString("Common/AccountManagement");
BreadcrumbCurrentText.Text = string.Format(
    _resourceLoader.GetString("AccountEdit_BreadcrumbCurrent"), 
    displayName
);
SubtitleTextBlock.Text = string.Format(
    _resourceLoader.GetString("AccountEdit_Subtitle"), 
    displayName
);
```

**UpdateSelectedRolesText:**
```csharp
if (_selectedRoleIds.Count == 0)
{
    SelectedRolesTextBlock.Text = _resourceLoader.GetString("AccountEdit_NoRolesSelected");
    return;
}
```

**Validation Error Messages:**
```csharp
ShowError(_resourceLoader.GetString("AccountEdit_Error_UsernameEmpty"));
ShowError(_resourceLoader.GetString("AccountEdit_Error_PasswordTooShort"));
ShowError(_resourceLoader.GetString("AccountEdit_Error_PasswordMismatch"));
```

**Success Messages:**
```csharp
await ShowSuccessDialog(_resourceLoader.GetString("AccountEdit_Success_Updated"));
```

---

## 📝 Resource Keys Added

### Vietnamese (vi-VN/Resources.resw)

#### Section Headers
```xml
<data name="AccountEdit_BasicInfoSection.Text">
  <value>Thông tin cơ bản</value>
</data>
<data name="AccountEdit_SecuritySection.Text">
  <value>Bảo mật</value>
</data>
<data name="AccountEdit_RolesSection.Text">
  <value>Vai trò và quyền hạn</value>
</data>
```

#### Form Fields
```xml
<data name="AccountEdit_UsernameTextBox.Header">
  <value>Tên đăng nhập</value>
</data>
<data name="AccountEdit_UsernameTextBox.PlaceholderText">
  <value>Nhập tên đăng nhập</value>
</data>
<!-- ... more fields ... -->
```

#### Toggle Switch
```xml
<data name="AccountEdit_IsActiveToggle.Header">
  <value>Trạng thái hoạt động</value>
</data>
<data name="AccountEdit_IsActiveToggle.OnContent">
  <value>Đang hoạt động</value>
</data>
<data name="AccountEdit_IsActiveToggle.OffContent">
  <value>Đã vô hiệu hóa</value>
</data>
```

#### Error Messages (with parameters)
```xml
<data name="AccountEdit_Error_UsernameEmpty">
  <value>Tên đăng nhập không được để trống</value>
</data>
<data name="AccountEdit_Error_PasswordMismatch">
  <value>Mật khẩu xác nhận không khớp</value>
</data>
<data name="AccountEdit_Error_Exception">
  <value>Đã xảy ra lỗi: {0}</value>
</data>
<data name="AccountEdit_BreadcrumbCurrent">
  <value>Chỉnh sửa: {0}</value>
</data>
<data name="AccountEdit_Subtitle">
  <value>Cập nhật thông tin, trạng thái và vai trò cho tài khoản {0}</value>
</data>
```

#### Common Strings
```xml
<data name="Common/AccountManagement">
  <value>Quản lý tài khoản</value>
</data>
<data name="Common/Success">
  <value>Thành công</value>
</data>
<data name="Common/Close">
  <value>Đóng</value>
</data>
<data name="Common/Cancel">
  <value>Hủy</value>
</data>
```

### English (en-US/Resources.resw)

All corresponding keys with English translations (38 keys total).

---

## 🔑 Complete List of Keys Added

| Key | Type | Description |
|-----|------|-------------|
| `AccountEdit_BasicInfoSection.Text` | Section | Basic info section title |
| `AccountEdit_UsernameTextBox.Header` | Field | Username label |
| `AccountEdit_UsernameTextBox.PlaceholderText` | Field | Username placeholder |
| `AccountEdit_FullNameTextBox.Header` | Field | Full name label |
| `AccountEdit_FullNameTextBox.PlaceholderText` | Field | Full name placeholder |
| `AccountEdit_EmailTextBox.Header` | Field | Email label |
| `AccountEdit_EmailTextBox.PlaceholderText` | Field | Email placeholder |
| `AccountEdit_PhoneTextBox.Header` | Field | Phone label |
| `AccountEdit_PhoneTextBox.PlaceholderText` | Field | Phone placeholder |
| `AccountEdit_IsActiveToggle.Header` | Toggle | Toggle label |
| `AccountEdit_IsActiveToggle.OnContent` | Toggle | On state text |
| `AccountEdit_IsActiveToggle.OffContent` | Toggle | Off state text |
| `AccountEdit_SecuritySection.Text` | Section | Security section title |
| `AccountEdit_PasswordBox.Header` | Field | Password label |
| `AccountEdit_PasswordBox.PlaceholderText` | Field | Password placeholder |
| `AccountEdit_PasswordBox.Description` | Field | Password description |
| `AccountEdit_ConfirmPasswordBox.Header` | Field | Confirm password label |
| `AccountEdit_ConfirmPasswordBox.PlaceholderText` | Field | Confirm password placeholder |
| `AccountEdit_RolesSection.Text` | Section | Roles section title |
| `AccountEdit_AssignedRolesLabel.Text` | Label | Assigned roles label |
| `AccountEdit_NoRolesSelected` | Code | No roles message |
| `AccountEdit_RolesSelectedCount` | Code | Roles count (param: {0}) |
| `AccountEdit_SelectRolesButton` | Code | Select roles button |
| `AccountEdit_SaveButton` | Code | Save button |
| `AccountEdit_BreadcrumbCurrent` | Code | Breadcrumb (param: {0}) |
| `AccountEdit_Subtitle` | Code | Subtitle (param: {0}) |
| `AccountEdit_Error_DataNotLoaded` | Error | Data not loaded error |
| `AccountEdit_Error_UsernameEmpty` | Error | Username empty error |
| `AccountEdit_Error_FullNameEmpty` | Error | Full name empty error |
| `AccountEdit_Error_PasswordTooShort` | Error | Password too short error |
| `AccountEdit_Error_PasswordMismatch` | Error | Password mismatch error |
| `AccountEdit_Error_SaveFailed` | Error | Save failed error |
| `AccountEdit_Success_Updated` | Success | Success message |
| `AccountEdit_Error_Exception` | Error | Exception (param: {0}) |
| `AccountEdit_Error_UnableToDetermineAccount` | Error | Cannot determine account |
| `AccountEdit_Error_LoadFailed` | Error | Load failed error |
| `AccountEdit_Error_LoadException` | Error | Load exception (param: {0}) |
| `AccountEdit_Error_RoleSelectorFailed` | Error | Role selector error |

---

## 🎨 Pattern Used

### 1. **x:Uid for XAML Elements**

**Format**: `<ElementName>.<PropertyName>`

```xaml
<TextBox x:Uid="AccountEdit_UsernameTextBox"
         x:Name="UsernameTextBox"
         Header="Tên đăng nhập"
         PlaceholderText="Nhập tên đăng nhập" />
```

**Resource Keys**:
- `AccountEdit_UsernameTextBox.Header`
- `AccountEdit_UsernameTextBox.PlaceholderText`

### 2. **ResourceLoader in Code-Behind**

**Simple String**:
```csharp
var text = _resourceLoader.GetString("KeyName");
```

**String with Parameters**:
```csharp
var text = string.Format(
    _resourceLoader.GetString("KeyWithParam"), 
    parameter
);
```

**Conditional Strings**:
```csharp
var text = condition
    ? _resourceLoader.GetString("TrueCase")
    : _resourceLoader.GetString("FalseCase");
```

---

## ⚠️ Important Notes

### 1. **Naming Convention**

- **Prefix**: `AccountEdit_` for all AccountEditPage keys
- **Section Headers**: `_BasicInfoSection`, `_SecuritySection`, `_RolesSection`
- **Form Fields**: `_<FieldName>TextBox`
- **Errors**: `_Error_<ErrorType>`
- **Success**: `_Success_<ActionType>`
- **Common**: `Common/<Name>` for shared strings across pages

### 2. **Property Suffixes**

- `.Header` - For TextBox/PasswordBox headers
- `.PlaceholderText` - For input placeholders
- `.Description` - For field descriptions
- `.Text` - For TextBlock content
- `.OnContent` / `.OffContent` - For ToggleSwitch states
- `.Content` - For Button content (when not set in code)

### 3. **Dynamic Content**

Content set dynamically in code-behind:
- Breadcrumb texts (set in `UpdateHeaderTexts`)
- Button contents (set in `InitializeLocalizedStrings`)
- Selected roles text (set in `UpdateSelectedRolesText`)
- Error messages (set in validation methods)

### 4. **Parameterized Strings**

Use `{0}`, `{1}`, etc. for parameters:
```xml
<data name="AccountEdit_Subtitle">
  <value>Cập nhật thông tin, trạng thái và vai trò cho tài khoản {0}</value>
</data>
```

Used with:
```csharp
string.Format(_resourceLoader.GetString("AccountEdit_Subtitle"), displayName)
```

---

## ✅ Checklist

- [x] Added x:Uid to all XAML elements
- [x] Added ResourceLoader to code-behind
- [x] Created InitializeLocalizedStrings method
- [x] Updated all validation messages
- [x] Updated all success messages
- [x] Updated dynamic text (breadcrumb, subtitle)
- [x] Added 38 keys to vi-VN/Resources.resw
- [x] Added 38 keys to en-US/Resources.resw
- [x] Tested with Vietnamese language
- [x] Tested with English language

---

## 🚀 Testing

```powershell
# Rebuild to load new resources
dotnet build PhoneStoreAdmin/PhoneStoreAdmin.csproj

# Run the application
dotnet run --project PhoneStoreAdmin/PhoneStoreAdmin.csproj
```

**Test Scenarios**:
1. Navigate to Account Edit page
2. Verify all labels and placeholders are localized
3. Test validation errors (empty username, password mismatch, etc.)
4. Test successful save
5. Change Windows display language to test language switching

---

## 📚 Related Documentation

- [Localization_xaml.md](Localization_xaml.md) - x:Uid in XAML guide
- [Localization_BehindCode.md](Localization_BehindCode.md) - ResourceLoader guide
- [AccountsPage_Localization.md](AccountsPage_Localization.md) - AccountsPage localization
- [AccountsPage_Localization_CodeBehind.md](AccountsPage_Localization_CodeBehind.md) - AccountsPage code-behind localization

---

**Status**: ✅ Complete  
**Last Updated**: 2025-10-14
