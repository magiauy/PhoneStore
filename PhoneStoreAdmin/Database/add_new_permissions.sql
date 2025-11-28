-- =============================================
-- Script: Add new permissions for PhoneStore Admin
-- Date: 2025-11-28
-- Description: Adds missing permissions for Brand, Batch, Role, Report, Setting, and Product Attribute management
-- =============================================

USE bandienthoai;

-- Start transaction for safety
START TRANSACTION;

-- =============================================
-- Insert new permissions
-- =============================================

-- Brand Permissions (for BrandPage)
INSERT INTO permissions (code, description) VALUES
('BRAND_VIEW', 'Xem danh sách thương hiệu'),
('BRAND_ADD', 'Thêm thương hiệu mới'),
('BRAND_EDIT', 'Chỉnh sửa thông tin thương hiệu'),
('BRAND_DELETE', 'Xóa thương hiệu');

-- Batch Permissions (for BatchesPage - inventory batch management)
INSERT INTO permissions (code, description) VALUES
('BATCH_VIEW', 'Xem danh sách lô hàng'),
('BATCH_ADD', 'Thêm lô hàng mới'),
('BATCH_EDIT', 'Chỉnh sửa thông tin lô hàng'),
('BATCH_DELETE', 'Xóa lô hàng');

-- Role Permissions (for RolesPage - role & permission management)
INSERT INTO permissions (code, description) VALUES
('ROLE_VIEW', 'Xem danh sách vai trò'),
('ROLE_ADD', 'Thêm vai trò mới'),
('ROLE_EDIT', 'Chỉnh sửa thông tin vai trò'),
('ROLE_DELETE', 'Xóa vai trò');

-- Report Permissions (for ReportsPage)
INSERT INTO permissions (code, description) VALUES
('REPORT_VIEW', 'Xem báo cáo'),
('REPORT_EXPORT', 'Xuất báo cáo');

-- Setting Permissions (for SettingsPage)
INSERT INTO permissions (code, description) VALUES
('SETTING_VIEW', 'Xem cài đặt hệ thống'),
('SETTING_EDIT', 'Chỉnh sửa cài đặt hệ thống');

-- Product Attribute Permissions (for ProductAttributesPage)
INSERT INTO permissions (code, description) VALUES
('PRODUCT_ATTRIBUTE_VIEW', 'Xem danh sách thuộc tính sản phẩm'),
('PRODUCT_ATTRIBUTE_MANAGE', 'Quản lý thuộc tính sản phẩm');

-- =============================================
-- Assign new permissions to Admin role (role_id = 1, weight = 0)
-- Admin already has all permissions due to weight = 0, but we can explicitly add them
-- =============================================

-- Get admin role id (assuming it's the role with weight = 0)
SET @admin_role_id = (SELECT id FROM roles WHERE weight = 0 LIMIT 1);

-- If you want to explicitly assign permissions to admin role, uncomment below:
-- (Note: Admin with weight=0 already bypasses permission checks, this is optional)

/*
INSERT INTO role_permissions (role_id, permission_id)
SELECT @admin_role_id, id FROM permissions 
WHERE code IN (
    'BRAND_VIEW', 'BRAND_ADD', 'BRAND_EDIT', 'BRAND_DELETE',
    'BATCH_VIEW', 'BATCH_ADD', 'BATCH_EDIT', 'BATCH_DELETE',
    'ROLE_VIEW', 'ROLE_ADD', 'ROLE_EDIT', 'ROLE_DELETE',
    'REPORT_VIEW', 'REPORT_EXPORT',
    'SETTING_VIEW', 'SETTING_EDIT',
    'PRODUCT_ATTRIBUTE_VIEW', 'PRODUCT_ATTRIBUTE_MANAGE'
)
ON DUPLICATE KEY UPDATE role_id = role_id;
*/

-- =============================================
-- Optionally assign some permissions to Manager role
-- =============================================

-- Get manager role id (assuming weight = 1 for manager)
SET @manager_role_id = (SELECT id FROM roles WHERE weight = 1 LIMIT 1);

-- Assign VIEW permissions to manager (if manager role exists)
INSERT INTO role_permissions (role_id, permission_id)
SELECT @manager_role_id, id FROM permissions 
WHERE code IN (
    'BRAND_VIEW',
    'BATCH_VIEW',
    'REPORT_VIEW',
    'PRODUCT_ATTRIBUTE_VIEW'
)
AND @manager_role_id IS NOT NULL
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Commit transaction
COMMIT;

-- =============================================
-- Verify inserted permissions
-- =============================================
SELECT id, code, description FROM permissions 
WHERE code IN (
    'BRAND_VIEW', 'BRAND_ADD', 'BRAND_EDIT', 'BRAND_DELETE',
    'BATCH_VIEW', 'BATCH_ADD', 'BATCH_EDIT', 'BATCH_DELETE',
    'ROLE_VIEW', 'ROLE_ADD', 'ROLE_EDIT', 'ROLE_DELETE',
    'REPORT_VIEW', 'REPORT_EXPORT',
    'SETTING_VIEW', 'SETTING_EDIT',
    'PRODUCT_ATTRIBUTE_VIEW', 'PRODUCT_ATTRIBUTE_MANAGE'
)
ORDER BY id;

-- Show total permission count
SELECT COUNT(*) as total_permissions FROM permissions;
