-- ============================================================================
-- Anglo-Eastern Backend Code Challenge
-- Script 01: Database Schema (DDL)
-- Target: Microsoft SQL Server 2019+ / Azure SQL Database
-- ============================================================================

-- Ensure clean database creation or context
IF DB_ID('ShipManagementDb') IS NULL
BEGIN
    CREATE DATABASE ShipManagementDb;
END
GO

USE ShipManagementDb;
GO

-- Drop tables in reverse foreign key order if re-running
IF OBJECT_ID('dbo.AccountTransactions', 'U') IS NOT NULL DROP TABLE dbo.AccountTransactions;
IF OBJECT_ID('dbo.Budgets', 'U') IS NOT NULL DROP TABLE dbo.Budgets;
IF OBJECT_ID('dbo.ChartOfAccounts', 'U') IS NOT NULL DROP TABLE dbo.ChartOfAccounts;
IF OBJECT_ID('dbo.CrewServiceHistories', 'U') IS NOT NULL DROP TABLE dbo.CrewServiceHistories;
IF OBJECT_ID('dbo.CrewMembers', 'U') IS NOT NULL DROP TABLE dbo.CrewMembers;
IF OBJECT_ID('dbo.Ranks', 'U') IS NOT NULL DROP TABLE dbo.Ranks;
IF OBJECT_ID('dbo.UserShipAssignments', 'U') IS NOT NULL DROP TABLE dbo.UserShipAssignments;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Ships', 'U') IS NOT NULL DROP TABLE dbo.Ships;
GO

-- ============================================================================
-- 1. Ships Table
-- ============================================================================
CREATE TABLE dbo.Ships (
    ShipCode VARCHAR(20) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    FiscalYear VARCHAR(4) NOT NULL,
    Status VARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Ships_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT PK_Ships PRIMARY KEY CLUSTERED (ShipCode),
    CONSTRAINT CK_Ships_FiscalYear_Format CHECK (
        FiscalYear LIKE '[0-1][0-9][0-1][0-9]'
        AND CAST(SUBSTRING(FiscalYear, 1, 2) AS INT) BETWEEN 1 AND 12
        AND CAST(SUBSTRING(FiscalYear, 3, 2) AS INT) BETWEEN 1 AND 12
    ),
    CONSTRAINT CK_Ships_Status CHECK (Status IN ('Active', 'Inactive'))
);
GO

-- ============================================================================
-- 2. Users Table
-- ============================================================================
CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) NOT NULL,
    Username NVARCHAR(50) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(255) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

-- ============================================================================
-- 3. UserShipAssignments Table (Many-to-Many)
-- ============================================================================
CREATE TABLE dbo.UserShipAssignments (
    UserId INT NOT NULL,
    ShipCode VARCHAR(20) NOT NULL,
    AssignedAt DATETIME2 NOT NULL CONSTRAINT DF_UserShipAssignments_AssignedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_UserShipAssignments PRIMARY KEY CLUSTERED (UserId, ShipCode),
    CONSTRAINT FK_UserShipAssignments_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UserShipAssignments_Ships FOREIGN KEY (ShipCode)
        REFERENCES dbo.Ships (ShipCode) ON DELETE CASCADE
);
GO

-- ============================================================================
-- 4. Ranks Reference Table
-- ============================================================================
CREATE TABLE dbo.Ranks (
    RankCode VARCHAR(20) NOT NULL,
    RankName NVARCHAR(50) NOT NULL,
    Department NVARCHAR(50) NOT NULL,
    SeniorityLevel INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Ranks_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Ranks PRIMARY KEY CLUSTERED (RankCode),
    CONSTRAINT UQ_Ranks_RankName UNIQUE (RankName),
    CONSTRAINT CK_Ranks_Department CHECK (Department IN ('Deck', 'Engine', 'Catering', 'Other'))
);
GO

-- ============================================================================
-- 5. CrewMembers Table
-- ============================================================================
CREATE TABLE dbo.CrewMembers (
    CrewMemberId VARCHAR(20) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    BirthDate DATE NOT NULL,
    Nationality NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CrewMembers_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT PK_CrewMembers PRIMARY KEY CLUSTERED (CrewMemberId),
    CONSTRAINT CK_CrewMembers_BirthDate CHECK (BirthDate < CAST(GETDATE() AS DATE))
);
GO

-- ============================================================================
-- 6. CrewServiceHistories Table
-- ============================================================================
CREATE TABLE dbo.CrewServiceHistories (
    ServiceHistoryId INT IDENTITY(1,1) NOT NULL,
    CrewMemberId VARCHAR(20) NOT NULL,
    ShipCode VARCHAR(20) NOT NULL,
    RankCode VARCHAR(20) NOT NULL,
    SignOnDate DATE NOT NULL,
    SignOffDate DATE NULL,
    EndOfContractDate DATE NOT NULL,
    Notes NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CrewServiceHistories_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_CrewServiceHistories PRIMARY KEY CLUSTERED (ServiceHistoryId),
    CONSTRAINT FK_CrewServiceHistories_CrewMembers FOREIGN KEY (CrewMemberId)
        REFERENCES dbo.CrewMembers (CrewMemberId) ON DELETE CASCADE,
    CONSTRAINT FK_CrewServiceHistories_Ships FOREIGN KEY (ShipCode)
        REFERENCES dbo.Ships (ShipCode) ON DELETE CASCADE,
    CONSTRAINT FK_CrewServiceHistories_Ranks FOREIGN KEY (RankCode)
        REFERENCES dbo.Ranks (RankCode),
    CONSTRAINT CK_CrewServiceHistories_Dates CHECK (
        EndOfContractDate >= SignOnDate
        AND (SignOffDate IS NULL OR SignOffDate >= SignOnDate)
    )
);
GO

