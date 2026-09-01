# Marking Scheme Checklist

Living document, tracked against the module's stated mark allocation. Update the Status column as phases complete — do not mark anything "Done" without evidence produced in-session (project constitution, Principle IV).

## Database Specification — 30 marks

| Item | Marks | Status | Evidence |
|---|---|---|---|
| ER Diagram | 20 | Guide ready, build pending | `docs/data-modeler-guide.md` (2026-08-30) specifies all 8 entities/attributes/relationships and the exact SDDM build steps; the diagram itself is a manual GUI task for the user |
| Data Dictionary | 5 | Approved (Phase 3) | `docs/data-dictionary.md` |
| SQL Developer Data Modeler representation | 5 | Guide ready, build pending | Manual: user builds logical + relational model in SDDM per `docs/data-modeler-guide.md`, RDBMS site = SQL Server, exports DDL preview + screenshots (6 required, listed in the guide §6) |

## Database Implementation — 20 marks

| Item | Marks | Status | Evidence |
|---|---|---|---|
| Models (CREATE TABLE + constraints) | 10 | Implemented & executed (Phase 4, 2026-08-30) | `database/02_CreateTables.sql`, `03_Indexes.sql`, `04_BookingOverlapProtection.sql` — run against a live SQL Server 2025 Developer Edition instance (`localhost`), all 8 tables + PK/FK/CHECK/UNIQUE constraints + indexes + stored procedure + trigger created without error |
| INSERT scripts | 5 | Implemented & executed (Phase 4, 2026-08-30) | `database/05_SeedData.sql` — run successfully; verified row counts: Member 5, Sport 6, MemberSport 8, Facility 6, FacilitySport 7, Booking 6, Review 2, Inquiry 3 |
| SELECT queries | 5 | Implemented & executed (Phase 4, 2026-08-30) | `database/06_TestQueries.sql` — all 10 queries run against real seeded data with real result sets captured; no `SELECT *` |

## Web Application — 42 marks

| Item | Status | Evidence |
|---|---|---|
| Home page | Implemented & verified (Phase 5, 2026-08-30) | `HomeController.Index` + `Views/Home/Index.cshtml`, reads real facility data via EF Core. Live-tested: `GET /` → 200 with correct nav and "5 active facilities" matching Phase 4 seed data exactly (`specs/001-mvc-foundation/tasks.md` T035) |
| Member sign-in | Implemented & verified (Phase 5, 2026-08-30) | `AccountController.Login`/`Logout`, custom `Member` table + `PasswordHasher<Member>` + cookie auth (no ASP.NET Core Identity). Live-tested: correct credentials authenticate + persist across requests; wrong password rejected generically with no cookie set; unauthenticated logout redirects (302). Minimal, functional UI only — full SPEC-003/SPEC-004 validation richness is Phase 6 (`tasks.md` T044/T048) |
| Member registration | Implemented & verified (Phase 6, 2026-08-30) | `AccountController.Register`, `Views/Account/Register.cshtml`. Live-tested: full field set + optional sports selection → signed in immediately, `sqlcmd` confirmed a real hashed row + matching MemberSport row; duplicate email rejected (both app pre-check and DB constraint proven separately); invalid fields rejected. Member profile view/edit and sports-preference management also implemented as part of this phase (`ProfileController`, `Views/Profile/Index.cshtml`) — see `specs/002-member-functionality/tasks.md` T011/T019/T025 |
| Member booking | Implemented & verified (Phase 7, re-verified 2026-09-02) | `BookingController.Create`, `Services/BookingService.cs` (calls the existing Phase 4 `usp_CreateBooking`). Live-tested: free window → booked, confirmed via `sqlcmd`; overlapping window from a second member → rejected with the distinct message; **and proven under genuine concurrency** — a `Task.WhenAll` test with two truly parallel booking attempts for the identical window passed on 3 consecutive full-suite runs. See `specs/003-facility-search-booking/tasks.md` T021-T023 |
| Member review submission | Pending | Separate, later feature — not part of Phase 7's approved scope (`specs/003-facility-search-booking/spec.md` Assumptions) |
| Member search | Implemented & verified (Phase 7, re-verified 2026-09-02) | `FacilityController.Index`/`.Details` (facility browsing, Guest+Member visible) + `.Search` (type/location for everyone, plus date/time with availability annotation for authenticated Members only — `IsMemberSearch` in `FacilitySearchViewModel`). Live-tested with a real antiforgery-token HTTP POST: search by type; search with date/time correctly excludes an overlapping window and includes a boundary-touching one |
| Guest registration | Pending | Phase 8 |
| Guest review search | Pending | Phase 8 |
| Guest restricted search | Implemented & verified (Phase 7, 2026-09-02) | `FacilityController.Search` (same action as Member search, SPEC-013) — a Guest gets Type/Location filtering only; Date/Time fields are not rendered and any crafted Date/Time input is stripped server-side before validation (SEC-013-01), never processed. Live-tested: unauthenticated `GET /Facility/Search` → 200, form shows Type/Location only |
| Guest inquiry | Pending | Phase 8 |

