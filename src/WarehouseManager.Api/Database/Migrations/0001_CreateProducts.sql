CREATE TABLE IF NOT EXISTS products
(
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code        text NOT NULL,
    description text NOT NULL,
    CONSTRAINT uq_products_code UNIQUE (code)
);
