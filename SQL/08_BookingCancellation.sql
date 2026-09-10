-- =============================================================================
-- 08_BookingCancellation.sql
-- Community Sports Facilities Booking System — Booking lifecycle change.
--
-- Purpose: replaces hard-delete cancellation with a persisted, soft-cancel
-- flag so Admin can see cancelled booking history and the original booking
-- record (Member, Facility, Sport via Facility, date/time) is preserved.
--
-- Additive only: adds two nullable/defaulted columns to the existing
-- dbo.Booking table; does not drop or recreate it, does not touch any other
-- table. "Completed" is still never stored — it stays derived from
-- BookingDate/EndTime at read time (BookingService.IsCompleted); only
-- "cancelled or not" becomes persistent.
--
-- Also updates the two existing overlap-protection objects
-- (dbo.usp_CreateBooking, trg_Booking_PreventOverlap) so a cancelled
-- booking is excluded from every overlap/conflict check — a cancelled slot
-- must never block a new booking for the same facility/date/time.
--
-- Safe to re-run: the ALTERs are guarded by existence checks; the
-- CREATE OR ALTER statements are idempotent by definition.
-- Run this after 01-07 have already been applied.
-- =============================================================================

USE CommunitySportsBookingDB;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Booking') AND name = 'IsCancelled'
)
BEGIN
    ALTER TABLE dbo.Booking ADD IsCancelled BIT NOT NULL CONSTRAINT DF_Booking_IsCancelled DEFAULT (0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Booking') AND name = 'CancelledDate'
)
BEGIN
    ALTER TABLE dbo.Booking ADD CancelledDate DATETIME2 NULL;
END
GO

-- -----------------------------------------------------------------------------
-- Layer 3 (trigger backstop) — same overlap definition as before, now with
-- "AND both rows are not cancelled" added to both sides of the self-join.
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
         AND b.IsCancelled = 0
         AND i.IsCancelled = 0
         AND i.StartTime   < b.EndTime
         AND i.EndTime     > b.StartTime
    )
    BEGIN
        RAISERROR('Booking overlaps an existing booking for this facility and date.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END
GO

-- -----------------------------------------------------------------------------
-- Layer 2 (sp_getapplock re-check inside usp_CreateBooking) — same addition,
-- everything else about the procedure (validation order, locking, RAISERROR
-- messages) is unchanged.
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

    IF @StartTime >= @EndTime
    BEGIN
        RAISERROR('StartTime must be before EndTime.', 16, 1);
        RETURN;
    END

    IF @BookingDate < CAST(SYSUTCDATETIME() AS DATE)
    BEGIN
        RAISERROR('BookingDate cannot be in the past.', 16, 1);
        RETURN;
    END

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

    IF EXISTS (
        SELECT 1
        FROM dbo.Booking b
        WHERE b.FacilityId = @FacilityId
          AND b.BookingDate = @BookingDate
          AND b.IsCancelled = 0
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
