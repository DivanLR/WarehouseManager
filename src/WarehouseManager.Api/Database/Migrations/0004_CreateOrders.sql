CREATE TABLE IF NOT EXISTS orders
(
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id               uuid NOT NULL,
    source_warehouse_id      uuid NOT NULL,
    destination_warehouse_id uuid NOT NULL,
    quantity                 integer NOT NULL,
    created_at               timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_orders_product FOREIGN KEY (product_id) REFERENCES products (id),
    CONSTRAINT fk_orders_source_warehouse FOREIGN KEY (source_warehouse_id) REFERENCES warehouses (id),
    CONSTRAINT fk_orders_destination_warehouse FOREIGN KEY (destination_warehouse_id) REFERENCES warehouses (id),
    CONSTRAINT ck_orders_quantity_positive CHECK (quantity > 0),
    CONSTRAINT ck_orders_distinct_warehouses CHECK (source_warehouse_id <> destination_warehouse_id)
);
