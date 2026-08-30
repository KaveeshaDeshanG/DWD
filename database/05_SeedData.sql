-- =============================================================================
-- 05_SeedData.sql
-- Community Sports Facilities Booking System — Phase 4: SQL Server Implementation
--
-- Assumes 01 -> 02 -> 03 -> 04 have just been run against a fresh database, so
-- IDENTITY columns start at 1 and the ID literals below (e.g. FacilityId = 1
-- for the first facility inserted) line up. Do not run against a database
-- that already has data unless you intend to reset it via 02_CreateTables.sql
-- first.
--
-- IMPORTANT — PasswordHash values below are placeholders (the literal text
-- 'PLACEHOLDER_HASH_REPLACE_IN_PHASE5'), not real ASP.NET Core
-- PasswordHasher<Member> output. Real hashes can only be produced by running
-- the .NET PasswordHasher API, which happens in Phase 5/6 (MVC Foundation /
-- Member Functionality). Do not attempt to sign in with these seed members
-- until the application replaces these placeholders with real hashes.
-- =============================================================================

USE CommunitySportsBookingDB;
GO

-- -----------------------------------------------------------------------------
-- Sport
-- -----------------------------------------------------------------------------
INSERT INTO dbo.Sport (SportName) VALUES
    (N'Tennis'),        -- 1
    (N'Basketball'),    -- 2
    (N'Swimming'),      -- 3
    (N'Badminton'),     -- 4
    (N'Football'),      -- 5
    (N'Athletics');     -- 6
GO

-- -----------------------------------------------------------------------------
-- Facility
-- -----------------------------------------------------------------------------
INSERT INTO dbo.Facility (FacilityName, FacilityType, Location, AddressLine, City, Capacity, Description, IsActive) VALUES
    (N'Riverside Tennis Courts',  N'Tennis Court',      N'Riverside',  N'12 High Street',    N'Springfield', 4,   N'Two outdoor hard courts by the river.', 1),   -- 1
    (N'Central Community Pool',   N'Swimming Pool',     N'City Centre', N'1 Pool Road',       N'Springfield', 50,  N'25m indoor pool with lane swimming sessions.', 1), -- 2
    (N'Oakwood Sports Hall',      N'Sports Hall',       N'Oakwood',    N'5 Oak Avenue',       N'Springfield', 30,  N'Multi-use hall for basketball and badminton.', 1), -- 3
    (N'Westside Football Pitch',  N'Football Pitch',    N'Westside',   N'20 West Road',       N'Springfield', 22,  N'Full-size grass pitch with floodlights.', 1),  -- 4
    (N'Northgate Athletics Track', N'Athletics Track',  N'Northgate',  N'8 North Boulevard',  N'Springfield', 100, N'8-lane outdoor athletics track.', 1),          -- 5
    (N'Old Mill Badminton Courts', N'Badminton Court',  N'Mill End',   N'3 Mill Lane',        N'Springfield', 8,   N'Currently closed for refurbishment.', 0);      -- 6 (inactive)
GO

-- -----------------------------------------------------------------------------
-- FacilitySport
-- -----------------------------------------------------------------------------
INSERT INTO dbo.FacilitySport (FacilityId, SportId) VALUES
    (1, 1),  -- Riverside Tennis Courts - Tennis
    (2, 3),  -- Central Community Pool - Swimming
    (3, 2),  -- Oakwood Sports Hall - Basketball
    (3, 4),  -- Oakwood Sports Hall - Badminton
    (4, 5),  -- Westside Football Pitch - Football
    (5, 6),  -- Northgate Athletics Track - Athletics
    (6, 4);  -- Old Mill Badminton Courts - Badminton (inactive facility)
GO

-- -----------------------------------------------------------------------------
-- Member
-- -----------------------------------------------------------------------------
INSERT INTO dbo.Member (FirstName, LastName, Email, Phone, AddressLine, City, PasswordHash, IsActive) VALUES
    (N'Alice',  N'Johnson', N'alice.johnson@example.com', N'07700 900001', N'10 Elm Street',   N'Springfield', N'PLACEHOLDER_HASH_REPLACE_IN_PHASE5', 1),  -- 1
    (N'Ben',    N'Carter',  N'ben.carter@example.com',    N'07700 900002', N'22 Birch Road',   N'Springfield', N'PLACEHOLDER_HASH_REPLACE_IN_PHASE5', 1),  -- 2
    (N'Chloe',  N'Davies',  N'chloe.davies@example.com',  N'07700 900003', N'5 Cedar Close',   N'Springfield', N'PLACEHOLDER_HASH_REPLACE_IN_PHASE5', 1),  -- 3
    (N'Daniel', N'Evans',   N'daniel.evans@example.com',  N'07700 900004', N'18 Pine Avenue',  N'Springfield', N'PLACEHOLDER_HASH_REPLACE_IN_PHASE5', 1),  -- 4
    (N'Emma',   N'Foster',  N'emma.foster@example.com',   N'07700 900005', N'9 Maple Grove',   N'Springfield', N'PLACEHOLDER_HASH_REPLACE_IN_PHASE5', 1);  -- 5
