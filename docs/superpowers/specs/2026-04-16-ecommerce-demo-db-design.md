# E-commerce Demo SQLite Generator Design

## Goal

Add a zero-dependency Python script that generates a realistic SQLite demo database for Open DB Viewer.

The script should create a fixed `demo.db` file in the repository root, overwrite any existing file, and seed enough relational data to demonstrate:

- connection creation with SQLite
- object tree browsing
- schema inspection
- paginated table browsing
- SQL filtering, sorting, aggregation, and joins

## Scope

This work covers:

- one Python script using only the standard library
- one generated SQLite database file format
- one light e-commerce domain model
- enough seeded volume to feel real without turning into a stress test

This work does not cover:

- configurable CLI arguments
- non-SQLite targets
- external fake-data libraries
- migrations or versioned schema evolution
- benchmark-grade large datasets

## Recommended Approach

Use a single standard-library Python script at `scripts/generate_demo_db.py`.

This script will:

1. remove an existing `demo.db` in the repository root if present
2. create a fresh SQLite database
3. create the schema
4. insert deterministic pseudo-random data with a fixed seed
5. print a short completion summary to stdout

This approach is preferred because it is:

- zero dependency
- easy to run on any machine with Python
- easy to inspect and modify
- aligned with the repo’s current lightweight setup

## Output Contract

Running the script from the repository root should produce:

- file: `C:/Code_Research/Open.Db.Viewer/demo.db`

If `demo.db` already exists, it should be deleted first so the result is always a clean rebuild.

The script should exit non-zero only on actual generation failure.

## Data Model

The demo database will model a simple e-commerce system with the following tables:

### `customers`

Stores buyers and account metadata.

Key columns:

- `id`
- `customer_code`
- `full_name`
- `email`
- `phone`
- `segment`
- `status`
- `created_at`

### `addresses`

Stores one or more delivery addresses per customer.

Key columns:

- `id`
- `customer_id`
- `recipient_name`
- `phone`
- `country`
- `province`
- `city`
- `district`
- `street`
- `postal_code`
- `is_default`

### `categories`

Stores product categories.

Key columns:

- `id`
- `name`
- `parent_name`
- `sort_order`

### `products`

Stores catalog products.

Key columns:

- `id`
- `sku`
- `name`
- `category_id`
- `brand`
- `price`
- `cost`
- `stock_qty`
- `status`
- `rating`
- `created_at`

### `orders`

Stores order headers.

Key columns:

- `id`
- `order_no`
- `customer_id`
- `address_id`
- `order_status`
- `payment_status`
- `shipment_status`
- `subtotal_amount`
- `discount_amount`
- `shipping_amount`
- `total_amount`
- `placed_at`
- `paid_at`

### `order_items`

Stores order lines.

Key columns:

- `id`
- `order_id`
- `product_id`
- `sku`
- `product_name`
- `unit_price`
- `quantity`
- `discount_amount`
- `line_total`

### `payments`

Stores payment records per order.

Key columns:

- `id`
- `order_id`
- `payment_method`
- `payment_channel`
- `amount`
- `payment_status`
- `transaction_no`
- `paid_at`

### `shipments`

Stores shipping records per order.

Key columns:

- `id`
- `order_id`
- `carrier`
- `tracking_no`
- `shipment_status`
- `shipped_at`
- `delivered_at`

## Relational Rules

- each customer has 1 to 3 addresses
- each product belongs to one category
- each order belongs to one customer
- each order points to one customer address
- each order has 1 to 5 order items
- each paid or refunded order has one payment row
- each shipped or delivered order has one shipment row

Foreign keys should be created for the main parent-child links so schema browsing shows real relationships.

## Dataset Size

This is a light demo dataset, not a benchmark dataset.

Target volumes:

- `customers`: 5,000
- `addresses`: about 8,000 to 10,000
- `categories`: about 20
- `products`: 2,000
- `orders`: 20,000
- `order_items`: about 60,000
- `payments`: about 15,000 to 18,000
- `shipments`: about 14,000 to 17,000

