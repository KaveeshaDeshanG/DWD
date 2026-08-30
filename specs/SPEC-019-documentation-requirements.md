# SPEC-019: Documentation Requirements

## Purpose
Map the coursework's required final-document structure (master brief §16) to the artifacts this project actually produces, so documentation stays synchronized with implementation rather than written from memory at the end.

## Scope
The structure and sourcing of the final submitted documentation. Does not itself contain the documentation content — that is written at Phase 11, pulling from the artifacts listed below.

## Actors
N/A (process specification).

## Required Document Structure → Source Mapping

| Doc Section (brief §16) | Sourced from |
|---|---|
| Introduction, Background, Problem Statement, Aim/Objectives, Scope | SPEC-001 |
| Requirements Analysis, Functional/Non-Functional Requirements | SPEC-001..SPEC-015 (functional), this project's non-functional requirements (to be drafted at Phase 5/6, realistic-for-prototype per constitution Principle V) |
| User Roles, Use Cases | SPEC-002 (roles/matrix), Main/Alternative/Exception flows across SPEC-003..SPEC-015 |
| Database Design, ER Diagram, Normalization, Data Dictionary | SPEC-016, `docs/data-dictionary.md` |
| SQL Developer Data Modeler | SPEC-016's Ambiguity Log manual task — user-captured screenshots, never fabricated |
| Database Implementation, CREATE TABLE, Constraints, INSERT, SELECT | Phase 4 deliverables (`database/*.sql`), SPEC-017 |
| ASP.NET MVC Architecture, Implementation | Phase 5–9 deliverables, project constitution Principle VI |
| Home Page, Member Features, Guest Features | SPEC-001, SPEC-003..SPEC-011 (member), SPEC-006/SPEC-012..SPEC-015 (guest) |
| Testing, Test Cases, Test Results | SPEC-018, Phase 10 execution evidence |
| Screenshots | Captured during Phase 9/10 from the actually-running application; SDDM screenshots are the one exception requiring the user's own tool access |
| Discussion, Reflection, Limitations, Future Improvements | Each spec's "Out of Scope" and "Ambiguity and Assumption Log" sections are the direct source material — they already record what was deliberately excluded and why |
| Conclusion, References, Appendices | Written at Phase 11; Appendices include full SQL scripts and the traceability matrix |

## Functional Requirements
- FR-019-01: Every implemented feature described in the final documentation shall trace to a SPEC-xxx ID via `docs/traceability-matrix.md`.
- FR-019-02: The documentation's "Limitations"/"Future Improvements" sections shall be drawn from the Out-of-Scope and Ambiguity logs already recorded across the specs, not invented fresh at write-up time.

## Business Rules
N/A.

## Validation Rules
N/A.

## Data Requirements
N/A.

## Security Requirements
N/A.

## Acceptance Criteria
- Given the final documentation is assembled, when any claimed feature is checked against `docs/traceability-matrix.md`, then a corresponding SPEC ID, DB component, MVC component, and test case shall exist.
- Given a claimed test result appears in the documentation, when checked against SPEC-018's evidence requirement, then it shall correspond to an actually-executed test, not a predicted one.

## Dependencies
All other specs; `docs/traceability-matrix.md`; `docs/marking-scheme-checklist.md`.

## Out of Scope
Writing the actual final document content (that is Phase 11 work, not part of this Phase 3 specification set).

## Ambiguity and Assumption Log
None.