## Testing — 10 marks

| Item | Status | Evidence |
|---|---|---|
| Test plan / test cases | Pending | Phase 10, SPEC-018 |
| Executed results | Pending | Only recorded once actually run |

## Reflection — 5 marks

| Item | Status | Evidence |
|---|---|---|
| Reflection/limitations/future work | Pending | Phase 11 documentation |

## Cross-cutting

| Item | Status | Notes |
|---|---|---|
| SpecKit specification structure (SPEC-001..019) | Approved (Phase 3, 2026-08-30) | `specs/` |
| Constitution | Approved (Phase 3, 2026-08-30) | `.specify/memory/constitution.md` |
| Traceability matrix | Approved (Phase 3, 2026-08-30) | `docs/traceability-matrix.md` |
| Phase 3 consistency check | Passed (2026-08-30) | Verified BR-01..14 numbering, table names, and traceability rows are consistent across all specs/docs; resolved operating-hours and auth-approach open items |
| Booking overlap protection (layered) | — | Verified (Phase 4, 2026-08-30) | Live-tested: `usp_CreateBooking` rejects an overlapping request and accepts a non-conflicting one; `trg_Booking_PreventOverlap` independently rejects a raw INSERT that bypasses the stored procedure; boundary-touching bookings (10:00-11:00 + 11:00-12:00) coexist as designed |
| Data Modeler build guide | Complete (2026-08-30) | `docs/data-modeler-guide.md` — cross-checked against SPEC-016, SPEC-017, data-dictionary.md, and 02_CreateTables.sql |
| Phase 5 (MVC Foundation) spec + plan + tasks | Implemented & verified (2026-08-30) | `specs/001-mvc-foundation/` — all 51 tasks (T001-T051) complete; ASP.NET Core MVC project (`src/CommunitySportsBooking.Web`), EF Core data layer over all 8 tables (no migrations, no `EnsureCreated()`), cookie auth + `PasswordHasher<Member>`, home page, minimal login/logout, Guest/Member nav distinction — all live-tested against the real running app and real database, not just built |
| Phase 6 (Member Functionality) spec + plan + tasks | Implemented & verified (2026-08-30) | `specs/002-member-functionality/` — all 29 tasks (T001-T029) complete; registration, profile view/edit, sports preferences all live-tested against the real running app and database; zero schema drift confirmed (still exactly 8 tables, 20 constraints, and the original Phase 4 row counts after full DB cleanup of manual test rows); zero changes to `Program.cs` or the EF Core mapping layer, as planned |
| Phase 7 (Facility Search and Booking) spec + plan + tasks | Implemented & verified (2026-08-30, re-verified after real fixes 2026-09-02) | `specs/003-facility-search-booking/` — all 32 tasks (T001-T032) plus a follow-on UI-polish pass (design system, images, error pages) existed uncommitted when this continuation session began, and `dotnet build` actually failed. Real bugs found and fixed this session (see `docs/traceability-matrix.md`'s 2026-09-02 note for full detail): a missing `IsMemberSearch` property (build break), a route-parameter-name bug in `HomeController.StatusCode` that silently zeroed every error page's HTTP status, a missing `HttpContext` in 10 direct-call `FacilityController.Search` tests, an unfinished Guest/Member split in `Search.cshtml` (SPEC-013), unwired design-system CSS on the shared facility card, and two dead `/Review`/`/Inquiry` links. After fixes: `dotnet build` 0 errors/0 warnings; `dotnet test` 32/32 passed across 3 consecutive runs; full HTTP sweep (browsing, Guest-restricted search, Member full search, booking, overlap rejection, ownership isolation, unauthenticated redirects, 404 pages) passed against the real running app; zero changes to `Program.cs`, `AppDbContext.cs`, or any EF Core configuration, as planned |
| Source control initialized | Done (2026-08-30) | Git baseline commit `0682ae6` ("Phase 6: Member functionality complete and verified"). Phase 7's work (now fixed and re-verified) is still not committed — a new commit is recommended once reviewed and approved |
