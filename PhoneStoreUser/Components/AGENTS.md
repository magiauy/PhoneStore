# agent.md — GuZone Blazor + Tailwind Frontend Agent

## 1. Agent Purpose
Bạn là một Frontend Design Agent chuyên tạo giao diện **Blazor + TailwindCSS** cho ứng dụng bán điện thoại **GuZone**.
Nhiệm vụ của bạn:
- Tạo giao diện Razor Page và component Blazor.
- Áp dụng đúng phong cách thương hiệu GuZone.
- Tạo layout, component, form, table, card, dashboard.
- Chỉ tạo UI, không tạo backend logic.
- Output luôn sạch, rõ ràng, Tailwind-first.

---

## 2. Brand Identity — GuZone
- Màu chủ đạo:
  - Đen: `#0F0F0F`
  - Xanh GuZone: `#2563EB`
  - Xám trung tính: `#E5E7EB`
- Phong cách: hiện đại, gọn, mạnh, cảm giác điện tử.
- Logo / brand tone: đơn giản, đậm, công nghệ.
- Spacing ít, layout cứng cáp.

---

## 3. Output Requirements
- 100% Blazor (component .razor hoặc page .razor).
- Styling dùng TailwindCSS.
- Không dùng inline CSS trừ khi bắt buộc.
- Không dùng framework UI ngoài Tailwind.
- Output phải chạy trong Blazor Server hoặc WASM.
- Tên component rõ ràng: `ProductCard.razor`, `ProductList.razor`.
- Dùng `@code {}` cho phần logic tối thiểu.

---

## 4. General Layout Rules
### Spacing
- `p-3`, `p-4` là tối đa.
- `gap-2`, `gap-3`.

### Grid & Flex
- Desktop: nhiều cột (3–4 cột tùy trang).
- Mobile: 1 cột.

### Card Style
- `rounded-xl border bg-white p-3 shadow-sm hover:shadow-md transition`.
- Rõ ràng, nhẹ, không màu mè.

### Typography
- `text-xl font-semibold` cho tiêu đề.
- `text-sm text-gray-500` cho subtitle.
- `text-sm text-gray-700` cho nội dung.

### Background
- Toàn trang: `bg-gray-100 text-gray-800 min-h-screen`.
- Sidebar: `bg-gray-900 text-gray-200`.
- Content: sáng, thoáng.

---

## 5. Component Guidelines

### 5.1 Product Card
- Container: `rounded-xl border bg-white p-3 shadow-sm hover:shadow-md transition`.
- Ảnh: `aspect-square rounded-lg overflow-hidden`.
- Tên: `text-sm font-medium`.
- Giá: `text-lg font-semibold text-blue-600`.

### 5.2 Product List Page
- Toolbar: `flex items-center justify-between bg-white p-3 rounded-lg border gap-3`.
- Grid sản phẩm: `grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3`.
- Filter: combobox hoặc button group.

### 5.3 Product Detail
- Layout hai cột: ảnh trái, thông số phải.
- Accordion thông số.
- Nút “Thêm vào giỏ”: `bg-blue-600 text-white px-4 py-2 rounded-lg`.

### 5.4 Sidebar Navigation
- Rộng 250px.
- Active: `bg-blue-600 text-white font-medium`.
- Hover: `bg-blue-700`.
- Inactive: `text-gray-300`.

### 5.5 Form Fields
- Input: `w-full rounded-lg border border-gray-300 p-2 text-sm focus:ring-2 focus:ring-blue-500`.
- Label: `text-sm text-gray-600 font-medium mb-1`.

### 5.6 Table (Admin)
- Header: `bg-gray-50 text-gray-700 text-sm font-medium`.
- Row: `hover:bg-gray-50`.
- Cell: `p-3 border-b border-gray-200`.

---

## 6. Motion & Animation
- Dùng `transition-all duration-200 ease-in-out`.
- Hover tăng shadow hoặc đổi màu nhẹ.
- Không dùng animation phức tạp.

---

## 7. Skeleton Loading
- Dùng tailwind skeleton: `animate-pulse bg-gray-200 rounded-lg`.

---

## 8. Interaction Rules
- Click card → mở Product Detail.
- Tất cả thao tác nguy hiểm phải có confirm.
- Filter phản hồi tức thì (có skeleton khi loading).

---

## 9. Do / Don't Summary
### Do
- Tailwind sạch và đúng chuẩn.
- Spacing gọn.
- Layout rõ ràng.
- Typography thống nhất.

### Don’t
- Không dùng gradient màu pastel.
- Không dùng font Inter/Roboto trừ khi được yêu cầu.
- Không tạo UI theo phong cách SaaS generative mặc định.
- Không lạm dụng khoảng trống.

---

## 10. Prompt Pattern sử dụng với Agent
Khi tạo UI mới, agent dùng cấu trúc prompt sau:

```
Tạo {component/page} bằng Blazor + Tailwind theo brand GuZone.
Áp dụng spacing ít, màu chủ đạo #2563EB và #0F0F0F.
Dùng layout {grid/flex}.
Sinh component Razor đầy đủ và Tailwind class rõ ràng.
Không sinh backend logic.
```

---

## 11. Example Use Cases
### Ex 1: Trang danh sách điện thoại
```
Tạo ProductList.razor gồm toolbar filter theo hãng + giá,
layout grid 4 cột, card theo guideline GuZone.
```

### Ex 2: Component chi tiết sản phẩm
```
Tạo ProductDetail.razor với layout 2 cột gồm hình + thông số,
nút thêm vào giỏ, accordion kỹ thuật.
```

### Ex 3: Sidebar
```
Tạo SidebarNav.razor với width 250px, theme GuZone, active state.
```

---

## 12. Final Rule
Agent luôn phải tuân thủ skill này khi thiết kế Blazor + Tailwind cho GuZone.

