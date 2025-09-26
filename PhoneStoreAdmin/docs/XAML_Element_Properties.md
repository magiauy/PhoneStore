# 📘 WinUI 3 – Detailed Elements & Properties (Only Microsoft.UI)

> Bản tổng hợp mở rộng, **chỉ bao gồm API WinUI 3 (Windows App SDK – namespace `Microsoft.UI.*`)**. Đã loại bỏ toàn bộ khái niệm/thuộc tính thuần WPF (như `Trigger`, `DataTrigger`, `EventTrigger`).
>
> ✅ Mỗi control liệt kê **thuộc tính quan trọng**, **sự kiện chính**, và ghi chú khác biệt thường gặp.

---

## 0) Cấu trúc thừa kế nhanh

* `DependencyObject` → `UIElement` → `FrameworkElement` → `Control` → (các Control cụ thể)
* Nhánh văn bản: `Microsoft.UI.Xaml.Controls.TextBlock` / `RichTextBlock` / `RichEditBox` + các inline trong `Microsoft.UI.Xaml.Documents` (`Run`, `Span`, …)
* Tạo style/visual: `Style`, `ControlTemplate`, **`VisualStateManager`** (không có Trigger)

---

## 1) Nhóm Văn bản & Soạn thảo

### 1.1 TextBlock (`Microsoft.UI.Xaml.Controls.TextBlock`)

**Mô tả**: Hiển thị văn bản chỉ đọc, hiệu năng cao cho hầu hết UI text.

**Thuộc tính chính**

* Nội dung: `Text`, `Inlines`
* Dòng & cắt chữ: `TextWrapping` (`NoWrap|Wrap|WrapWholeWords`), `TextTrimming` (`None|CharacterEllipsis|WordEllipsis`), `MaxLines`, `LineHeight`, `LineStackingStrategy`
* Căn chỉnh & typography: `TextAlignment`, `CharacterSpacing`, `FlowDirection`
* Font & màu: `FontFamily`, `FontSize`, `FontStyle`, `FontWeight`, `Foreground`
* Lựa chọn văn bản: `IsTextSelectionEnabled`, `SelectionStart`, `SelectionLength`

**Sự kiện**: không có sự kiện gõ chữ (vì chỉ đọc); kế thừa pointer/tapped từ `UIElement`.

**Ghi chú**: Dùng `RichTextBlock` nếu cần nhiều đoạn, cột, hoặc chèn UI inline.

---

### 1.2 RichTextBlock (`Microsoft.UI.Xaml.Controls.RichTextBlock`)

**Mô tả**: Hiển thị văn bản định dạng phong phú (nhiều đoạn, hyperlink, inline image/UI). Hỗ trợ `RichTextBlockOverflow` để dàn trang nhiều cột.

**Thuộc tính chính**: `Blocks` (chứa `Paragraph`), `TextWrapping`, `TextTrimming`, `MaxLines`, `Foreground`, `Font*` các loại.

**Liên quan**: `RichTextBlockOverflow` (thuộc tính `OverflowContentTarget` để đổ tràn sang cột kế).

---

### 1.3 RichEditBox (`Microsoft.UI.Xaml.Controls.RichEditBox`)

**Mô tả**: Ô nhập văn bản định dạng (bold/italic/underline, ảnh, RTF, IME…).

**Thuộc tính chính**

* Văn bản & format: `Document` (mô hình văn bản), `TextAlignment`, `TextWrapping`, `IsSpellCheckEnabled`, `IsHandwritingViewEnabled`
* Trải nghiệm bàn phím: `PreventKeyboardDisplayOnProgrammaticFocus`, `DesiredCandidateWindowAlignment`
* Header/description: `Header`, `HeaderTemplate`, `Description`
* Màu chọn: `SelectionHighlightColor`, `SelectionHighlightColorWhenNotFocused`

**Sự kiện**: `TextChanging` (đồng bộ), `SelectionChanging`

---

### 1.4 Inline text (namespace `Microsoft.UI.Xaml.Documents`)

* `Run` (đoạn văn bản nhỏ), `Span` (nhóm inline), `LineBreak`, `InlineUIContainer` (chèn UI inline)
* `Typography.*` các thuộc tính OpenType dạng **attached** (ligatures, stylistic sets, v.v.)

---

### 1.5 TextBox (`Microsoft.UI.Xaml.Controls.TextBox`)

**Mô tả**: Nhập văn bản đơn giản (plain text), hỗ trợ nhiều dòng.

**Thuộc tính chính**

* Nội dung: `Text`, `PlaceholderText`, `MaxLength`
* Nhiều dòng & cuộn: `AcceptsReturn`, `TextWrapping`, `ScrollViewer.HorizontalScrollBarVisibility`, `ScrollViewer.VerticalScrollBarVisibility`
* Bàn phím/IME: `InputScope`, `PreventKeyboardDisplayOnProgrammaticFocus`
* Trợ giúp UI: `Header`, `HeaderTemplate`, `Description`
* Lỗi & xác thực: `SelectionStart`, `SelectionLength`