These numbers are intentionally large enough to demonstrate pagination and query usefulness while still generating quickly on a normal developer machine.

## Data Characteristics

The dataset should look plausible rather than purely random.

### Customers

- mixed `segment` values such as `new`, `repeat`, `vip`, `enterprise`
- mixed `status` values such as `active`, `inactive`
- realistic name and email combinations

### Products

- category-driven product names
- brands drawn from a fixed curated list
- prices distributed by category rather than fully uniform
- stock quantities ranging from low stock to healthy inventory

### Orders

- timestamps spread over roughly the last 18 months
- order amounts derived from order items rather than invented separately
- consistent status combinations

Examples:

- `pending` orders should usually be unpaid and unshipped
- `paid` orders may still be awaiting shipment
- `delivered` orders should have both payment and shipment completion timestamps
- `cancelled` orders should not have shipment completion

### Payments

- channels such as `alipay`, `wechat_pay`, `card`, `bank_transfer`
- successful payment records only for orders that logically reached payment
- refunded orders may still have a paid payment row and a refunded order state

### Shipments

- carriers from a fixed list
- shipment and delivery timestamps must be later than order placement

## Determinism

Use a fixed random seed so repeated runs generate the same dataset shape and similar values.

This matters because:

- screenshots stay stable
- local debugging becomes reproducible
- schema and sample queries remain predictable

## Performance Strategy

Use standard SQLite bulk-loading practices:

- wrap inserts in transactions
- use `executemany` for batched inserts
- create indexes after core table creation where useful for demo browsing

Recommended indexes:

- `orders(customer_id)`
- `orders(placed_at)`
- `orders(order_status)`
- `order_items(order_id)`
- `order_items(product_id)`
- `payments(order_id)`
- `shipments(order_id)`
- `products(category_id)`

The goal is not maximum optimization, only enough responsiveness for demo use in the viewer.

## Script Structure

The implementation should remain in one file, but internally be separated into focused functions:

- `create_schema`
- `seed_categories`
- `seed_products`
- `seed_customers`
- `seed_addresses`
- `seed_orders_and_items`
- `seed_payments`
- `seed_shipments`
- `main`

This keeps the single-file delivery simple without turning the script into a monolith that is hard to edit.

## Verification

Minimum verification for implementation:

1. run the script with the local Python interpreter
2. verify `demo.db` is created at repo root
3. verify main table counts are non-zero and roughly match targets
4. open the generated database in Open DB Viewer and confirm:
   - tables are visible
   - schema loads
   - table data pagination works
   - a multi-table join query returns results

## Example Demo Queries

The seeded schema should support queries like:

```sql
select order_status, count(*) as orders
from orders
group by order_status
order by orders desc;
```

```sql
select o.order_no, c.full_name, o.total_amount, o.placed_at
from orders o
join customers c on c.id = o.customer_id
order by o.placed_at desc
limit 50;
```

```sql
select p.name, sum(oi.quantity) as sold_qty
from order_items oi
join products p on p.id = oi.product_id
group by p.id, p.name
order by sold_qty desc
limit 20;
```

## Risks and Mitigations

### Risk: generation is too slow

Mitigation:

- keep dataset in the light range
- use transactions and batch inserts
- avoid unnecessary per-row queries during generation

### Risk: data relationships become inconsistent

Mitigation:

- derive payments and shipments from created orders
- compute money fields from order items
- keep state transitions rule-based

### Risk: data feels too synthetic

Mitigation:

- use curated category, brand, city, and status vocabularies
- bias values by business rules rather than pure uniform randomness

## Acceptance Criteria

The design is complete when implementation can produce a `demo.db` that:

- is created by one Python script using only the standard library
- overwrites any existing root-level `demo.db`
- contains the eight planned tables
- contains tens of thousands of rows overall
- supports realistic browsing and SQL demos inside Open DB Viewer
