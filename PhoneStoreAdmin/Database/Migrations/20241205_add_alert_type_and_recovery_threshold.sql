-- Migration: Add alert_type column and recovery threshold setting
-- Date: 2024-12-05
-- Description: Add alert_type column to pricing_alert table to distinguish between
--              PRICE_DROP (0) and CLEARANCE_RECOVERY (1) alerts.
--              Also add PRICING_RECOVERY_THRESHOLD setting and update status enum.

-- ============================================================
-- 1. ALTER TABLE pricing_alert - Add alert_type column
-- ============================================================
ALTER TABLE `pricing_alert` 
ADD COLUMN `alert_type` TINYINT NOT NULL DEFAULT 0 
    COMMENT 'Loại cảnh báo: 0=PRICE_DROP (giá nhập giảm), 1=CLEARANCE_RECOVERY (market hồi phục)' 
AFTER `product_id`;

-- Add index for alert_type to optimize queries
CREATE INDEX `idx_pricing_alert_type` ON `pricing_alert` (`alert_type`);

-- ============================================================
-- 2. Update status column comment to include new status
-- ============================================================
-- Note: Status values are now:
--   0 = PENDING
--   1 = RESOLVED_HOLD  
--   2 = RESOLVED_CLEARANCE
--   3 = RESOLVED_RESET_AUTO (new - khi admin reset từ CLEARANCE về AUTO_PROTECT)
ALTER TABLE `pricing_alert` 
MODIFY COLUMN `status` TINYINT NOT NULL DEFAULT 0 
    COMMENT 'Trạng thái: 0=PENDING, 1=RESOLVED_HOLD, 2=RESOLVED_CLEARANCE, 3=RESOLVED_RESET_AUTO';

-- ============================================================
-- 3. INSERT default PRICING_RECOVERY_THRESHOLD setting
-- ============================================================
INSERT INTO `setting_string` (`code`, `value`, `description`, `created_at`, `updated_at`) 
VALUES (
    'PRICING_RECOVERY_THRESHOLD',
    '0.10',
    'Ngưỡng hồi phục thị trường (%) để tạo alert CLEARANCE_RECOVERY. Mặc định +10% (đối xứng với VarianceThreshold -10%)',
    NOW(),
    NOW()
) ON DUPLICATE KEY UPDATE 
    `description` = VALUES(`description`),
    `updated_at` = NOW();

-- ============================================================
-- 4. Verify changes
-- ============================================================
-- Run these queries to verify the migration was successful:
-- SELECT COLUMN_NAME, COLUMN_TYPE, COLUMN_COMMENT 
-- FROM INFORMATION_SCHEMA.COLUMNS 
-- WHERE TABLE_NAME = 'pricing_alert' AND COLUMN_NAME IN ('alert_type', 'status');

-- SELECT * FROM setting_string WHERE code = 'PRICING_RECOVERY_THRESHOLD';
