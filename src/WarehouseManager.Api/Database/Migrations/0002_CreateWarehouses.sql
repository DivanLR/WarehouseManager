CREATE TABLE IF NOT EXISTS warehouses
(
    id   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code text NOT NULL,
    name text NOT NULL,
    CONSTRAINT uq_warehouses_code UNIQUE (code)
);