**Sự kiện**: `TextChanged`, các sự kiện keyboard/pointer kế thừa.

---

### 1.6 **PasswordBox** (`Microsoft.UI.Xaml.Controls.PasswordBox`)

**Mô tả**: Ô nhập mật khẩu có nút “reveal”.

**Thuộc tính chính**

* Giá trị: `Password`, `MaxLength`, `PasswordChar`
* Trợ giúp: `PlaceholderText`, `Header`, `HeaderTemplate`, `Description`
* **Hiển thị mật khẩu**: **`PasswordRevealMode`** (`Peek` – giữ chuột để xem, `Hidden` – luôn che, `Visible` – luôn hiện)
* Bàn phím: `PreventKeyboardDisplayOnProgrammaticFocus`

**Sự kiện**: `PasswordChanging` (đồng bộ, trước khi render), `PasswordChanged`

**Ghi chú**: `IsPasswordRevealButtonEnabled` đã **deprecated** – dùng `PasswordRevealMode`.

---

## 2) Nhóm Nhập liệu & Picker

* **NumberBox** → `Value`, `Minimum`, `Maximum`, `SmallChange`, `SpinButtonPlacementMode`, `Header`, `HeaderTemplate`
* **Slider** → `Minimum`, `Maximum`, `Value`, `StepFrequency`, `IsThumbToolTipEnabled`, `Orientation`
* **ToggleSwitch** → `IsOn`, `OnContent`, `OffContent`
* **CheckBox** → `IsChecked`, `IsThreeState`
* **RadioButton** → `IsChecked`, `GroupName`
* **ComboBox** → `ItemsSource`, `SelectedItem`, `SelectedIndex`, `IsEditable`
* **AutoSuggestBox** → `Text`, `ItemsSource`, `UpdateTextOnEnter`, `TextMemberPath`; sự kiện: `TextChanged`, `SuggestionChosen`, `QuerySubmitted`
* **DatePicker** → `Date`, `MinYear`, `MaxYear`
* **TimePicker** → `Time`, `ClockIdentifier`, `MinuteIncrement`
* **CalendarDatePicker** / **CalendarView** → chọn ngày/phạm vi ngày
* **ColorPicker** → `Color`, `IsMoreButtonVisible`
* **RatingControl** → `Value`, `MaxValue`, `IsReadOnly`

---

## 3) Nhóm Dữ liệu & Items

* **ListView** / **GridView** → `ItemsSource`, `ItemTemplate`, `SelectedItem`, `SelectionMode`, `IsItemClickEnabled`; sự kiện: `ItemClick`, `SelectionChanged`
* **ItemsRepeater** → `ItemsSource`, `ItemTemplate`, `Layout` (Stack/UniformGrid…)
* **ListBox** → `ItemsSource`, `SelectedItem`, `SelectionMode`
* **TreeView** → `RootNodes`/`ItemsSource`, `SelectionMode`; sự kiện: `ItemInvoked`
* **ItemsView** (mới) → trình bày dữ liệu linh hoạt theo layout.

---

## 4) Điều hướng & Khung chứa

* **NavigationView** → `MenuItems/MenuItemsSource`, `SelectedItem`, `PaneDisplayMode`, `IsPaneOpen`, `IsBackButtonVisible`, `IsSettingsVisible`, `AlwaysShowHeader`
* **TabView** → `TabItemsSource`, `SelectedItem`, `TabWidthMode`, `CanDragTabs`, `IsAddTabButtonVisible`; sự kiện: `TabCloseRequested`, `TabDragStarting`
* **Frame** → `Navigate(Type)`, `CanGoBack`, `BackStack`
* **ContentDialog** → `Title`, `Content`, `PrimaryButtonText`, `SecondaryButtonText`, `CloseButtonText`, `DefaultButton`, `IsPrimaryButtonEnabled`
* **Pivot** / **SplitView** → tổ chức trang/tab và panel
* **BreadcrumbBar** → `ItemsSource`, sự kiện: `ItemClicked`

---

## 5) Bố cục (Layout)

* **Grid** → `RowDefinitions`, `ColumnDefinitions`; attached: `Grid.Row`, `Grid.Column`, `Grid.RowSpan`, `Grid.ColumnSpan`
* **StackPanel** → `Orientation`, `Spacing`
* **Canvas** → attached: `Canvas.Left`, `Canvas.Top`, `Canvas.ZIndex`
* **RelativePanel** → attached: `RightOf`, `Below`, `AlignLeftWith`…
* **ScrollViewer** → `HorizontalScrollBarVisibility`, `VerticalScrollBarVisibility`, `ZoomMode`
* **Border** → `Background`, `BorderBrush`, `BorderThickness`, `CornerRadius`
* **VariableSizedWrapGrid** / **WrapGrid** (khi cần dàn ô)

---

