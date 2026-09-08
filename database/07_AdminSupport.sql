-- =============================================================================
-- 07_AdminSupport.sql
-- Community Sports Facilities Booking System — Admin Panel, Phase 1
-- Ref: Admin Panel architecture decision (user-approved 2026-09-08).
--
-- Purpose: adds the minimum schema needed to identify an administrator.
-- Booking lifecycle (Upcoming/Completed) stays fully derived, exactly as
-- designed in Phase 3 — no BookingStatus column is added here or anywhere.
--
-- Additive only:
--   - Adds ONE new column to the existing dbo.Member table (mirrors the
--     existing IsActive bit column exactly). No table is dropped or
--     recreated; no other table, column, or row is touched.
--   - Promotes exactly one existing member (MemberId 104) to administrator.
--
-- Safe to re-run: the ALTER is guarded by an existence check, and the UPDATE
-- is idempotent (setting IsAdmin = 1 a second time has no further effect).
-- Run this after 01-06 have already been applied.
-- =============================================================================

USE CommunitySportsBookingDB;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Member') AND name = 'IsAdmin'
)
BEGIN
    ALTER TABLE dbo.Member ADD IsAdmin BIT NOT NULL CONSTRAINT DF_Member_IsAdmin DEFAULT (0);
END
GO

-- Initial administrator: MemberId 104 (Kaveesha Deshan) — the project
-- owner's own real, already-registered account, chosen explicitly over
-- adding a separate dedicated seed admin member.
UPDATE dbo.Member SET IsAdmin = 1 WHERE MemberId = 104;
GO