GO

-- -----------------------------------------------------------------------------
-- MemberSport
-- -----------------------------------------------------------------------------
INSERT INTO dbo.MemberSport (MemberId, SportId) VALUES
    (1, 1),  -- Alice - Tennis
    (1, 3),  -- Alice - Swimming
    (2, 3),  -- Ben - Swimming
    (3, 2),  -- Chloe - Basketball
    (3, 4),  -- Chloe - Badminton
    (4, 5),  -- Daniel - Football
    (5, 6),  -- Emma - Athletics
    (5, 1);  -- Emma - Tennis
GO

-- -----------------------------------------------------------------------------
-- Booking
--
-- Historical (already-occurred) bookings are inserted directly rather than
-- via usp_CreateBooking: BR-03 ("BookingDate not in the past") is an
-- application-submission-time rule — it correctly blocks a *new* booking
-- request for a past date (proven below, see execution note), but does not
-- apply to seeding pre-existing historical records that already happened.
-- The overlap trigger (Layer 3) still fires on these INSERTs regardless.
-- -----------------------------------------------------------------------------
INSERT INTO dbo.Booking (MemberId, FacilityId, BookingDate, StartTime, EndTime) VALUES
    (1, 1, '2026-08-10', '09:00', '10:00'),  -- 1 (past, completed)
    (2, 2, '2026-08-15', '14:00', '15:00');  -- 2 (past, completed)
GO

-- Upcoming bookings go through dbo.usp_CreateBooking so the seed data
-- exercises the same overlap-protected path the application will use.
--
-- Booking 3 and Booking 6 are deliberately boundary-touching on the same
-- facility/date (10:00-11:00 then 11:00-12:00) to seed a concrete, real
-- example of BR-04's "touching is allowed" rule for later demonstration
-- queries and screenshots.
DECLARE @NewId INT;

EXEC dbo.usp_CreateBooking @MemberId = 3, @FacilityId = 1, @BookingDate = '2026-09-10', @StartTime = '10:00', @EndTime = '11:00', @NewBookingId = @NewId OUTPUT; -- 3 (future, overlap-demo anchor)
EXEC dbo.usp_CreateBooking @MemberId = 1, @FacilityId = 3, @BookingDate = '2026-09-12', @StartTime = '18:00', @EndTime = '19:00', @NewBookingId = @NewId OUTPUT; -- 4 (future)
EXEC dbo.usp_CreateBooking @MemberId = 4, @FacilityId = 4, @BookingDate = '2026-09-14', @StartTime = '16:00', @EndTime = '17:30', @NewBookingId = @NewId OUTPUT; -- 5 (future)
EXEC dbo.usp_CreateBooking @MemberId = 5, @FacilityId = 1, @BookingDate = '2026-09-10', @StartTime = '11:00', @EndTime = '12:00', @NewBookingId = @NewId OUTPUT; -- 6 (future, boundary-touching with 3)
GO

-- -----------------------------------------------------------------------------
-- Review (only for the two past/completed bookings: BookingId 1 and 2)
-- -----------------------------------------------------------------------------
INSERT INTO dbo.Review (BookingId, Rating, Comment) VALUES
    (1, 5, N'Excellent courts, well maintained and the surface was in great condition.'),
    (2, 4, N'Clean facility, but quite crowded during the afternoon session.');
GO

-- -----------------------------------------------------------------------------
-- Inquiry
-- -----------------------------------------------------------------------------
INSERT INTO dbo.Inquiry (Name, Email, Subject, Message, Status) VALUES
    (N'Grace Lee',  N'grace.lee@example.com',  N'Membership fees',       N'Could you tell me the annual membership fee for community members?', N'New'),
    (N'Henry Wu',   N'henry.wu@example.com',   N'Facility hire for event', N'Is it possible to hire the Sports Hall for a private event next month?', N'Reviewed'),
    (N'Isla Brown', N'isla.brown@example.com', N'Accessibility',         N'Are the changing rooms at the Community Pool wheelchair accessible?', N'Closed');
GO