## 6) Media, Thông tin & Popup

* **Image** → `Source`, `Stretch`, `NineGrid`
* **MediaPlayerElement** → `Source`, `AreTransportControlsEnabled`
* **ProgressBar** → `Minimum`, `Maximum`, `Value`, `IsIndeterminate`
* **ProgressRing** → `IsActive`
* **InfoBar** → `Title`, `Message`, `Severity`, `IsOpen`, `IsClosable`, `IconSource`
* **TeachingTip** → `Title`, `Subtitle`, `IsOpen`, `Target`, `Placement`
* **Flyout** / **MenuFlyout** / **ToolTip** → `Content`, `Placement`; `MenuFlyoutItem` với sự kiện `Click`

---

## 7) Hình khối & Icon

* **Rectangle** (`Fill`, `RadiusX`, `RadiusY`), **Ellipse**, **Line** (`X1/Y1/X2/Y2`), **Path** (`Data`), \*\*Polygon/Polyline` (`Points\`)
* Icon: **FontIcon**, **PathIcon**, **SymbolIcon**, **ImageIcon** (thường dùng trong `AppBarButton`, `NavigationViewItem`…)

---

## 8) Styling & Visual State (thay thế Trigger)

* **Style** → `TargetType`, `Setters`
* **ControlTemplate** → `VisualTree`
* **VisualStateManager**

  * Định nghĩa trạng thái trong `VisualStateGroups`
  * Dùng `VisualState.Setters` hoặc `Storyboard` để thay đổi thuộc tính khi chuyển trạng thái
  * Ví dụ nhanh:

    ```xaml
    <Grid x:Name="Root">
      <VisualStateManager.VisualStateGroups>
        <VisualStateGroup x:Name="CommonStates">
          <VisualState x:Name="PointerOver">
            <VisualState.Setters>
              <Setter Target="Root.Background" Value="LightGray"/>
            </VisualState.Setters>
          </VisualState>
        </VisualStateGroup>
      </VisualStateManager.VisualStateGroups>
    </Grid>
    ```

---

## 9) Mẫu thuộc tính chi tiết – trọng tâm theo góp ý của bạn

### 9.1 **TextBlock** – checklist thuộc tính bạn thường dùng

* Văn bản: `Text` (không null → rỗng nếu gán null), `Inlines`
* Dòng: `TextWrapping`, `TextTrimming`, `MaxLines`, `LineHeight`, `LineStackingStrategy`
* Căn: `TextAlignment` (Start/Center/End/Justify), `FlowDirection`
* Font & màu: `FontFamily`, `FontSize`, `FontStyle`, `FontWeight`, `Foreground`, `CharacterSpacing`
* Lựa chọn: `IsTextSelectionEnabled`, `SelectionStart`, `SelectionLength`

### 9.2 **PasswordBox** – các điểm hay thiếu

* Giá trị & mask: `Password`, `PasswordChar`, `MaxLength`
* Trợ giúp: `Header`, `HeaderTemplate`, `PlaceholderText`, `Description`
* **Hiển thị mật khẩu**: `PasswordRevealMode = Peek|Hidden|Visible`
* Bàn phím: `PreventKeyboardDisplayOnProgrammaticFocus`
* Sự kiện: `PasswordChanging` (đồng bộ – validate sớm), `PasswordChanged`
* Lưu ý: `IsPasswordRevealButtonEnabled` **không dùng nữa**; hiển thị nút 👁️ còn phụ thuộc focus, độ rộng tối thiểu, đã nhập ký tự hay chưa.

---

## 10) Attached properties hay dùng

* `Grid.Row`, `Grid.Column`, `Grid.RowSpan`, `Grid.ColumnSpan`
* `Canvas.Left`, `Canvas.Top`, `Canvas.ZIndex`
* `RelativePanel.RightOf`, `Below`, `AlignLeftWith`, …
* `ToolTipService.ToolTip`

---

## 11) Mẹo thực tiễn

* Văn bản chỉ đọc → ưu tiên `TextBlock` (nhẹ, render đẹp). Cần đoạn phức tạp/inline UI → `RichTextBlock`.
* Nhập văn bản cơ bản → `TextBox`. Cần định dạng/đổi line height → cân nhắc `RichEditBox`.
* Mật khẩu → `PasswordBox` với `PasswordRevealMode` để kiểm soát trải nghiệm nút reveal.
* Trạng thái control → dùng **VisualState** + Setters/Storyboard (không có Trigger).

---

### 🔗 Gợi ý tài nguyên tra cứu (chính thức)

* API namespaces: `Microsoft.UI.Xaml`, `Microsoft.UI.Xaml.Controls`, `Microsoft.UI.Xaml.Documents`
* WinUI 3 Gallery (có demo tương tác hầu hết control)

> Nếu bạn muốn, mình có thể **xuất thêm một bản kèm link tài liệu chính thức đến từng class + từng thuộc tính** để bạn tra cứu ngay trong IDE.
