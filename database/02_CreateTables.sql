-- =============================================================================
-- 02_CreateTables.sql
-- Community Sports Facilities Booking System — Phase 4: SQL Server Implementation
-- Ref: SPEC-016 (Database Design), SPEC-017 (Constraints and Business Rules),
--      docs/data-dictionary.md (authoritative column list/types)
--
-- Re-runnable: drops the 8 tables (in dependency order) if they already exist,
-- then recreates them. Seed data (05_SeedData.sql) assumes IDENTITY columns
-- start fresh at 1, so run 01 -> 02 -> 03 -> 04 -> 05 -> 06 in order on a
-- database you are happy to reset.
-- =============================================================================

USE CommunitySportsBookingDB;
GO

IF OBJECT_ID(N'dbo.Review', N'U') IS NOT NULL DROP TABLE dbo.Review;
IF OBJECT_ID(N'dbo.Booking', N'U') IS NOT NULL DROP TABLE dbo.Booking;
IF OBJECT_ID(N'dbo.MemberSport', N'U') IS NOT NULL DROP TABLE dbo.MemberSport;
IF OBJECT_ID(N'dbo.FacilitySport', N'U') IS NOT NULL DROP TABLE dbo.FacilitySport;
IF OBJECT_ID(N'dbo.Inquiry', N'U') IS NOT NULL DROP TABLE dbo.Inquiry;
IF OBJECT_ID(N'dbo.Facility', N'U') IS NOT NULL DROP TABLE dbo.Facility;
IF OBJECT_ID(N'dbo.Sport', N'U') IS NOT NULL DROP TABLE dbo.Sport;
IF OBJECT_ID(N'dbo.Member', N'U') IS NOT NULL DROP TABLE dbo.Member;
GO

-- -----------------------------------------------------------------------------
-- 1. Member
-- -----------------------------------------------------------------------------
CREATE TABLE dbo.Member (
    MemberId        INT IDENTITY(1,1)  NOT NULL,
    FirstName       NVARCHAR(50)       NOT NULL,
    LastName        NVARCHAR(50)       NOT NULL,
    Email           NVARCHAR(256)      NOT NULL,
    Phone           NVARCHAR(20)       NOT NULL,
    AddressLine     NVARCHAR(200)      NOT NULL,
    City            NVARCHAR(100)      NOT NULL,
    PasswordHash    NVARCHAR(MAX)      NOT NULL,
    RegisteredDate  DATETIME2          NOT NULL CONSTRAINT DF_Member_RegisteredDate DEFAULT (SYSUTCDATETIME()),
    IsActive        BIT                NOT NULL CONSTRAINT DF_Member_IsActive DEFAULT (1),
    CONSTRAINT PK_Member PRIMARY KEY CLUSTERED (MemberId),
    CONSTRAINT UQ_Member_Email UNIQUE (Email)
);
GO

-- -----------------------------------------------------------------------------
-- 2. Sport (reference list)
-- -----------------------------------------------------------------------------
CREATE TABLE dbo.Sport (
    SportId     INT IDENTITY(1,1)  NOT NULL,
    SportName   NVARCHAR(50)       NOT NULL,
    CONSTRAINT PK_Sport PRIMARY KEY CLUSTERED (SportId),
    CONSTRAINT UQ_Sport_SportName UNIQUE (SportName)
);
GO

