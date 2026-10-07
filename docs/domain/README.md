# Initial domain

Site (name, city, district, address) → Block (siteId, name) → Apartment (blockId, number, floor, grossSquareMeters).

Names/numbers are trimmed and required. Blocks and apartments are unique within their parent, ignoring case. Negative floor values support basements. Area must be positive and fit the database decimal precision. Collections are exposed through read-only wrappers. All entities inherit the mandatory BaseEntity.
