-- ============================================================================
-- Anglo-Eastern Backend Code Challenge
-- Master Deployment Script (Run in sqlcmd mode or via deployment runner)
-- ============================================================================

PRINT '>>> [1/4] Executing Schema DDL...';
:r 01_schema.sql

PRINT '>>> [2/4] Deploying Scalar and Table-Valued Functions...';
:r 02_functions.sql

PRINT '>>> [3/4] Deploying Stored Procedures...';
:r 03_stored_procedures.sql

PRINT '>>> [4/4] Inserting Realistic Sample Data...';
:r 04_sample_data.sql

PRINT '>>> Database deployment and seeding completed successfully.';
GO
