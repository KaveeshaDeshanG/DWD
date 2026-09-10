-- =============================================================================
-- 06_TestQueries.sql
-- Community Sports Facilities Booking System — Phase 4: SQL Server Implementation
-- Demonstration SELECT queries, ref: coursework brief Section 5 required
-- query categories. None of these are bare SELECT * — each demonstrates a
-- meaningful join, filter, or aggregate.
-- =============================================================================

USE CommunitySportsBookingDB;
GO

-- -----------------------------------------------------------------------------
-- Q1. List active facilities (SPEC-006)
-- -----------------------------------------------------------------------------
SELECT FacilityId, FacilityName, FacilityType, Location, City, Capacity
FROM dbo.Facility
WHERE IsActive = 1
ORDER BY FacilityName;
GO

-- -----------------------------------------------------------------------------
-- Q2. Search facilities by type and location (SPEC-007/SPEC-013)
-- -----------------------------------------------------------------------------
SELECT FacilityId, FacilityName, FacilityType, Location, City
FROM dbo.Facility
WHERE IsActive = 1
  AND FacilityType LIKE '%Tennis%'
  AND Location LIKE '%Riverside%';
GO

-- -----------------------------------------------------------------------------
-- Q3. Search availability by date/time for a specific facility (SPEC-008,
-- BR-04) — demonstrates the overlap rule directly against seeded data:
-- Facility 1 has a booking 10:00-11:00 on 2026-09-10. A request for
-- 10:30-11:30 is shown as UNAVAILABLE (true overlap); a request for
-- 09:00-10:00 is shown as AVAILABLE (boundary-touching, allowed).
-- -----------------------------------------------------------------------------
DECLARE @CheckFacilityId INT = 1, @CheckDate DATE = '2026-09-10';

SELECT
    CASE WHEN EXISTS (
        SELECT 1 FROM dbo.Booking b
        WHERE b.FacilityId = @CheckFacilityId AND b.BookingDate = @CheckDate
          AND '10:30' < b.EndTime AND '11:30' > b.StartTime
    ) THEN 'UNAVAILABLE' ELSE 'AVAILABLE' END AS Window_1030_1130,
    CASE WHEN EXISTS (
        SELECT 1 FROM dbo.Booking b
        WHERE b.FacilityId = @CheckFacilityId AND b.BookingDate = @CheckDate
          AND '09:00' < b.EndTime AND '10:00' > b.StartTime
    ) THEN 'UNAVAILABLE' ELSE 'AVAILABLE' END AS Window_0900_1000;
GO

-- -----------------------------------------------------------------------------
-- Q4. List a member's own bookings (SPEC-010), joined to Facility for display
-- -----------------------------------------------------------------------------
SELECT
    bk.BookingId, f.FacilityName, bk.BookingDate, bk.StartTime, bk.EndTime,
    CASE WHEN DATEADD(SECOND, DATEDIFF(SECOND, '00:00:00', bk.EndTime), CAST(bk.BookingDate AS DATETIME2)) < SYSUTCDATETIME()
         THEN 'Completed' ELSE 'Upcoming' END AS BookingState
FROM dbo.Booking bk
JOIN dbo.Facility f ON f.FacilityId = bk.FacilityId
WHERE bk.MemberId = 1
ORDER BY bk.BookingDate, bk.StartTime;
GO

-- -----------------------------------------------------------------------------
-- Q5. List all bookings for a given facility (facility-side booking sheet)
-- -----------------------------------------------------------------------------
SELECT
    bk.BookingId, m.FirstName + ' ' + m.LastName AS MemberName,
    bk.BookingDate, bk.StartTime, bk.EndTime
FROM dbo.Booking bk
JOIN dbo.Member m ON m.MemberId = bk.MemberId
WHERE bk.FacilityId = 1
ORDER BY bk.BookingDate, bk.StartTime;
GO

-- -----------------------------------------------------------------------------
-- Q6. Display facility reviews (SPEC-012) — Review joined through Booking to
-- reach Member/Facility, since Review stores neither directly (SPEC-016).
-- -----------------------------------------------------------------------------
SELECT
    f.FacilityName,
    m.FirstName + ' ' + LEFT(m.LastName, 1) + '.' AS ReviewedBy,
    r.Rating, r.Comment, r.ReviewDate
FROM dbo.Review r
JOIN dbo.Booking bk ON bk.BookingId = r.BookingId
JOIN dbo.Facility f ON f.FacilityId = bk.FacilityId
JOIN dbo.Member m   ON m.MemberId   = bk.MemberId
ORDER BY r.ReviewDate DESC;
GO

-- -----------------------------------------------------------------------------
-- Q7. Calculate average facility rating (SPEC-012 BR-012-01) — computed live,
-- never stored/denormalized. LEFT JOIN so a facility with zero reviews still
-- appears, with a NULL average rather than being silently omitted.
-- -----------------------------------------------------------------------------
SELECT
    f.FacilityId, f.FacilityName,
    COUNT(r.BookingId) AS ReviewCount,
    CAST(ROUND(AVG(CAST(r.Rating AS DECIMAL(3,1))), 1) AS DECIMAL(3,1)) AS AverageRating
FROM dbo.Facility f
LEFT JOIN dbo.Booking bk ON bk.FacilityId = f.FacilityId
LEFT JOIN dbo.Review r   ON r.BookingId   = bk.BookingId
WHERE f.IsActive = 1
GROUP BY f.FacilityId, f.FacilityName
ORDER BY AverageRating DESC;
GO

-- -----------------------------------------------------------------------------
-- Q8. Display a member's preferred sports (SPEC-005)
-- -----------------------------------------------------------------------------
SELECT m.FirstName, m.LastName, s.SportName
FROM dbo.MemberSport ms
JOIN dbo.Member m ON m.MemberId = ms.MemberId
JOIN dbo.Sport s  ON s.SportId  = ms.SportId
WHERE m.MemberId = 5
ORDER BY s.SportName;
GO

-- -----------------------------------------------------------------------------
-- Q9. Full booking information via JOINs across Member, Facility, and Review
-- existence (LEFT JOIN so an unreviewed booking still shows, with NULLs)
-- -----------------------------------------------------------------------------
SELECT
    bk.BookingId,
    m.FirstName + ' ' + m.LastName AS MemberName,
    f.FacilityName, f.FacilityType,
    bk.BookingDate, bk.StartTime, bk.EndTime,
    r.Rating AS ReviewRating
FROM dbo.Booking bk
JOIN dbo.Member m   ON m.MemberId   = bk.MemberId
JOIN dbo.Facility f ON f.FacilityId = bk.FacilityId
LEFT JOIN dbo.Review r ON r.BookingId = bk.BookingId
ORDER BY bk.BookingDate, bk.StartTime;
GO

-- -----------------------------------------------------------------------------
-- Q10. Identify all active facilities available for a given date/time window
-- (SPEC-007/SPEC-008) — "available" = active AND no overlapping booking.
-- -----------------------------------------------------------------------------
DECLARE @SearchDate DATE = '2026-09-10', @SearchStart TIME = '10:30', @SearchEnd TIME = '11:30';

SELECT f.FacilityId, f.FacilityName, f.FacilityType, f.Location
FROM dbo.Facility f
WHERE f.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Booking b
      WHERE b.FacilityId = f.FacilityId
        AND b.BookingDate = @SearchDate
        AND @SearchStart < b.EndTime
        AND @SearchEnd   > b.StartTime
  )
ORDER BY f.FacilityName;
GO
