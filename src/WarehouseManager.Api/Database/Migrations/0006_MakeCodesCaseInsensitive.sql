ALTER TABLE products DROP CONSTRAINT IF EXISTS uq_products_code;
CREATE UNIQUE INDEX IF NOT EXISTS uq_products_code ON products (lower(code));

ALTER TABLE warehouses DROP CONSTRAINT IF EXISTS uq_warehouses_code;
CREATE UNIQUE INDEX IF NOT EXISTS uq_warehouses_code ON warehouses (lower(code));
