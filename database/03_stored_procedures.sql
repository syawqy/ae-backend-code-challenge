-- ============================================================================
-- Anglo-Eastern Backend Code Challenge
-- Script 03: Stored Procedures (Data Access Layer)
-- Target: Microsoft SQL Server 2019+ / Azure SQL Database
-- ============================================================================

USE ShipManagementDb;
GO

-- Drop existing stored procedures if re-running
IF OBJECT_ID('dbo.sp_GetCrewList', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetCrewList;
IF OBJECT_ID('dbo.sp_GetFinancialReportDetail', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetFinancialReportDetail;
IF OBJECT_ID('dbo.sp_GetFinancialReportSummary', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetFinancialReportSummary;
IF OBJECT_ID('dbo.sp_CreateUser', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CreateUser;
IF OBJECT_ID('dbo.sp_GetUsers', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetUsers;
IF OBJECT_ID('dbo.sp_GetUserById', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetUserById;
IF OBJECT_ID('dbo.sp_GetUserByUsername', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetUserByUsername;
IF OBJECT_ID('dbo.sp_CreateShip', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CreateShip;
IF OBJECT_ID('dbo.sp_GetShips', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetShips;
IF OBJECT_ID('dbo.sp_GetShipByCode', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetShipByCode;
IF OBJECT_ID('dbo.sp_AssignShipToUser', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AssignShipToUser;
IF OBJECT_ID('dbo.sp_GetShipsByUserId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetShipsByUserId;
IF OBJECT_ID('dbo.sp_GetUsersByShipCode', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetUsersByShipCode;
GO

-- ============================================================================
-- 1. sp_GetCrewList
-- Returns crew currently onboard or relief due for a selected ship as of @AsOfDate
-- Supports: pagination, dynamic sorting, searching across non-status columns
-- ============================================================================
CREATE PROCEDURE dbo.sp_GetCrewList
    @ShipCode VARCHAR(20),
    @PageNumber INT = 1,
    @PageSize INT = 10,
    @SortColumn NVARCHAR(50) = 'SignOnDate',
    @SortDirection NVARCHAR(4) = 'ASC',
    @SearchTerm NVARCHAR(100) = NULL,
    @AsOfDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate pagination parameters
    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 SET @PageSize = 10;
    IF @PageSize > 100 SET @PageSize = 100;

    -- Normalize SortDirection
    IF UPPER(@SortDirection) NOT IN ('ASC', 'DESC')
        SET @SortDirection = 'ASC';

    -- Default AsOfDate to current date if not supplied
    IF @AsOfDate IS NULL
        SET @AsOfDate = CAST(GETDATE() AS DATE);

    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), '');

    ;WITH ActiveCrewCTE AS (
        SELECT
            r.RankName,
            cm.CrewMemberId,
            cm.FirstName,
            cm.LastName,
            dbo.fn_CalculateAge(cm.BirthDate, @AsOfDate) AS Age,
            cm.Nationality,
            csh.SignOnDate,
            dbo.fn_GetCrewStatus(csh.SignOnDate, csh.SignOffDate, csh.EndOfContractDate, @AsOfDate) AS Status,
            FORMAT(csh.SignOnDate, 'dd MMM yyyy') AS FormattedSignOnDate
        FROM dbo.CrewServiceHistories csh
        INNER JOIN dbo.CrewMembers cm ON csh.CrewMemberId = cm.CrewMemberId
        INNER JOIN dbo.Ranks r ON csh.RankCode = r.RankCode
        INNER JOIN dbo.Ships s ON csh.ShipCode = s.ShipCode
        WHERE csh.ShipCode = @ShipCode
          AND s.Status = 'Active'
          AND csh.SignOffDate IS NULL               -- Exclude signed off
          AND csh.SignOnDate <= @AsOfDate           -- Exclude planned future joiners
    ),
    FilteredCrewCTE AS (
        SELECT
            RankName,
            CrewMemberId,
            FirstName,
            LastName,
            Age,
            Nationality,
            SignOnDate,
            Status,
            FormattedSignOnDate
        FROM ActiveCrewCTE
        WHERE Status IN ('Onboard', 'Relief Due')
          AND (
              @SearchTerm IS NULL
              OR RankName LIKE '%' + @SearchTerm + '%'
              OR CrewMemberId LIKE '%' + @SearchTerm + '%'
              OR FirstName LIKE '%' + @SearchTerm + '%'
              OR LastName LIKE '%' + @SearchTerm + '%'
              OR Nationality LIKE '%' + @SearchTerm + '%'
              OR CAST(Age AS VARCHAR(10)) LIKE '%' + @SearchTerm + '%'
              OR FormattedSignOnDate LIKE '%' + @SearchTerm + '%'
          )
    ),
    CountedCTE AS (
        SELECT *, COUNT(*) OVER() AS TotalCount
        FROM FilteredCrewCTE
    )
    SELECT
        RankName,
        CrewMemberId,
        FirstName,
        LastName,
        Age,
        Nationality,
        FormattedSignOnDate AS SignOnDate,
        Status,
        TotalCount
    FROM CountedCTE
    ORDER BY
        -- Dynamic sorting with deterministic tie-breaker
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'Rank Name'   THEN RankName END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'Rank Name'   THEN RankName END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'Crew ID'     THEN CrewMemberId END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'Crew ID'     THEN CrewMemberId END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'First Name'  THEN FirstName END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'First Name'  THEN FirstName END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'Last Name'   THEN LastName END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'Last Name'   THEN LastName END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'Age'         THEN Age END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'Age'         THEN Age END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'Nationality' THEN Nationality END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'Nationality' THEN Nationality END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'SignOnDate'  THEN SignOnDate END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'SignOnDate'  THEN SignOnDate END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'Status'      THEN Status END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'Status'      THEN Status END DESC,
        CrewMemberId ASC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- ============================================================================
-- 2. sp_GetFinancialReportDetail
-- Returns detailed financial report with parent rollup and fiscal year YTD
-- ============================================================================
CREATE PROCEDURE dbo.sp_GetFinancialReportDetail
    @ShipCode VARCHAR(20),
    @AccountPeriod VARCHAR(7) -- Format: YYYY-MM
AS
BEGIN
    SET NOCOUNT ON;

    -- Fetch ship's fiscal year
    DECLARE @FiscalYear VARCHAR(4);
    SELECT @FiscalYear = FiscalYear
    FROM dbo.Ships
    WHERE ShipCode = @ShipCode AND Status = 'Active';

    IF @FiscalYear IS NULL
    BEGIN
        RAISERROR('Ship not found or is inactive.', 16, 1);
        RETURN;
    END

    -- Retrieve all YTD accounting periods into a temporary table
    DECLARE @YTDPeriods TABLE (AccountPeriod VARCHAR(7) PRIMARY KEY);
    INSERT INTO @YTDPeriods (AccountPeriod)
    SELECT AccountPeriod FROM dbo.fn_GetFiscalYearPeriods(@FiscalYear, @AccountPeriod);

    -- Build recursive hierarchy: map each account (parent or child) to all its leaf descendants
    ;WITH AccountLeaves AS (
        -- Leaf accounts map to themselves
        SELECT
            AccountNumber AS AncestorAccountNumber,
            AccountNumber AS LeafAccountNumber
        FROM dbo.ChartOfAccounts
        WHERE AccountType = 'Child'

        UNION ALL

        -- Parent accounts map to child leaves
        SELECT
            c.ParentAccountNumber AS AncestorAccountNumber,
            al.LeafAccountNumber
        FROM AccountLeaves al
        INNER JOIN dbo.ChartOfAccounts c ON al.AncestorAccountNumber = c.AccountNumber
        WHERE c.ParentAccountNumber IS NOT NULL
    ),
    -- Actuals aggregation per leaf account
    LeafActuals AS (
        SELECT
            at.AccountNumber,
            SUM(CASE WHEN at.AccountPeriod = @AccountPeriod THEN at.ActualValue ELSE 0 END) AS MonthlyActual,
            COUNT(CASE WHEN at.AccountPeriod = @AccountPeriod THEN 1 END) AS MonthlyActualCount,
            SUM(CASE WHEN p.AccountPeriod IS NOT NULL THEN at.ActualValue ELSE 0 END) AS YTDActual,
            COUNT(CASE WHEN p.AccountPeriod IS NOT NULL THEN 1 END) AS YTDActualCount
        FROM dbo.AccountTransactions at
        LEFT JOIN @YTDPeriods p ON at.AccountPeriod = p.AccountPeriod
        WHERE at.ShipCode = @ShipCode
        GROUP BY at.AccountNumber
    ),
    -- Budgets aggregation per leaf account
    LeafBudgets AS (
        SELECT
            b.AccountNumber,
            SUM(CASE WHEN b.AccountPeriod = @AccountPeriod THEN b.BudgetValue ELSE 0 END) AS MonthlyBudget,
            COUNT(CASE WHEN b.AccountPeriod = @AccountPeriod THEN 1 END) AS MonthlyBudgetCount,
            SUM(CASE WHEN p.AccountPeriod IS NOT NULL THEN b.BudgetValue ELSE 0 END) AS YTDBudget,
            COUNT(CASE WHEN p.AccountPeriod IS NOT NULL THEN 1 END) AS YTDBudgetCount
        FROM dbo.Budgets b
        LEFT JOIN @YTDPeriods p ON b.AccountPeriod = p.AccountPeriod
        WHERE b.ShipCode = @ShipCode
        GROUP BY b.AccountNumber
    ),
    -- Rollup to all accounts (parent and child)
    AccountRollup AS (
        SELECT
            coa.AccountNumber,
            coa.Description AS COADescription,
            coa.AccountType,
            coa.HierarchyLevel,
            coa.ParentAccountNumber,
            -- Monthly Actual: NULL if no actual transaction rows exist across leaves
            CASE
                WHEN SUM(COALESCE(la.MonthlyActualCount, 0)) = 0 THEN NULL
                ELSE SUM(COALESCE(la.MonthlyActual, 0))
            END AS Actual,
            -- Monthly Budget: NULL if no budget rows exist across leaves
            CASE
                WHEN SUM(COALESCE(lb.MonthlyBudgetCount, 0)) = 0 THEN NULL
                ELSE SUM(COALESCE(lb.MonthlyBudget, 0))
            END AS Budget,
            -- YTD Actual: NULL if no actual rows in YTD
            CASE
                WHEN SUM(COALESCE(la.YTDActualCount, 0)) = 0 THEN NULL
                ELSE SUM(COALESCE(la.YTDActual, 0))
            END AS ActualYTD,
            -- YTD Budget: NULL if no budget rows in YTD
            CASE
                WHEN SUM(COALESCE(lb.YTDBudgetCount, 0)) = 0 THEN NULL
                ELSE SUM(COALESCE(lb.YTDBudget, 0))
            END AS BudgetYTD
        FROM dbo.ChartOfAccounts coa
        INNER JOIN AccountLeaves al ON coa.AccountNumber = al.AncestorAccountNumber
        LEFT JOIN LeafActuals la ON al.LeafAccountNumber = la.AccountNumber
        LEFT JOIN LeafBudgets lb ON al.LeafAccountNumber = lb.AccountNumber
        GROUP BY coa.AccountNumber, coa.Description, coa.AccountType, coa.HierarchyLevel, coa.ParentAccountNumber
    )
    SELECT
        COADescription,
        AccountNumber,
        AccountType,
        HierarchyLevel,
        ParentAccountNumber,
        Actual,
        Budget,
        -- Variance Actual = Actual - Budget (treat NULL as 0 for variance computation if one is present)
        CASE
            WHEN Actual IS NULL AND Budget IS NULL THEN NULL
            ELSE COALESCE(Actual, 0) - COALESCE(Budget, 0)
        END AS VarianceActual,
        ActualYTD,
        BudgetYTD,
        -- Variance YTD = ActualYTD - BudgetYTD
        CASE
            WHEN ActualYTD IS NULL AND BudgetYTD IS NULL THEN NULL
            ELSE COALESCE(ActualYTD, 0) - COALESCE(BudgetYTD, 0)
        END AS VarianceYTD
    FROM AccountRollup
    WHERE
        -- Filter rule: Only include records where budget or actual values are non-zero
        (COALESCE(Actual, 0) <> 0 OR COALESCE(Budget, 0) <> 0
         OR COALESCE(ActualYTD, 0) <> 0 OR COALESCE(BudgetYTD, 0) <> 0)
    ORDER BY AccountNumber ASC;
END;
GO

-- ============================================================================
-- 3. sp_GetFinancialReportSummary
-- Returns overall YTD financial expense summary for a ship (not specific to any COA)
-- Covers all periods from fiscal year start up to the target @AccountPeriod
-- ============================================================================
CREATE PROCEDURE dbo.sp_GetFinancialReportSummary
    @ShipCode VARCHAR(20),
    @AccountPeriod VARCHAR(7)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FiscalYear VARCHAR(4);
    DECLARE @ShipName NVARCHAR(100);

    SELECT
        @FiscalYear = FiscalYear,
        @ShipName = Name
    FROM dbo.Ships
    WHERE ShipCode = @ShipCode AND Status = 'Active';

    IF @FiscalYear IS NULL
    BEGIN
        RAISERROR('Ship not found or is inactive.', 16, 1);
        RETURN;
    END

    -- Retrieve all YTD accounting periods
    DECLARE @YTDPeriods TABLE (AccountPeriod VARCHAR(7) PRIMARY KEY);
    INSERT INTO @YTDPeriods (AccountPeriod)
    SELECT AccountPeriod FROM dbo.fn_GetFiscalYearPeriods(@FiscalYear, @AccountPeriod);

    -- Pre-aggregate actual transactions by period for the ship
    ;WITH ActualsByPeriod AS (
        SELECT
            at.AccountPeriod,
            SUM(at.ActualValue) AS TotalActual
        FROM dbo.AccountTransactions at
        WHERE at.ShipCode = @ShipCode
        GROUP BY at.AccountPeriod
    ),
    -- Pre-aggregate budgets by period for the ship
    BudgetsByPeriod AS (
        SELECT
            b.AccountPeriod,
            SUM(b.BudgetValue) AS TotalBudget
        FROM dbo.Budgets b
        WHERE b.ShipCode = @ShipCode
        GROUP BY b.AccountPeriod
    ),
    -- Join to all YTD periods
    MonthlyData AS (
        SELECT
            p.AccountPeriod,
            COALESCE(a.TotalActual, 0) AS MonthlyActual,
            COALESCE(b.TotalBudget, 0) AS MonthlyBudget
        FROM @YTDPeriods p
        LEFT JOIN ActualsByPeriod a ON p.AccountPeriod = a.AccountPeriod
        LEFT JOIN BudgetsByPeriod b ON p.AccountPeriod = b.AccountPeriod
    ),
    CumulativeData AS (
        SELECT
            @ShipCode AS ShipCode,
            @ShipName AS ShipName,
            @FiscalYear AS FiscalYear,
            AccountPeriod,
            MonthlyActual,
            MonthlyBudget,
            MonthlyActual - MonthlyBudget AS MonthlyVariance,
            SUM(MonthlyActual) OVER(ORDER BY AccountPeriod ASC ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS CumulativeActualYTD,
            SUM(MonthlyBudget) OVER(ORDER BY AccountPeriod ASC ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS CumulativeBudgetYTD,
            SUM(MonthlyActual - MonthlyBudget) OVER(ORDER BY AccountPeriod ASC ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS CumulativeVarianceYTD
        FROM MonthlyData
    )
    SELECT
        ShipCode,
        ShipName,
        FiscalYear,
        AccountPeriod,
        MonthlyActual,
        MonthlyBudget,
        MonthlyVariance,
        CumulativeActualYTD,
        CumulativeBudgetYTD,
        CumulativeVarianceYTD
    FROM CumulativeData
    ORDER BY AccountPeriod ASC;
END;
GO

-- ============================================================================
-- 4. CRUD Stored Procedures for Users, Ships, and Assignments
-- ============================================================================

-- Create User
CREATE PROCEDURE dbo.sp_CreateUser
    @Username NVARCHAR(50),
    @FullName NVARCHAR(100),
    @Email NVARCHAR(100),
    @Role NVARCHAR(50),
    @PasswordHash NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
    BEGIN
        RAISERROR('Username already exists.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
    BEGIN
        RAISERROR('Email already exists.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Users (Username, FullName, Email, Role, PasswordHash)
    VALUES (@Username, @FullName, @Email, @Role, @PasswordHash);

    SELECT
        UserId, Username, FullName, Email, Role, IsActive, CreatedAt
    FROM dbo.Users
    WHERE UserId = SCOPE_IDENTITY();
END;
GO

-- Get All Users
CREATE PROCEDURE dbo.sp_GetUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Username, FullName, Email, Role, IsActive, CreatedAt
    FROM dbo.Users
    ORDER BY UserId ASC;
END;
GO

-- Get User By ID
CREATE PROCEDURE dbo.sp_GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Username, FullName, Email, Role, IsActive, CreatedAt
    FROM dbo.Users
    WHERE UserId = @UserId;
END;
GO

-- Get User By Username (for Auth)
CREATE PROCEDURE dbo.sp_GetUserByUsername
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Username, FullName, Email, Role, PasswordHash, IsActive, CreatedAt
    FROM dbo.Users
    WHERE Username = @Username;
END;
GO

-- Create Ship
CREATE PROCEDURE dbo.sp_CreateShip
    @ShipCode VARCHAR(20),
    @Name NVARCHAR(100),
    @FiscalYear VARCHAR(4),
    @Status VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Ships WHERE ShipCode = @ShipCode)
    BEGIN
        RAISERROR('ShipCode already exists.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Ships (ShipCode, Name, FiscalYear, Status)
    VALUES (@ShipCode, @Name, @FiscalYear, @Status);

    SELECT ShipCode, Name, FiscalYear, Status, CreatedAt, UpdatedAt
    FROM dbo.Ships
    WHERE ShipCode = @ShipCode;
END;
GO

-- Get All Ships (optionally filter by status)
CREATE PROCEDURE dbo.sp_GetShips
    @Status VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ShipCode, Name, FiscalYear, Status, CreatedAt, UpdatedAt
    FROM dbo.Ships
    WHERE @Status IS NULL OR Status = @Status
    ORDER BY ShipCode ASC;
END;
GO

-- Get Ship By Code
CREATE PROCEDURE dbo.sp_GetShipByCode
    @ShipCode VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ShipCode, Name, FiscalYear, Status, CreatedAt, UpdatedAt
    FROM dbo.Ships
    WHERE ShipCode = @ShipCode;
END;
GO

-- Assign Ship to User
CREATE PROCEDURE dbo.sp_AssignShipToUser
    @UserId INT,
    @ShipCode VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @UserId)
    BEGIN
        RAISERROR('User does not exist.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Ships WHERE ShipCode = @ShipCode)
    BEGIN
        RAISERROR('Ship does not exist.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.UserShipAssignments WHERE UserId = @UserId AND ShipCode = @ShipCode)
    BEGIN
        INSERT INTO dbo.UserShipAssignments (UserId, ShipCode)
        VALUES (@UserId, @ShipCode);
    END

    SELECT
        usa.UserId,
        u.Username,
        usa.ShipCode,
        s.Name AS ShipName,
        usa.AssignedAt
    FROM dbo.UserShipAssignments usa
    INNER JOIN dbo.Users u ON usa.UserId = u.UserId
    INNER JOIN dbo.Ships s ON usa.ShipCode = s.ShipCode
    WHERE usa.UserId = @UserId AND usa.ShipCode = @ShipCode;
END;
GO

-- Get Ships Assigned to User
CREATE PROCEDURE dbo.sp_GetShipsByUserId
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.ShipCode,
        s.Name,
        s.FiscalYear,
        s.Status,
        usa.AssignedAt
    FROM dbo.UserShipAssignments usa
    INNER JOIN dbo.Ships s ON usa.ShipCode = s.ShipCode
    WHERE usa.UserId = @UserId
    ORDER BY s.ShipCode ASC;
END;
GO

-- Get Users Assigned to Ship
CREATE PROCEDURE dbo.sp_GetUsersByShipCode
    @ShipCode VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.Username,
        u.FullName,
        u.Email,
        u.Role,
        usa.AssignedAt
    FROM dbo.UserShipAssignments usa
    INNER JOIN dbo.Users u ON usa.UserId = u.UserId
    WHERE usa.ShipCode = @ShipCode
    ORDER BY u.UserId ASC;
END;
GO
