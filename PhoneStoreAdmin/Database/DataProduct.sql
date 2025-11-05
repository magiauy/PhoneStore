-- Sample data for product-related tables
USE `bandienthoai`;

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE `product_serials`;
TRUNCATE TABLE `product_attribute_values`;
TRUNCATE TABLE `products`;
TRUNCATE TABLE `product_attributes`;
TRUNCATE TABLE `product_categories`;
TRUNCATE TABLE `brands`;
SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO `brands` (`name`) VALUES
  ('Apple'),
  ('Samsung'),
  ('Xiaomi');

INSERT INTO `product_categories` (`name`, `parent_id`, `note`) VALUES
  ('Smartphones', NULL, 'All smartphone devices'),
  ('Accessories', NULL, 'Supporting gadgets and add-ons'),
  ('Android Phones', 1, 'Android-based smartphones'),
  ('Wireless Audio', 2, 'Bluetooth earphones and speakers'),
  ('Charging Solutions', 2, 'Chargers, cables and power accessories');

INSERT INTO `product_attributes` (`name`, `data_type`, `note`) VALUES
  ('Color', 'text', 'Primary color of the product'),
  ('StorageCapacity', 'number', 'Storage capacity measured in GB'),
  ('ReleaseDate', 'date', 'Official release date'),
  ('Supports5G', 'bool', 'Indicates 5G network support');

INSERT INTO `products` (
  `sku`, `name`, `category_id`, `brand_id`, `price`, `cost`,
  `is_serial_tracked`, `warranty_months`, `status`, `created_at`
) VALUES
  ('IP15PRO-128-BLK', 'iPhone 15 Pro 128GB Black', 1, 1, 999.99, 799.50, 1, 24, 'active', '2024-02-01 09:00:00'),
  ('SMGS24-256-BLK', 'Samsung Galaxy S24 256GB Phantom Black', 3, 2, 899.99, 650.00, 1, 24, 'active', '2024-01-25 10:30:00'),
  ('XM-BUDS4PRO', 'Xiaomi Redmi Buds 4 Pro', 4, 3, 129.99, 80.00, 0, 12, 'active', '2023-08-20 08:15:00'),
  ('GAN65W-WHT', '65W GaN Fast Charger', 5, NULL, 49.99, 25.00, 0, 18, 'active', '2023-05-10 14:45:00');

INSERT INTO `product_attribute_values` (
  `product_id`, `attribute_id`, `value_text`, `value_number`, `value_date`, `value_bool`
) VALUES
  (1, 1, 'Black', NULL, NULL, NULL),
  (1, 2, NULL, 128, NULL, NULL),
  (1, 3, NULL, NULL, '2023-09-22', NULL),
  (1, 4, NULL, NULL, NULL, 1),
  (2, 1, 'Phantom Black', NULL, NULL, NULL),
  (2, 2, NULL, 256, NULL, NULL),
  (2, 3, NULL, NULL, '2024-01-17', NULL),
  (2, 4, NULL, NULL, NULL, 1),
  (3, 1, 'Midnight Blue', NULL, NULL, NULL),
  (3, 2, NULL, 0, NULL, NULL),
  (3, 3, NULL, NULL, '2023-08-15', NULL),
  (3, 4, NULL, NULL, NULL, 0),
  (4, 1, 'White', NULL, NULL, NULL),
  (4, 2, NULL, 0, NULL, NULL),
  (4, 3, NULL, NULL, '2023-05-01', NULL),
  (4, 4, NULL, NULL, NULL, 0);

INSERT INTO `product_serials` (
  `product_id`, `serial_number`, `imei1`, `imei2`, `batch_id`, `status`, `purchase_order_line_id`, `note`
) VALUES
  (1, 'SN-IP15-0001', '358000000000001', '358000000000002', NULL, 'in_stock', NULL, 'Initial stock shipment'),
  (1, 'SN-IP15-0002', '358000000000003', '358000000000004', NULL, 'reserved', NULL, 'Reserved for VIP customer'),
  (2, 'SN-S24-0001', '356000000000001', '356000000000002', NULL, 'in_stock', NULL, 'Available for sale'),
  (2, 'SN-S24-0002', '356000000000003', '356000000000004', NULL, 'sold', NULL, 'Sold with invoice #INV1001');

COMMIT;
