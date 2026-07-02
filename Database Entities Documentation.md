# Database Entities Documentation

## Overview
This document provides a comprehensive overview of all TypeORM entities in the system. Each entity extends the `AbstractEntity` class which provides common timestamp and audit fields.

## AbstractEntity
Base class providing common fields for all entities.

| Field | Type | Description |
|-------|------|-------------|
| `createdt` | Date | Creation timestamp (default: current date) |
| `createdby` | string | User who created the record |
| `updatedt` | Date | Last update timestamp (nullable) |
| `updatedby` | string | User who last updated the record (nullable) |

---

## Entities

### Antennas
Represents antenna/network access points.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `code` | string(60) | Antenna code |
| `label` | string(100) | Antenna label |
| `district` | number | Associated district ID |
| `status` | string(20) | Current status |

---

### Branches
Represents organizational branches.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `code` | string(60) | Branch code |
| `label` | string(100) | Branch label |
| `district` | number | Associated district ID |
| `status` | string(20) | Current status |

---

### Cities
Represents cities/communes.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `code` | string(10) | City code |
| `label` | string(150) | City name |
| `province` | number | Associated province ID |
| `status` | string(20) | Current status |

---

### Civilities
Represents title/civility options (Mr., Mrs., Dr., etc.).

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `code` | string(60) | Civility code |
| `label` | string(100) | Civility label |
| `status` | string(20) | Current status |

---

### Configs
Represents system configuration settings.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `name` | string(200) | Configuration label |
| `status` | string(20) | Current status |

---

### Continents
Represents continents.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `code` | string(10) | Continent code |
| `label` | string(100) | Continent name |
| `status` | string(20) | Current status |

---

### Countries
Represents countries with nationality information.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `code` | string(10) | Country code |
| `labelCountry` | string(200) | Country name |
| `labelNationality` | string(210) | Nationality adjective |
| `continent` | number | Associated continent ID |
| `status` | string(20) | Current status |

---

### Districts
Represents administrative districts.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `code` | string(10) | District code |
| `label` | string(150) | District name |
| `city` | number | Associated city ID |
| `status` | string(20) | Current status |

---

### Members
Represents system members with unique constraints.

**Unique Constraints:**
- (`lastName`, `firstName`)

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `reference` | string(60) | Member reference |
| `entryDate` | Date | Entry date |
| `lastName` | string(200) | Last name |
| `firstName` | string(200) | First name |
| `gender` | string(6) | Gender (M/F) |
| `introducer` | number | Introducer member ID (nullable) |
| `birthDate` | Date | Birth date |
| `birthPlace` | number | Birth place city ID |
| `birthCity` | number | Birth city ID |
| `mobile` | string(255) | Mobile phone number |
| `email` | string(255) | Email address |
| `address` | string(255) | Physical address |
| `district` | number | Residence district ID |
| `nationality` | number | Nationality ID |
| `status` | string(20) | Current status |

---

### Provinces
Represents provinces/regions.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `code` | string(10) | Province code |
| `label` | string(150) | Province name |
| `country` | number | Associated country ID |
| `status` | string(20) | Current status |

---

### Ranks
Represents member ranks/hierarchy levels.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `label` | string(200) | Rank label |
| `status` | string(20) | Current status |

---

### Sponsorships
Represents sponsorship relationships between members.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `child` | number | Child member ID |
| `sponsor` | number | Sponsor member ID |
| `startDate` | Date | Sponsorship start date (default: current date) |
| `endDate` | Date | Sponsorship end date (nullable) |
| `status` | string(20) | Current status |

---

### Zones
Represents geographic zones within districts.

| Field | Type | Description |
|-------|------|-------------|
| `id` | number | Primary key (auto-generated) |
| `code` | string(60) | Zone code |
| `label` | string(100) | Zone label |
| `district` | number | Associated district ID |
| `status` | string(20) | Current status |

---

## Entity Relationships

### Geographic Hierarchy
1. **Continents** (1) → **Countries** (N)
2. **Countries** (1) → **Provinces** (N)
3. **Provinces** (1) → **Cities** (N)
4. **Cities** (1) → **Districts** (N)
5. **Districts** (1) → **Zones** (N) and **Antennas** (N) and **Branches** (N)

### Member Relationships
- **Members** can have a self-referencing relationship via `introducer` field
- **Sponsorships** links two **Members** as child/sponsor
- **Members** reference geographic entities:
  - `birthPlace` → Cities
  - `birthCity` → Cities
  - `district` → Districts
  - `nationality` → Countries

---

## Migration Notes

When creating migrations for these entities, consider:

1. **Foreign Key Constraints**: All `number` fields that reference other entities should have foreign key constraints
2. **Indexes**: Consider indexes on frequently queried fields:
   - Members: `reference`, `lastName`, `firstName`, `email`, `mobile`
   - All status fields for filtering
3. **Default Values**: 
   - `createdt` defaults to current date
   - `startDate` in Sponsorships defaults to current date
4. **Nullable Fields**: 
   - `updatedt`, `updatedby`
   - `introducer` in Members
   - `endDate` in Sponsorships

---

# Technical Documentation: Database Entities

## Architecture Overview

### Design Patterns

**1. Base Entity Pattern (AbstractEntity)**
- All entities inherit from `AbstractEntity` to ensure consistency
- Provides automatic timestamp tracking for auditing
- Tracks creation and modification users

**2. Active Record Pattern**
- Using TypeORM's decorator-based entity definitions
- Entities represent both database tables and domain models

### Technology Stack
- **ORM**: TypeORM
- **Database**: Compatible with PostgreSQL, MySQL, SQLite, etc.
- **Language**: TypeScript

### Naming Conventions

| Convention | Example |
|------------|---------|
| Table names | Plural lowercase (`antennas`, `members`) |
| Entity class names | PascalCase, plural (`Antennas`, `Members`) |
| Property names | camelCase (`entryDate`, `lastName`) |
| Database column names | snake_case (`entry_date`, `last_name`) |
| Primary key | `id` (auto-generated integer) |
| Foreign keys | Singular reference name (`district`, `province`) |

### Audit Trail Implementation

The `AbstractEntity` provides built-in auditing:

```typescript
createdt: Date    // When record was created
createdby: string // Who created it
updatedt: Date    // When record was last modified
updatedby: string // Who last modified it