-- Index for operational crew queries (filtered on active service)
CREATE NONCLUSTERED INDEX IX_CrewServiceHistories_Ship_Active
ON dbo.CrewServiceHistories (ShipCode, SignOffDate, SignOnDate)
INCLUDE (CrewMemberId, RankCode, EndOfContractDate);
GO
-- Trigger enforcing strict Rank Exclusivity (No overlapping active contracts for same ship and rank)
CREATE OR ALTER TRIGGER dbo.TR_CrewServiceHistories_NoRankOverlap
ON dbo.CrewServiceHistories
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM dbo.CrewServiceHistories existing
        INNER JOIN inserted i
            ON existing.ShipCode = i.ShipCode
           AND existing.RankCode = i.RankCode
           AND existing.ServiceHistoryId <> i.ServiceHistoryId
        WHERE
            existing.SignOnDate <= ISNULL(i.SignOffDate, i.EndOfContractDate)
            AND i.SignOnDate <= ISNULL(existing.SignOffDate, existing.EndOfContractDate)
    )
    BEGIN
        RAISERROR('Rank exclusivity violation: Only one crew member may hold the specified rank on board a ship during a contract period. Overlapping dates are not permitted.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO


-- ============================================================================
-- 7. ChartOfAccounts (COA) Table
-- ============================================================================
CREATE TABLE dbo.ChartOfAccounts (
    AccountNumber VARCHAR(20) NOT NULL,
    Description NVARCHAR(150) NOT NULL,
    ParentAccountNumber VARCHAR(20) NULL,
    AccountType VARCHAR(10) NOT NULL,
    HierarchyLevel INT NOT NULL CONSTRAINT DF_ChartOfAccounts_HierarchyLevel DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChartOfAccounts_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ChartOfAccounts PRIMARY KEY CLUSTERED (AccountNumber),
    CONSTRAINT FK_ChartOfAccounts_Parent FOREIGN KEY (ParentAccountNumber)
        REFERENCES dbo.ChartOfAccounts (AccountNumber),
    CONSTRAINT CK_ChartOfAccounts_Type CHECK (AccountType IN ('Parent', 'Child')),
    CONSTRAINT CK_ChartOfAccounts_Level CHECK (HierarchyLevel >= 1)
);
GO

CREATE NONCLUSTERED INDEX IX_ChartOfAccounts_Parent
ON dbo.ChartOfAccounts (ParentAccountNumber)
INCLUDE (Description, AccountType, HierarchyLevel);
GO

-- ============================================================================
-- 8. Budgets Table
-- ============================================================================
CREATE TABLE dbo.Budgets (
    BudgetId INT IDENTITY(1,1) NOT NULL,
    ShipCode VARCHAR(20) NOT NULL,
    AccountNumber VARCHAR(20) NOT NULL,
    AccountPeriod VARCHAR(7) NOT NULL, -- Format: YYYY-MM
    BudgetValue DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Budgets_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Budgets PRIMARY KEY CLUSTERED (BudgetId),
    CONSTRAINT FK_Budgets_Ships FOREIGN KEY (ShipCode)
        REFERENCES dbo.Ships (ShipCode) ON DELETE CASCADE,
    CONSTRAINT FK_Budgets_ChartOfAccounts FOREIGN KEY (AccountNumber)
        REFERENCES dbo.ChartOfAccounts (AccountNumber),
    CONSTRAINT CK_Budgets_NonNegative CHECK (BudgetValue >= 0),
    CONSTRAINT CK_Budgets_Period_Format CHECK (
        AccountPeriod LIKE '[1-2][0-9][0-9][0-9]-[0-1][0-9]'
        AND CAST(SUBSTRING(AccountPeriod, 6, 2) AS INT) BETWEEN 1 AND 12
    )
);
GO

CREATE NONCLUSTERED INDEX IX_Budgets_Ship_Period
ON dbo.Budgets (ShipCode, AccountPeriod)
INCLUDE (AccountNumber, BudgetValue);
GO

-- ============================================================================
-- 9. AccountTransactions (Actuals) Table
-- ============================================================================
CREATE TABLE dbo.AccountTransactions (
    TransactionId INT IDENTITY(1,1) NOT NULL,
    ShipCode VARCHAR(20) NOT NULL,
    AccountNumber VARCHAR(20) NOT NULL,
    AccountPeriod VARCHAR(7) NOT NULL, -- Format: YYYY-MM
    ActualValue DECIMAL(18,2) NOT NULL,
    TransactionDate DATE NOT NULL,
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AccountTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_AccountTransactions PRIMARY KEY CLUSTERED (TransactionId),
    CONSTRAINT FK_AccountTransactions_Ships FOREIGN KEY (ShipCode)
        REFERENCES dbo.Ships (ShipCode) ON DELETE CASCADE,
    CONSTRAINT FK_AccountTransactions_ChartOfAccounts FOREIGN KEY (AccountNumber)
        REFERENCES dbo.ChartOfAccounts (AccountNumber),
    CONSTRAINT CK_AccountTransactions_NonNegative CHECK (ActualValue >= 0),
    CONSTRAINT CK_AccountTransactions_Period_Format CHECK (
        AccountPeriod LIKE '[1-2][0-9][0-9][0-9]-[0-1][0-9]'
        AND CAST(SUBSTRING(AccountPeriod, 6, 2) AS INT) BETWEEN 1 AND 12
    )
);
GO

CREATE NONCLUSTERED INDEX IX_AccountTransactions_Ship_Period
ON dbo.AccountTransactions (ShipCode, AccountPeriod)
INCLUDE (AccountNumber, ActualValue, TransactionDate);
GO
