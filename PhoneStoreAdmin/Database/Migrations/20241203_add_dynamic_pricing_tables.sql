-- Migration: Add Dynamic Pricing Engine tables and columns
-- Date: 2024-12-03
-- Description: Add pricing columns to products table, create pricing_alert and pricing_history tables,
--              and insert default pricing configuration settings.
-- Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 9.1, 9.2, 9.3

-- ============================================================
-- 1. ALTER TABLE products - Add Dynamic Pricing columns
-- ============================================================
ALTER TABLE `products` 
ADD COLUMN `cost_fifo` DECIMAL(12,2) NOT NULL DEFAULT 0.00 
    COMMENT 'Giá vốn FIFO - bình quân của lô hàng cũ nhất đang tồn kho',
ADD COLUMN `cost_nifo` DECIMAL(12,2) NOT NULL DEFAULT 0.00 
    COMMENT 'Giá thay thế NIFO - giá nhập dự kiến của lô hàng mới nhất',
ADD COLUMN `market_trend` TINYINT NOT NULL DEFAULT 0 
    COMMENT 'Xu hướng thị trường: 0=STABLE, 1=UP, 2=DOWN',
ADD COLUMN `pricing_mode` TINYINT NOT NULL DEFAULT 0 
    COMMENT 'Chế độ định giá: 0=AUTO_PROTECT, 1=CLEARANCE',
ADD COLUMN `price_updated_at` DATETIME NULL 
    COMMENT 'Thời điểm cập nhật giá gần nhất';

-- ============================================================
-- 2. CREATE TABLE pricing_alert - Cảnh báo rủi ro tồn kho
-- ============================================================
CREATE TABLE IF NOT EXISTS `pricing_alert` (
    `id` INT(11) NOT NULL AUTO_INCREMENT,
    `product_id` INT(11) NOT NULL,
    `variance_percent` DECIMAL(5,2) NOT NULL 
        COMMENT 'Tỷ lệ chênh lệch giữa NIFO và FIFO (%)',
    `cost_fifo_snapshot` DECIMAL(12,2) NOT NULL 
        COMMENT 'FIFO cost tại thời điểm tạo alert',
    `cost_nifo_snapshot` DECIMAL(12,2) NOT NULL 
        COMMENT 'NIFO cost tại thời điểm tạo alert',
    `current_stock` INT(11) NOT NULL 
        COMMENT 'Số lượng tồn kho tại thời điểm tạo alert',
    `status` TINYINT NOT NULL DEFAULT 0 
        COMMENT 'Trạng thái: 0=PENDING, 1=RESOLVED_HOLD, 2=RESOLVED_CLEARANCE',
    `resolved_by` INT(11) NULL 
        COMMENT 'ID của Admin đã xử lý cảnh báo',
    `resolved_at` DATETIME NULL 
        COMMENT 'Thời điểm xử lý cảnh báo',
    `resolved_note` VARCHAR(500) NULL 
        COMMENT 'Ghi chú khi xử lý cảnh báo',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_pricing_alert_product` (`product_id`),
    KEY `idx_pricing_alert_status` (`status`),
    KEY `idx_pricing_alert_created` (`created_at`),
    CONSTRAINT `fk_pricing_alert_product` FOREIGN KEY (`product_id`) 
        REFERENCES `products` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_pricing_alert_resolved_by` FOREIGN KEY (`resolved_by`) 
        REFERENCES `accounts` (`id`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci
COMMENT='Cảnh báo rủi ro tồn kho khi thị trường giảm giá';

-- ============================================================
-- 3. CREATE TABLE pricing_history - Lịch sử biến động giá
-- ============================================================
CREATE TABLE IF NOT EXISTS `pricing_history` (
    `id` INT(11) NOT NULL AUTO_INCREMENT,
    `product_id` INT(11) NOT NULL,
    `old_price` DECIMAL(12,2) NOT NULL 
        COMMENT 'Giá cũ trước khi thay đổi',
    `new_price` DECIMAL(12,2) NOT NULL 
        COMMENT 'Giá mới sau khi thay đổi',
    `cost_fifo` DECIMAL(12,2) NOT NULL 
        COMMENT 'FIFO cost tại thời điểm thay đổi giá',
    `cost_nifo` DECIMAL(12,2) NOT NULL 
        COMMENT 'NIFO cost tại thời điểm thay đổi giá',
    `change_reason` VARCHAR(50) NOT NULL 
        COMMENT 'Lý do thay đổi: AUTO_INCREASE, CLEARANCE, MANUAL',
    `changed_by` INT(11) NULL 
        COMMENT 'ID của người thay đổi (null = hệ thống tự động)',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_pricing_history_product` (`product_id`),
    KEY `idx_pricing_history_created` (`created_at`),
    KEY `idx_pricing_history_reason` (`change_reason`),
    CONSTRAINT `fk_pricing_history_product` FOREIGN KEY (`product_id`) 
        REFERENCES `products` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_pricing_history_changed_by` FOREIGN KEY (`changed_by`) 
        REFERENCES `accounts` (`id`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci
COMMENT='Lịch sử biến động giá sản phẩm';

-- ============================================================
-- 4. INSERT setting_string - Pricing configuration defaults
-- ============================================================
INSERT INTO `setting_string` (`code`, `value`, `type`) VALUES 
    ('PRICING_DESIRED_MARGIN', '0.10', 'decimal'),
    ('PRICING_MINIMUM_MARGIN', '0.05', 'decimal'),
    ('PRICING_VARIANCE_THRESHOLD', '-0.10', 'decimal'),
    ('PRICING_STABLE_RANGE_MAX', '0', 'decimal'),
    ('PRICING_STABLE_RANGE_MIN', '-0.05', 'decimal')
ON DUPLICATE KEY UPDATE `code` = `code`;

-- ============================================================
-- 5. Initialize existing products with default pricing values
-- ============================================================
-- Set cost_fifo and cost_nifo to current cost for existing products
UPDATE `products` 
SET `cost_fifo` = `cost`, 
    `cost_nifo` = `cost`,
    `market_trend` = 0,
    `pricing_mode` = 0
WHERE `cost_fifo` = 0 AND `cost` > 0;

-- ============================================================
-- Verification queries (optional - run manually to verify)
-- ============================================================
-- SELECT * FROM information_schema.columns WHERE table_name = 'products' AND column_name LIKE '%cost%';
-- SELECT * FROM pricing_alert LIMIT 10;
-- SELECT * FROM pricing_history LIMIT 10;
-- SELECT * FROM setting_string WHERE code LIKE 'PRICING_%';
