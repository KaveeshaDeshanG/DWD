-- =============================================================================
-- 03_Indexes.sql
-- Community Sports Facilities Booking System — Phase 4: SQL Server Implementation
-- Ref: SPEC-016 (Database Design) — indexes beyond what PK/UNIQUE already provide.
-- =============================================================================

USE CommunitySportsBookingDB;
GO

-- Hottest query path in the system: overlap/availability checks filter by
-- FacilityId + BookingDate (SPEC-008, SPEC-009).
CREATE NONCLUSTERED INDEX IX_Booking_FacilityId_BookingDate
    ON dbo.Booking (FacilityId, BookingDate);
GO

-- "My Bookings" (SPEC-010) filters by MemberId.
CREATE NONCLUSTERED INDEX IX_Booking_MemberId
    ON dbo.Booking (MemberId);
GO

-- Reverse lookups ("which members prefer sport X") — the composite PK's
-- leading column (MemberId) doesn't serve this query shape.
CREATE NONCLUSTERED INDEX IX_MemberSport_SportId
    ON dbo.MemberSport (SportId);
GO

-- Reverse lookup ("which facilities support sport X").
CREATE NONCLUSTERED INDEX IX_FacilitySport_SportId
    ON dbo.FacilitySport (SportId);
GO
