-- Migration: Align products with product_models and attribute options lookup
USE `bandienthoai`;

START TRANSACTION;

-- Rename products.product_models -> products.model_id and refresh FK
ALTER TABLE `products`
  DROP FOREIGN KEY `FK_products_product_models`;

ALTER TABLE `products`
  DROP INDEX `FK_products_product_models`;

ALTER TABLE `products`
  CHANGE COLUMN `product_models` `model_id` int(11) DEFAULT NULL;

ALTER TABLE `products`
  ADD INDEX `idx_products_model` (`model_id`);

ALTER TABLE `products`
  ADD CONSTRAINT `FK_products_model`
    FOREIGN KEY (`model_id`) REFERENCES `product_models` (`id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Extend product_attribute_options with sort ordering
ALTER TABLE `product_attribute_options`
  ADD COLUMN `sort_order` int(11) NOT NULL DEFAULT 0 AFTER `normalized_value`;

-- Add lookup reference to product_attribute_values
ALTER TABLE `product_attribute_values`
  ADD COLUMN `option_id` int(11) DEFAULT NULL AFTER `attribute_id`;

-- Ensure every existing attribute value has a backing option row
INSERT INTO `product_attribute_options` (`attribute_id`, `display_value`, `normalized_value`, `sort_order`, `is_active`)
SELECT DISTINCT v.`attribute_id`, v.`value_text`, NULL, 0, 1
FROM `product_attribute_values` v
LEFT JOIN `product_attribute_options` o
  ON o.`attribute_id` = v.`attribute_id`
 AND o.`display_value` = v.`value_text`
WHERE v.`value_text` IS NOT NULL
  AND o.`id` IS NULL;

INSERT INTO `product_attribute_options` (`attribute_id`, `display_value`, `normalized_value`, `sort_order`, `is_active`)
SELECT DISTINCT v.`attribute_id`, CAST(v.`value_number` AS CHAR), CAST(v.`value_number` AS SIGNED), 0, 1
FROM `product_attribute_values` v
LEFT JOIN `product_attribute_options` o
  ON o.`attribute_id` = v.`attribute_id`
 AND o.`normalized_value` = CAST(v.`value_number` AS SIGNED)
WHERE v.`value_number` IS NOT NULL
  AND o.`id` IS NULL;

INSERT INTO `product_attribute_options` (`attribute_id`, `display_value`, `normalized_value`, `sort_order`, `is_active`)
SELECT DISTINCT v.`attribute_id`, DATE_FORMAT(v.`value_date`, '%Y-%m-%d'), NULL, 0, 1
FROM `product_attribute_values` v
LEFT JOIN `product_attribute_options` o
  ON o.`attribute_id` = v.`attribute_id`
 AND o.`display_value` = DATE_FORMAT(v.`value_date`, '%Y-%m-%d')
WHERE v.`value_date` IS NOT NULL
  AND o.`id` IS NULL;

INSERT INTO `product_attribute_options` (`attribute_id`, `display_value`, `normalized_value`, `sort_order`, `is_active`)
SELECT DISTINCT v.`attribute_id`, CASE v.`value_bool` WHEN 1 THEN '1' WHEN 0 THEN '0' END, v.`value_bool`, 0, 1
FROM `product_attribute_values` v
LEFT JOIN `product_attribute_options` o
  ON o.`attribute_id` = v.`attribute_id`
 AND o.`normalized_value` = v.`value_bool`
WHERE v.`value_bool` IS NOT NULL
  AND o.`id` IS NULL;

-- Backfill the new option_id column
UPDATE `product_attribute_values` v
JOIN `product_attribute_options` o
  ON o.`attribute_id` = v.`attribute_id`
 AND v.`value_text` IS NOT NULL
 AND o.`display_value` = v.`value_text`
SET v.`option_id` = o.`id`
WHERE v.`value_text` IS NOT NULL;

UPDATE `product_attribute_values` v
JOIN `product_attribute_options` o
  ON o.`attribute_id` = v.`attribute_id`
 AND v.`value_number` IS NOT NULL
 AND o.`normalized_value` = CAST(v.`value_number` AS SIGNED)
SET v.`option_id` = o.`id`
WHERE v.`value_number` IS NOT NULL
  AND v.`option_id` IS NULL;

UPDATE `product_attribute_values` v
JOIN `product_attribute_options` o
  ON o.`attribute_id` = v.`attribute_id`
 AND v.`value_date` IS NOT NULL
 AND o.`display_value` = DATE_FORMAT(v.`value_date`, '%Y-%m-%d')
SET v.`option_id` = o.`id`
WHERE v.`value_date` IS NOT NULL
  AND v.`option_id` IS NULL;

UPDATE `product_attribute_values` v
JOIN `product_attribute_options` o
  ON o.`attribute_id` = v.`attribute_id`
 AND v.`value_bool` IS NOT NULL
 AND o.`normalized_value` = v.`value_bool`
SET v.`option_id` = o.`id`
WHERE v.`value_bool` IS NOT NULL
  AND v.`option_id` IS NULL;

-- Add FK once data is aligned
ALTER TABLE `product_attribute_values`
  ADD INDEX `idx_product_attribute_value_option` (`option_id`);

ALTER TABLE `product_attribute_values`
  ADD CONSTRAINT `product_attribute_values_ibfk_3`
    FOREIGN KEY (`option_id`) REFERENCES `product_attribute_options` (`id`);

COMMIT;
