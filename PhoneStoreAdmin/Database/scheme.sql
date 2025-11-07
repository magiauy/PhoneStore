-- --------------------------------------------------------
-- Host:                         localhost
-- Server version:               10.4.32-MariaDB - mariadb.org binary distribution
-- Server OS:                    Win64
-- HeidiSQL Version:             12.6.0.6765
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Dumping database structure for bandienthoai
CREATE DATABASE IF NOT EXISTS `bandienthoai` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci */;
USE `bandienthoai`;

-- Dumping structure for table bandienthoai.accounts
CREATE TABLE IF NOT EXISTS `accounts` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `person_id` int(11) NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT 1,
  `created_at` datetime NOT NULL,
  `last_login` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `username` (`username`),
  KEY `person_id` (`person_id`),
  CONSTRAINT `accounts_ibfk_1` FOREIGN KEY (`person_id`) REFERENCES `persons` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=500054 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.account_roles
CREATE TABLE IF NOT EXISTS `account_roles` (
  `account_id` int(11) NOT NULL,
  `role_id` int(11) NOT NULL,
  PRIMARY KEY (`account_id`,`role_id`),
  KEY `role_id` (`role_id`),
  CONSTRAINT `account_roles_ibfk_1` FOREIGN KEY (`account_id`) REFERENCES `accounts` (`id`),
  CONSTRAINT `account_roles_ibfk_2` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.batches
CREATE TABLE IF NOT EXISTS `batches` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `purchase_order_id` int(11) NOT NULL,
  `batch_code` varchar(50) NOT NULL,
  `created_at` datetime NOT NULL,
  `note` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `batch_code` (`batch_code`),
  KEY `purchase_order_id` (`purchase_order_id`),
  CONSTRAINT `batches_ibfk_1` FOREIGN KEY (`purchase_order_id`) REFERENCES `purchase_orders` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.batch_products
CREATE TABLE IF NOT EXISTS `batch_products` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `batch_id` int(11) NOT NULL,
  `product_id` int(11) NOT NULL,
  `quantity` int(11) NOT NULL DEFAULT 0,
  `cost_price` decimal(12,2) NOT NULL DEFAULT 0.00,
  `selling_price` decimal(12,2) NOT NULL DEFAULT 0.00,
  PRIMARY KEY (`id`),
  KEY `batch_id` (`batch_id`),
  KEY `product_id` (`product_id`),
  CONSTRAINT `batch_products_ibfk_1` FOREIGN KEY (`batch_id`) REFERENCES `batches` (`id`),
  CONSTRAINT `batch_products_ibfk_2` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.brands
CREATE TABLE IF NOT EXISTS `brands` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.customers
CREATE TABLE IF NOT EXISTS `customers` (
  `person_id` int(11) NOT NULL,
  `address` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`person_id`),
  KEY `idx_customers_person_id` (`person_id`),
  KEY `idx_customers_address` (`address`(100)),
  FULLTEXT KEY `ft_address` (`address`),
  CONSTRAINT `customers_ibfk_1` FOREIGN KEY (`person_id`) REFERENCES `persons` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.employees
CREATE TABLE IF NOT EXISTS `employees` (
  `person_id` int(11) NOT NULL,
  `hire_date` date DEFAULT NULL,
  PRIMARY KEY (`person_id`),
  CONSTRAINT `employees_ibfk_1` FOREIGN KEY (`person_id`) REFERENCES `persons` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.invoices
CREATE TABLE IF NOT EXISTS `invoices` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `person_id` int(11) DEFAULT NULL COMMENT 'người mua (customer) hoặc null nếu vãng lai',
  `promotion_code_id` int(11) DEFAULT NULL,
  `created_by` int(11) NOT NULL COMMENT 'nhân viên lập hóa đơn',
  `invoice_date` datetime NOT NULL,
  `status` enum('unpaid','paid','refunded','cancelled') NOT NULL DEFAULT 'unpaid',
  `total_amount` decimal(14,2) NOT NULL DEFAULT 0.00,
  `discount_amount` decimal(14,2) NOT NULL DEFAULT 0.00,
  `final_amount` decimal(14,2) NOT NULL DEFAULT 0.00,
  `payment_method` enum('cash','card','bank','ewallet') NOT NULL DEFAULT 'cash',
  `note` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `person_id` (`person_id`),
  KEY `promotion_code_id` (`promotion_code_id`),
  KEY `created_by` (`created_by`),
  CONSTRAINT `invoices_ibfk_1` FOREIGN KEY (`person_id`) REFERENCES `persons` (`id`),
  CONSTRAINT `invoices_ibfk_2` FOREIGN KEY (`promotion_code_id`) REFERENCES `promotion_codes` (`id`),
  CONSTRAINT `invoices_ibfk_3` FOREIGN KEY (`created_by`) REFERENCES `persons` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.invoice_lines
CREATE TABLE IF NOT EXISTS `invoice_lines` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `invoice_id` int(11) NOT NULL,
  `product_id` int(11) NOT NULL,
  `quantity` int(11) NOT NULL DEFAULT 1,
  `unit_price` decimal(12,2) NOT NULL,
  `discount_pct` decimal(5,2) NOT NULL DEFAULT 0.00,
  `total_price` decimal(14,2) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `invoice_lines_index_3` (`invoice_id`),
  KEY `product_id` (`product_id`),
  CONSTRAINT `invoice_lines_ibfk_1` FOREIGN KEY (`invoice_id`) REFERENCES `invoices` (`id`),
  CONSTRAINT `invoice_lines_ibfk_2` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.invoice_line_serials
CREATE TABLE IF NOT EXISTS `invoice_line_serials` (
  `invoice_line_id` int(11) NOT NULL,
  `product_serial_id` int(11) NOT NULL,
  PRIMARY KEY (`invoice_line_id`,`product_serial_id`),
  KEY `invoice_line_serials_index_4` (`product_serial_id`),
  CONSTRAINT `invoice_line_serials_ibfk_1` FOREIGN KEY (`invoice_line_id`) REFERENCES `invoice_lines` (`id`),
  CONSTRAINT `invoice_line_serials_ibfk_2` FOREIGN KEY (`product_serial_id`) REFERENCES `product_serials` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.payments
CREATE TABLE IF NOT EXISTS `payments` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `invoice_id` int(11) NOT NULL,
  `payment_date` datetime NOT NULL,
  `amount` decimal(14,2) NOT NULL,
  `method` enum('cash','card','bank','ewallet') NOT NULL,
  `reference_no` varchar(100) DEFAULT NULL,
  `note` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `invoice_id` (`invoice_id`),
  CONSTRAINT `payments_ibfk_1` FOREIGN KEY (`invoice_id`) REFERENCES `invoices` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.permissions
CREATE TABLE IF NOT EXISTS `permissions` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `code` varchar(100) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=33 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.persons
CREATE TABLE IF NOT EXISTS `persons` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `code` varchar(30) DEFAULT NULL,
  `full_name` varchar(120) NOT NULL,
  `phone` varchar(20) DEFAULT NULL,
  `email` varchar(120) DEFAULT NULL,
  `person_type` enum('EMPLOYEE','CUSTOMER') NOT NULL,
  `created_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `idx_persons_customer_active` (`person_type`,`is_active`,`id`) USING BTREE,
  FULLTEXT KEY `ft_search` (`full_name`,`email`,`phone`,`code`),
  FULLTEXT KEY `ft_full_name` (`full_name`)
) ENGINE=InnoDB AUTO_INCREMENT=1000054 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.products
CREATE TABLE IF NOT EXISTS `products` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `sku` varchar(50) NOT NULL,
  `name` varchar(200) NOT NULL,
  `category_id` int(11) NOT NULL,
  `brand_id` int(11) DEFAULT NULL,
  `price` decimal(12,2) NOT NULL DEFAULT 0.00,
  `cost` decimal(12,2) NOT NULL DEFAULT 0.00,
  `is_serial_tracked` tinyint(1) NOT NULL DEFAULT 0 COMMENT 'Điện thoại/laptop true; phụ kiện nhỏ có thể false',
  `warranty_months` int(11) NOT NULL DEFAULT 12,
  `status` enum('active','inactive','discontinued') NOT NULL DEFAULT 'active',
  `created_at` datetime NOT NULL,
  `model_id` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `sku` (`sku`),
  KEY `category_id` (`category_id`),
  KEY `brand_id` (`brand_id`),
  KEY `idx_products_model` (`model_id`),
  CONSTRAINT `FK_products_model` FOREIGN KEY (`model_id`) REFERENCES `product_models` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `products_ibfk_1` FOREIGN KEY (`category_id`) REFERENCES `product_categories` (`id`),
  CONSTRAINT `products_ibfk_2` FOREIGN KEY (`brand_id`) REFERENCES `brands` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.product_attributes
CREATE TABLE IF NOT EXISTS `product_attributes` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(80) NOT NULL,
  `data_type` enum('text','number','date','bool') NOT NULL DEFAULT 'text',
  `note` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.product_attribute_options
CREATE TABLE IF NOT EXISTS `product_attribute_options` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `attribute_id` int(11) DEFAULT NULL,
  `display_value` varchar(50) DEFAULT NULL,
  `normalized_value` int(11) DEFAULT NULL,
  `sort_order` int(11) NOT NULL DEFAULT 0,
  `is_active` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `FK__product_attributes` (`attribute_id`),
  CONSTRAINT `FK__product_attributes` FOREIGN KEY (`attribute_id`) REFERENCES `product_attributes` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.product_attribute_values
CREATE TABLE IF NOT EXISTS `product_attribute_values` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `product_id` int(11) NOT NULL,
  `attribute_id` int(11) NOT NULL,
  `option_id` int(11) DEFAULT NULL,
  `value_text` varchar(255) DEFAULT NULL,
  `value_number` decimal(18,4) DEFAULT NULL,
  `value_date` date DEFAULT NULL,
  `value_bool` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `product_attribute_values_index_0` (`product_id`,`attribute_id`),
  KEY `attribute_id` (`attribute_id`),
  KEY `option_id` (`option_id`),
  CONSTRAINT `product_attribute_values_ibfk_1` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`),
  CONSTRAINT `product_attribute_values_ibfk_2` FOREIGN KEY (`attribute_id`) REFERENCES `product_attributes` (`id`),
  CONSTRAINT `product_attribute_values_ibfk_3` FOREIGN KEY (`option_id`) REFERENCES `product_attribute_options` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.product_categories
CREATE TABLE IF NOT EXISTS `product_categories` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(120) NOT NULL,
  `parent_id` int(11) DEFAULT NULL,
  `note` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`),
  KEY `parent_id` (`parent_id`),
  CONSTRAINT `product_categories_ibfk_1` FOREIGN KEY (`parent_id`) REFERENCES `product_categories` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.product_models
CREATE TABLE IF NOT EXISTS `product_models` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(255) DEFAULT NULL,
  `slug` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `default_image_url` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT NULL,
  `updated_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.product_serials
CREATE TABLE IF NOT EXISTS `product_serials` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `product_id` int(11) NOT NULL,
  `serial_number` varchar(100) DEFAULT NULL COMMENT 'nullable nếu chỉ có IMEI',
  `imei1` varchar(20) DEFAULT NULL COMMENT 'điện thoại',
  `imei2` varchar(20) DEFAULT NULL,
  `batch_id` int(11) DEFAULT NULL,
  `status` enum('in_stock','reserved','sold','returned','defective','rma') NOT NULL DEFAULT 'in_stock',
  `purchase_order_line_id` int(11) DEFAULT NULL COMMENT 'serial nhập từ PO line nào',
  `note` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `serial_number` (`serial_number`),
  UNIQUE KEY `imei1` (`imei1`),
  UNIQUE KEY `imei2` (`imei2`),
  KEY `product_serials_index_1` (`product_id`,`status`),
  KEY `batch_id` (`batch_id`),
  KEY `purchase_order_line_id` (`purchase_order_line_id`),
  CONSTRAINT `product_serials_ibfk_1` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`),
  CONSTRAINT `product_serials_ibfk_2` FOREIGN KEY (`batch_id`) REFERENCES `batches` (`id`),
  CONSTRAINT `product_serials_ibfk_3` FOREIGN KEY (`purchase_order_line_id`) REFERENCES `purchase_order_lines` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.promotions
CREATE TABLE IF NOT EXISTS `promotions` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(120) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `start_date` datetime NOT NULL,
  `end_date` datetime NOT NULL,
  `is_active` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.promotion_codes
CREATE TABLE IF NOT EXISTS `promotion_codes` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `promotion_id` int(11) NOT NULL,
  `code` varchar(50) NOT NULL,
  `discount_amount` decimal(12,2) NOT NULL,
  `minimum_amount` decimal(12,2) NOT NULL,
  `usage_limit` int(11) DEFAULT NULL,
  `used_count` int(11) DEFAULT 0,
  `is_active` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `promotion_id` (`promotion_id`),
  CONSTRAINT `promotion_codes_ibfk_1` FOREIGN KEY (`promotion_id`) REFERENCES `promotions` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.purchase_orders
CREATE TABLE IF NOT EXISTS `purchase_orders` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `supplier_id` int(11) NOT NULL,
  `created_by` int(11) NOT NULL COMMENT 'nhân viên lập phiếu nhập',
  `order_date` datetime NOT NULL,
  `status` enum('draft','received','cancelled') NOT NULL DEFAULT 'draft',
  `total_amount` decimal(14,2) NOT NULL DEFAULT 0.00,
  `note` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `supplier_id` (`supplier_id`),
  KEY `created_by` (`created_by`),
  CONSTRAINT `purchase_orders_ibfk_1` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`),
  CONSTRAINT `purchase_orders_ibfk_2` FOREIGN KEY (`created_by`) REFERENCES `persons` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.purchase_order_lines
CREATE TABLE IF NOT EXISTS `purchase_order_lines` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `purchase_order_id` int(11) NOT NULL,
  `product_id` int(11) NOT NULL,
  `quantity` int(11) NOT NULL DEFAULT 1,
  `unit_cost` decimal(12,2) NOT NULL DEFAULT 0.00,
  `total_cost` decimal(14,2) NOT NULL DEFAULT 0.00,
  PRIMARY KEY (`id`),
  KEY `purchase_order_lines_index_2` (`purchase_order_id`),
  KEY `product_id` (`product_id`),
  CONSTRAINT `purchase_order_lines_ibfk_1` FOREIGN KEY (`purchase_order_id`) REFERENCES `purchase_orders` (`id`),
  CONSTRAINT `purchase_order_lines_ibfk_2` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.roles
CREATE TABLE IF NOT EXISTS `roles` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(50) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `weight` int(11) NOT NULL DEFAULT 100,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.role_permissions
CREATE TABLE IF NOT EXISTS `role_permissions` (
  `role_id` int(11) NOT NULL,
  `permission_id` int(11) NOT NULL,
  PRIMARY KEY (`role_id`,`permission_id`),
  KEY `permission_id` (`permission_id`),
  CONSTRAINT `role_permissions_ibfk_1` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE,
  CONSTRAINT `role_permissions_ibfk_2` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table bandienthoai.suppliers
CREATE TABLE IF NOT EXISTS `suppliers` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(160) NOT NULL,
  `phone` varchar(20) DEFAULT NULL,
  `email` varchar(120) DEFAULT NULL,
  `address` varchar(255) DEFAULT NULL,
  `tax_number` varchar(50) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=500001 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Data exporting was unselected.

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
