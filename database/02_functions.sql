-- ============================================================================
-- Anglo-Eastern Backend Code Challenge
-- Script 02: Scalar and Table-Valued Functions (Logic Reuse)
-- Target: Microsoft SQL Server 2019+ / Azure SQL Database
-- ============================================================================

USE ShipManagementDb;
GO

-- Drop functions if re-running
IF OBJECT_ID('dbo.fn_CalculateAge', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_CalculateAge;
IF OBJECT_ID('dbo.fn_GetCrewStatus', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_GetCrewStatus;
IF OBJECT_ID('dbo.fn_GetFiscalYearStartPeriod', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_GetFiscalYearStartPeriod;
IF OBJECT_ID('dbo.fn_GetFiscalYearPeriods', 'IF') IS NOT NULL DROP FUNCTION dbo.fn_GetFiscalYearPeriods;
GO

-- ============================================================================
-- 1. fn_CalculateAge: Returns exact age given BirthDate and AsOfDate
-- ============================================================================
CREATE FUNCTION dbo.fn_CalculateAge (
    @BirthDate DATE,
    @AsOfDate DATE
)
RETURNS INT
AS
BEGIN
    IF @BirthDate IS NULL OR @AsOfDate IS NULL
        RETURN NULL;

    RETURN DATEDIFF(year, @BirthDate, @AsOfDate) -
        CASE
            WHEN (MONTH(@BirthDate) > MONTH(@AsOfDate))
                 OR (MONTH(@BirthDate) = MONTH(@AsOfDate) AND DAY(@BirthDate) > DAY(@AsOfDate))
            THEN 1
            ELSE 0
        END;
END;
GO

-- ============================================================================
-- 2. fn_GetCrewStatus: Returns crew status string relative to AsOfDate
-- ============================================================================
CREATE FUNCTION dbo.fn_GetCrewStatus (
    @SignOnDate DATE,
    @SignOffDate DATE,
    @EndOfContractDate DATE,
    @AsOfDate DATE
)
RETURNS VARCHAR(20)
AS
BEGIN
    IF @SignOffDate IS NOT NULL
        RETURN 'Signed Off';

    IF @SignOnDate > @AsOfDate
        RETURN 'Planned';

    IF DATEDIFF(day, @EndOfContractDate, @AsOfDate) > 30
        RETURN 'Relief Due';

    RETURN 'Onboard';
END;
GO

-- ============================================================================
-- 3. fn_GetFiscalYearStartPeriod: Returns start 'YYYY-MM' period for target period
-- ============================================================================
CREATE FUNCTION dbo.fn_GetFiscalYearStartPeriod (
    @FiscalYearCode VARCHAR(4),
    @TargetPeriod VARCHAR(7)
)
RETURNS VARCHAR(7)
AS
BEGIN
    IF @FiscalYearCode IS NULL OR @TargetPeriod IS NULL
        RETURN NULL;

    DECLARE @TargetYear INT = CAST(SUBSTRING(@TargetPeriod, 1, 4) AS INT);
    DECLARE @TargetMonth INT = CAST(SUBSTRING(@TargetPeriod, 6, 2) AS INT);
    DECLARE @StartMonth INT = CAST(SUBSTRING(@FiscalYearCode, 1, 2) AS INT);
    DECLARE @StartYear INT;

    IF @StartMonth = 1
    BEGIN
        SET @StartYear = @TargetYear;
    END
    ELSE
    BEGIN
        IF @TargetMonth >= @StartMonth
            SET @StartYear = @TargetYear;
        ELSE
            SET @StartYear = @TargetYear - 1;
    END

    RETURN FORMAT(@StartYear, '0000') + '-' + RIGHT('0' + CAST(@StartMonth AS VARCHAR(2)), 2);
END;
GO

-- ============================================================================
-- 4. fn_GetFiscalYearPeriods: Inline Table-Valued Function returning all YTD periods
-- ============================================================================
CREATE FUNCTION dbo.fn_GetFiscalYearPeriods (
    @FiscalYearCode VARCHAR(4),
    @TargetPeriod VARCHAR(7)
)
RETURNS TABLE
AS
RETURN (
    WITH MonthsCTE AS (
        SELECT
            dbo.fn_GetFiscalYearStartPeriod(@FiscalYearCode, @TargetPeriod) AS StartPeriod,
            @TargetPeriod AS TargetPeriod
    ),
    NumbersCTE AS (
        SELECT 0 AS Num
        UNION ALL SELECT 1
        UNION ALL SELECT 2
        UNION ALL SELECT 3
        UNION ALL SELECT 4
        UNION ALL SELECT 5
        UNION ALL SELECT 6
        UNION ALL SELECT 7
        UNION ALL SELECT 8
        UNION ALL SELECT 9
        UNION ALL SELECT 10
        UNION ALL SELECT 11
    ),
    PeriodsCTE AS (
        SELECT
            FORMAT(
                DATEADD(month, n.Num, CAST(m.StartPeriod + '-01' AS DATE)),
                'yyyy-MM'
            ) AS AccountPeriod
        FROM MonthsCTE m
        CROSS JOIN NumbersCTE n
        WHERE DATEADD(month, n.Num, CAST(m.StartPeriod + '-01' AS DATE)) <= CAST(m.TargetPeriod + '-01' AS DATE)
    )
    SELECT AccountPeriod FROM PeriodsCTE
);
GO
