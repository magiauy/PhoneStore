-- ============================================================
-- Script: Insert default system settings
-- Table: setting_string
-- Description: Creates all system settings defined in SystemSettingCode enum
-- IMPORTANT: These settings are managed by enum, do not add manually!
-- ============================================================

-- Clear existing settings (optional - uncomment if needed for fresh start)
-- TRUNCATE TABLE setting_string;

-- Insert all system settings from SystemSettingCode enum
INSERT INTO setting_string (code, value, type) VALUES 
    ('PROFIT_MARGIN', '0.20', 'percentage'),
    ('TAX_RATE', '0.10', 'percentage'),
    ('CURRENCY', 'VND', 'string'),
    ('MAX_DISCOUNT_PERCENT', '50', 'number'),
    ('DEFAULT_WARRANTY_MONTHS', '12', 'number'),
    ('LOW_STOCK_THRESHOLD', '10', 'number'),
    ('STORE_NAME', 'GuZone Phone Store', 'string'),
    ('STORE_ADDRESS', '', 'string'),
    ('STORE_PHONE', '', 'string'),
    ('STORE_EMAIL', '', 'string')
ON DUPLICATE KEY UPDATE code = code;

-- Query to verify the insertion
SELECT * FROM setting_string ORDER BY code;
