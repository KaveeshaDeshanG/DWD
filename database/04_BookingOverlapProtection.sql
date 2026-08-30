-- =============================================================================
-- 04_BookingOverlapProtection.sql
-- Community Sports Facilities Booking System — Phase 4: SQL Server Implementation
-- Ref: SPEC-009 (Facility Booking), SPEC-017 BR-04/BR-06 (overlap rule and its
-- three-layer enforcement).
--
-- Layer 1 (application pre-check) lives in the MVC service layer, built in a
-- later phase — not part of this database script.
-- Layer 2 (this script): dbo.usp_CreateBooking — sp_getapplock-guarded
-- transaction that closes the check-then-act race.
-- Layer 3 (this script): trg_Booking_PreventOverlap — AFTER INSERT, UPDATE
-- trigger as the authoritative backstop, independent of which code path wrote
-- the row.
--
-- BR-04 overlap definition: two windows on the same FacilityId/BookingDate
-- overlap iff NewStart < ExistingEnd AND NewEnd > ExistingStart. Touching
-- boundaries (NewStart == ExistingEnd or NewEnd == ExistingStart) are NOT
-- overlaps and are allowed.
-- =============================================================================

USE CommunitySportsBookingDB;
GO

-- -----------------------------------------------------------------------------
-- Layer 2: dbo.usp_CreateBooking
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_CreateBooking
    @MemberId     INT,
    @FacilityId   INT,
    @BookingDate  DATE,
    @StartTime    TIME(0),
    @EndTime      TIME(0),
    @NewBookingId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- BR-02: StartTime < EndTime (also enforced by CK_Booking_StartBeforeEnd,
    -- checked again here so the friendly RAISERROR fires before any lock/txn).
    IF @StartTime >= @EndTime
    BEGIN
        RAISERROR('StartTime must be before EndTime.', 16, 1);
        RETURN;
    END

    -- BR-03: BookingDate must not be in the past (time-relative, so not a
    -- static CHECK constraint — enforced here).
    IF @BookingDate < CAST(SYSUTCDATETIME() AS DATE)
    BEGIN
        RAISERROR('BookingDate cannot be in the past.', 16, 1);
        RETURN;
    END

    -- BR-01/BR-07: Member and Facility must exist and be active.
    IF NOT EXISTS (SELECT 1 FROM dbo.Facility WHERE FacilityId = @FacilityId AND IsActive = 1)
    BEGIN
        RAISERROR('Facility does not exist or is not active.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Member WHERE MemberId = @MemberId AND IsActive = 1)
    BEGIN
        RAISERROR('Member does not exist or is not active.', 16, 1);
        RETURN;
    END

    DECLARE @LockResource NVARCHAR(200) =
        N'Booking_Facility_' + CAST(@FacilityId AS NVARCHAR(10)) + N'_' + CONVERT(NVARCHAR(10), @BookingDate, 112);
    DECLARE @LockResult INT;

    BEGIN TRANSACTION;

    -- Exclusive application lock keyed on (FacilityId, BookingDate) — closes
    -- the check-then-insert race between concurrent booking attempts for the
    -- same facility/date. @LockOwner = 'Transaction' auto-releases on
    -- COMMIT/ROLLBACK, no explicit sp_releaseapplock needed.
    EXEC @LockResult = sp_getapplock
        @Resource    = @LockResource,
        @LockMode    = 'Exclusive',
        @LockOwner   = 'Transaction',
        @LockTimeout = 5000;

    IF @LockResult < 0
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('Could not acquire the booking lock for this facility/date; please try again.', 16, 1);
        RETURN;
    END

    -- Re-check availability inside the lock — BR-04 overlap test.
    IF EXISTS (
        SELECT 1
        FROM dbo.Booking b
        WHERE b.FacilityId = @FacilityId
          AND b.BookingDate = @BookingDate
          AND @StartTime < b.EndTime
          AND @EndTime   > b.StartTime
    )
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('The facility is not available for the requested time window.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Booking (MemberId, FacilityId, BookingDate, StartTime, EndTime)
    VALUES (@MemberId, @FacilityId, @BookingDate, @StartTime, @EndTime);

    SET @NewBookingId = SCOPE_IDENTITY();

    COMMIT TRANSACTION;
END
GO

-- -----------------------------------------------------------------------------
-- Layer 3: trg_Booking_PreventOverlap
-- Authoritative backstop — fires regardless of which code path wrote the row
-- (the stored procedure above, a future EF Core path, or a direct script).
-- -----------------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.trg_Booking_PreventOverlap
ON dbo.Booking
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Booking b
          ON b.FacilityId  = i.FacilityId
         AND b.BookingDate = i.BookingDate
         AND b.BookingId  <> i.BookingId
         AND i.StartTime   < b.EndTime
         AND i.EndTime     > b.StartTime
    )
    BEGIN
        RAISERROR('Booking overlaps an existing booking for this facility and date.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END
GO