-- -----------------------------------------------------------------------------
-- 3. MemberSport (associative entity — Member's preferred sports, SPEC-005)
-- -----------------------------------------------------------------------------
CREATE TABLE dbo.MemberSport (
    MemberId    INT NOT NULL,
    SportId     INT NOT NULL,
    CONSTRAINT PK_MemberSport PRIMARY KEY CLUSTERED (MemberId, SportId),
    CONSTRAINT FK_MemberSport_Member FOREIGN KEY (MemberId) REFERENCES dbo.Member (MemberId) ON DELETE CASCADE,
    CONSTRAINT FK_MemberSport_Sport  FOREIGN KEY (SportId)  REFERENCES dbo.Sport (SportId)   ON DELETE NO ACTION
);
GO

-- -----------------------------------------------------------------------------
-- 4. Facility
-- -----------------------------------------------------------------------------
CREATE TABLE dbo.Facility (
    FacilityId      INT IDENTITY(1,1)  NOT NULL,
    FacilityName    NVARCHAR(100)      NOT NULL,
    FacilityType    NVARCHAR(50)       NOT NULL,
    Location        NVARCHAR(100)      NOT NULL,
    AddressLine     NVARCHAR(200)      NOT NULL,
    City            NVARCHAR(100)      NOT NULL,
    Capacity        INT                NULL,
    Description     NVARCHAR(1000)     NULL,
    IsActive        BIT                NOT NULL CONSTRAINT DF_Facility_IsActive DEFAULT (1),
    CONSTRAINT PK_Facility PRIMARY KEY CLUSTERED (FacilityId)
);
GO

-- -----------------------------------------------------------------------------
-- 5. FacilitySport (associative entity — sports a Facility supports)
-- -----------------------------------------------------------------------------
CREATE TABLE dbo.FacilitySport (
    FacilityId  INT NOT NULL,
    SportId     INT NOT NULL,
    CONSTRAINT PK_FacilitySport PRIMARY KEY CLUSTERED (FacilityId, SportId),
    CONSTRAINT FK_FacilitySport_Facility FOREIGN KEY (FacilityId) REFERENCES dbo.Facility (FacilityId) ON DELETE CASCADE,
    CONSTRAINT FK_FacilitySport_Sport    FOREIGN KEY (SportId)    REFERENCES dbo.Sport (SportId)       ON DELETE NO ACTION
);
GO

-- -----------------------------------------------------------------------------
-- 6. Booking
-- BR-02 (SPEC-017): StartTime < EndTime, enforced by CHECK.
-- BR-03: BookingDate not in the past — time-relative, application-enforced only.
-- BR-04/BR-06: overlap prevention — see 04_BookingOverlapProtection.sql.
-- -----------------------------------------------------------------------------
CREATE TABLE dbo.Booking (
    BookingId       INT IDENTITY(1,1)  NOT NULL,
    MemberId        INT                NOT NULL,
    FacilityId      INT                NOT NULL,
    BookingDate     DATE               NOT NULL,
    StartTime       TIME(0)            NOT NULL,
    EndTime         TIME(0)            NOT NULL,
    CreatedDate     DATETIME2          NOT NULL CONSTRAINT DF_Booking_CreatedDate DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Booking PRIMARY KEY CLUSTERED (BookingId),
    CONSTRAINT FK_Booking_Member   FOREIGN KEY (MemberId)   REFERENCES dbo.Member (MemberId)     ON DELETE NO ACTION,
    CONSTRAINT FK_Booking_Facility FOREIGN KEY (FacilityId) REFERENCES dbo.Facility (FacilityId) ON DELETE NO ACTION,
    CONSTRAINT CK_Booking_StartBeforeEnd CHECK (StartTime < EndTime)
);
GO

-- -----------------------------------------------------------------------------
-- 7. Review — BookingId is both PK and FK (SPEC-016 decision 1): enforces
-- "at most one review per booking" (BR-09) through the primary key itself.
-- No MemberId/FacilityId columns (SPEC-016 decision 2) — reached via Booking.
-- -----------------------------------------------------------------------------
CREATE TABLE dbo.Review (
    BookingId   INT             NOT NULL,
    Rating      TINYINT         NOT NULL,
    Comment     NVARCHAR(1000)  NOT NULL,
    ReviewDate  DATETIME2       NOT NULL CONSTRAINT DF_Review_ReviewDate DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Review PRIMARY KEY CLUSTERED (BookingId),
    CONSTRAINT FK_Review_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Booking (BookingId) ON DELETE CASCADE,
    CONSTRAINT CK_Review_Rating CHECK (Rating BETWEEN 1 AND 5)
);
GO

-- -----------------------------------------------------------------------------
-- 8. Inquiry
-- -----------------------------------------------------------------------------
CREATE TABLE dbo.Inquiry (
    InquiryId    INT IDENTITY(1,1)  NOT NULL,
    Name         NVARCHAR(100)      NOT NULL,
    Email        NVARCHAR(256)      NOT NULL,
    Subject      NVARCHAR(200)      NOT NULL,
    Message      NVARCHAR(2000)     NOT NULL,
    InquiryDate  DATETIME2          NOT NULL CONSTRAINT DF_Inquiry_InquiryDate DEFAULT (SYSUTCDATETIME()),
    Status       NVARCHAR(20)       NOT NULL CONSTRAINT DF_Inquiry_Status DEFAULT ('New'),
    CONSTRAINT PK_Inquiry PRIMARY KEY CLUSTERED (InquiryId),
    CONSTRAINT CK_Inquiry_Status CHECK (Status IN ('New','Reviewed','Closed'))
);
GO
