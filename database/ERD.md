# Entity Relationship Diagram (ERD) & Database Architecture

## Overview
The database schema for Anglo-Eastern Ship Management is designed in Microsoft SQL Server (T-SQL). It implements normalization (3NF), integrity constraints (non-negative financial amounts, date ranges, fiscal year formats), cascading referential integrity where appropriate, and covering indexes for reporting performance.

---

## Mermaid ERD

```mermaid
erDiagram
    SHIPS ||--o{ USER_SHIP_ASSIGNMENTS : "assigned to"
    USERS ||--o{ USER_SHIP_ASSIGNMENTS : "has"
    SHIPS ||--o{ CREW_SERVICE_HISTORIES : "serves on"
    CREW_MEMBERS ||--o{ CREW_SERVICE_HISTORIES : "signs"
    RANKS ||--o{ CREW_SERVICE_HISTORIES : "assigned"
    SHIPS ||--o{ BUDGETS : "budgeted for"
    SHIPS ||--o{ ACCOUNT_TRANSACTIONS : "incurs"
    CHART_OF_ACCOUNTS ||--o{ CHART_OF_ACCOUNTS : "parent of"
    CHART_OF_ACCOUNTS ||--o{ BUDGETS : "allocates"
    CHART_OF_ACCOUNTS ||--o{ ACCOUNT_TRANSACTIONS : "categorizes"

    SHIPS {
        varchar(20) ShipCode PK
        nvarchar(100) Name
        varchar(4) FiscalYear "e.g. 0112, 0403"
        varchar(20) Status "Active | Inactive"
        datetime2 CreatedAt
        datetime2 UpdatedAt
    }

    USERS {
        int UserId PK
        nvarchar(50) Username UK
        nvarchar(100) FullName
        nvarchar(100) Email UK
        nvarchar(50) Role
        nvarchar(255) PasswordHash
        bit IsActive
        datetime2 CreatedAt
    }

    USER_SHIP_ASSIGNMENTS {
        int UserId PK,FK
        varchar(20) ShipCode PK,FK
        datetime2 AssignedAt
    }

    RANKS {
        varchar(20) RankCode PK
        nvarchar(50) RankName UK
        nvarchar(50) Department "Deck | Engine | Catering"
        int SeniorityLevel
    }

    CREW_MEMBERS {
        varchar(20) CrewMemberId PK
        nvarchar(50) FirstName
        nvarchar(50) LastName
        date BirthDate
        nvarchar(50) Nationality
        datetime2 CreatedAt
    }

    CREW_SERVICE_HISTORIES {
        int ServiceHistoryId PK
        varchar(20) CrewMemberId FK
        varchar(20) ShipCode FK
        varchar(20) RankCode FK
        date SignOnDate
        date SignOffDate "NULL if active"
        date EndOfContractDate
        nvarchar(255) Notes
    }

    CHART_OF_ACCOUNTS {
        varchar(20) AccountNumber PK
        nvarchar(150) Description
        varchar(20) ParentAccountNumber FK "Self-referencing"
        varchar(10) AccountType "Parent | Child"
        int HierarchyLevel
    }

    BUDGETS {
        int BudgetId PK
        varchar(20) ShipCode FK
        varchar(20) AccountNumber FK
        varchar(7) AccountPeriod "YYYY-MM"
        decimal(18_2) BudgetValue ">= 0"
    }

    ACCOUNT_TRANSACTIONS {
        int TransactionId PK
        varchar(20) ShipCode FK
        varchar(20) AccountNumber FK
        varchar(7) AccountPeriod "YYYY-MM"
        decimal(18_2) ActualValue ">= 0"
        date TransactionDate
        nvarchar(255) Description
    }
```

---

## Entity Descriptions & Data Integrity Rules

### 1. `Ships`
- **Primary Key:** `ShipCode` (e.g. `SHIP01`).
- **FiscalYear:** 4-character code (`0112` = Jan to Dec, `0403` = Apr to Mar). Enforced via regex pattern check and range validation `[01-12]`.
- **Status:** Constrained to `('Active', 'Inactive')`. Only active ships are returned in operational endpoints and crew lists.

### 2. `Users` & `UserShipAssignments`
- Many-to-many relationship allowing users (superintendents, fleet managers, crew coordinators) to be linked to multiple ships.
- `UserShipAssignments` composite PK `(UserId, ShipCode)` with cascading delete.

### 3. `Ranks`, `CrewMembers`, and `CrewServiceHistories`
- `Ranks`: Standard maritime ranks across Deck, Engine, Catering departments.
- `CrewMembers`: Biographical info (First/Last name, BirthDate, Nationality).
- `CrewServiceHistories`: Service contracts per ship with `SignOnDate`, `SignOffDate`, and `EndOfContractDate`.
- **Status Rule (Operational View):**
  - `SignOffDate IS NOT NULL` $\to$ Signed Off
  - `SignOnDate > AsOfDate` $\to$ Planned
  - `SignOffDate IS NULL AND DATEDIFF(day, EndOfContractDate, AsOfDate) > 30` $\to$ **Relief Due**
  - `SignOffDate IS NULL AND SignOnDate <= AsOfDate` $\to$ **Onboard**

### 4. `ChartOfAccounts`, `Budgets`, and `AccountTransactions`
- `ChartOfAccounts`: Hierarchical self-referencing tree structure. Leaf nodes have `AccountType = 'Child'` and summary accounts have `AccountType = 'Parent'`.
- `Budgets` & `AccountTransactions`: Recorded strictly at the child account level. Check constraints guarantee `BudgetValue >= 0` and `ActualValue >= 0`.
- Summary aggregation is computed dynamically via recursive CTEs up through the COA hierarchy.
