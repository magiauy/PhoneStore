-- Migration: Add profit_margin columns to purchase_order_lines and batch_products tables
-- Date: 2024-12-01
-- Description: Add profit_margin column to track profit margin for purchase order items
--              and batch products. Selling price = Unit Cost * (1 + ProfitMargin / 100)

-- Add profit_margin column to purchase_order_lines table
ALTER TABLE `purchase_order_lines` 
ADD COLUMN `profit_margin` FLOAT NOT NULL DEFAULT 20.0 
COMMENT 'Profit margin percentage (0-100)';

-- Add profit_margin column to batch_products table
ALTER TABLE `batch_products` 
ADD COLUMN `profit_margin` FLOAT NOT NULL DEFAULT 20.0 
COMMENT 'Profit margin percentage (0-100)';

-- Update existing batch_products selling_price based on cost_price if needed
-- UPDATE batch_products SET selling_price = cost_price * (1 + profit_margin / 100) WHERE selling_price = 0;
