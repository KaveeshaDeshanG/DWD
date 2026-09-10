-- =============================================================================
-- 01_CreateDatabase.sql
-- Community Sports Facilities Booking System — Phase 4: SQL Server Implementation
-- Ref: SPEC-016 (Database Design), SPEC-017 (Constraints and Business Rules)
-- =============================================================================

IF DB_ID(N'CommunitySportsBookingDB') IS NULL
BEGIN
    CREATE DATABASE CommunitySportsBookingDB;
END
GO

USE CommunitySportsBookingDB;
GO
