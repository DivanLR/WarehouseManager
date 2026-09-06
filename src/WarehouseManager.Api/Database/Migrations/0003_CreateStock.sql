CREATE TABLE IF NOT EXISTS stock
(
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    warehouse_id uuid NOT NULL,
    product_id   uuid NOT NULL,
    quantity     integer NOT NULL DEFAULT 0,
    CONSTRAINT fk_stock_warehouse FOREIGN KEY (warehouse_id) REFERENCES warehouses (id),
    CONSTRAINT fk_stock_product FOREIGN KEY (product_id) REFERENCES products (id),
    CONSTRAINT uq_stock_warehouse_product UNIQUE (warehouse_id, product_id)
);
