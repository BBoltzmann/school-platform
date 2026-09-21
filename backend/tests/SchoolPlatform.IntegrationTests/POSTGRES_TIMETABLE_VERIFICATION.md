# PostgreSQL timetable verification gate

Verified on 2026-09-21 from branch `feature/parallel-timetable-complete`, starting at
`07a4e7193bfa76663167ac2075453e76b85ded59`.

## Environment and isolation

`SCHOOL_AUTH_TEST_POSTGRES_CONNECTION` was visible and Npgsql connected to local
PostgreSQL at `127.0.0.1:5433`. Tests create randomly named
`school_timetable_test_<guid>` / `school_auth_test_<guid>` databases through the
local `postgres` maintenance database. Application data is seeded only into those
new databases; disposal drops them with `WITH (FORCE)`. Pooling is disabled.
The TCP fixtures reject other hosts/ports. Credentials are never written to this
report. The stale socket, Railway, and production PostgreSQL were not used.

## Executed results

| Verification | Result |
| --- | --- |
| Timetable PostgreSQL suite | 19 passed, 0 failed, 0 skipped |
| Existing PostgreSQL recovery suite | 3 passed, 0 failed, 0 skipped |
| SQLite integration suite | 42 passed, 0 failed; 22 PostgreSQL-only tests skipped |
| Unit suite | 10 passed, 0 failed |
| Solution restore | Passed |
| Solution build | Passed; 0 warnings, 0 errors |
| Whitespace validation | `git diff --check` passed |

The 19 executed timetable tests cover:

1. Existing rows and IDs preserved after each of the three feature migrations.
2. Two-member parallel generation.
3. Three-member parallel generation.
4. A parallel teacher whose available hours do not overlap teaching slots.
5. A teacher shared across classes, without slot collisions.
6. Zero-group legacy generation.
7. Slash-named legacy subject retained as one real subject.
8. Successful versioned regeneration with historical entries preserved.
9. Full-regeneration rollback after an injected entry-insert failure.
10. Class-regeneration rollback after an injected entry-insert failure.
11. Failed full/class planning preserves the active version and entries.
12. Reset preserves history and subsequent version numbering.
13. Class-specific regeneration preserves other classes.
14. Parallel-group class regeneration preserves other classes.
15. Concurrent full regenerations, using separate connections and a transaction-start barrier.
16. Concurrent full/class regeneration, using the same concurrency barrier.
17. The database rejects a second active version.
18. The database rejects duplicate parallel members and unpaired group/occurrence IDs.
19. Migrated index names match EF's model.

The migration fixture starts at `20260915182241_AddTeacherPortalInvitations`, seeds
existing academic, student, staffing and timetable rows, and applies, in order:

- `20260921120000_AddParallelSubjectGroupFoundation`
- `20260921150000_AddParallelTimetableOccurrence`
- `20260922100000_AddTimetableVersions`

It compares every existing column and ID in 16 fixture tables after each migration,
then verifies version-1 activation backfill and unchanged legacy timetable entries.

## Defects corrected

- TCP authentication-test configuration incorrectly selected SQLite and reused a
  local variable name. TCP now takes precedence, validates its local destination,
  and only drops a database it successfully created.
- The three feature migrations lacked EF discovery/target metadata. Added target
  model designers and regenerated the inconsistent model snapshot.
- A migration dropped an index using its unshortened name and passed a foreign-key
  column as the schema argument. Corrected both, plus remaining overlong identifiers.
- The parallel-occurrence unique index rejected valid multi-member occurrences.
  It now includes the subject key, while keeping teacher-slot uniqueness and
  checking that group and occurrence IDs are paired.
- Migrated group-member indexes and timetable class/subject indexes/foreign keys
  did not match the model. Aligned the migrations and model index names.
- Concurrent regeneration raced to activate versions. Generation/reset now use a
  PostgreSQL transaction advisory lock per tenant/session/term, including empty
  scopes. Class regeneration reads fixed entries after acquiring the lock.
- Deactivation is saved before inserting a new active version, within the same
  transaction. Each caller reads its own generated result before releasing the
  lock, preventing a later request from replacing the returned result.

## Reproduction

Export the local connection in the host shell, then run from `backend`:

```sh
../dotnet10 restore SchoolPlatform.sln
../dotnet10 test tests/SchoolPlatform.UnitTests/SchoolPlatform.UnitTests.csproj --no-restore --disable-build-servers -m:1
env -u SCHOOL_AUTH_TEST_POSTGRES_CONNECTION -u SCHOOL_AUTH_TEST_POSTGRES_SOCKET ../dotnet10 test tests/SchoolPlatform.IntegrationTests/SchoolPlatform.IntegrationTests.csproj --no-restore --disable-build-servers -m:1
env -u SCHOOL_AUTH_TEST_POSTGRES_SOCKET ../dotnet10 test tests/SchoolPlatform.IntegrationTests/SchoolPlatform.IntegrationTests.csproj --filter 'FullyQualifiedName~PostgresTimetableTests|FullyQualifiedName~PostgresRecoveryTests' --no-restore --disable-build-servers -m:1
../dotnet10 build SchoolPlatform.sln --no-restore --disable-build-servers -m:1
git diff --check
```

No frontend files changed. Stages 8–10 were not started.

**Gate verdict: READY FOR STAGES 8–10.**

## Changed files

Under `src/SchoolPlatform.Infrastructure`:

- `Persistence/Configurations/Timetabling/GeneratedTimetableConfiguration.cs`
- `Persistence/Configurations/Timetabling/GeneratedTimetableEntryConfiguration.cs`
- `Persistence/Migrations/20260921120000_AddParallelSubjectGroupFoundation.cs`
- `Persistence/Migrations/20260921120000_AddParallelSubjectGroupFoundation.Designer.cs`
- `Persistence/Migrations/20260921150000_AddParallelTimetableOccurrence.cs`
- `Persistence/Migrations/20260921150000_AddParallelTimetableOccurrence.Designer.cs`
- `Persistence/Migrations/20260922100000_AddTimetableVersions.Designer.cs`
- `Persistence/Migrations/SchoolPlatformDbContextModelSnapshot.cs`
- `Timetabling/TimetableGenerationService.cs`

Under `tests/SchoolPlatform.IntegrationTests`:

- `AuthenticationFactory.cs` (existing changes preserved and corrected)
- `PostgresRecoveryTests.cs` (existing changes preserved)
- `PostgresTimetableFixture.cs`
- `PostgresTimetableTests.cs`
- `POSTGRES_TIMETABLE_VERIFICATION.md`

## Git status

Staging failed with `.git/index.lock: Operation not permitted` in the sandbox.
No commit or push was performed. Generated build artifacts were restored/removed;
only the 14 intended files above remain changed. Run in the host shell:

```sh
cd /Users/cedriccampbell/dev/school-platform/backend
[ "$(git branch --show-current)" = feature/parallel-timetable-complete ] &&
git add -- \
  src/SchoolPlatform.Infrastructure/Persistence/Configurations/Timetabling \
  src/SchoolPlatform.Infrastructure/Persistence/Migrations \
  src/SchoolPlatform.Infrastructure/Timetabling/TimetableGenerationService.cs \
  tests/SchoolPlatform.IntegrationTests/*.cs \
  tests/SchoolPlatform.IntegrationTests/POSTGRES_TIMETABLE_VERIFICATION.md &&
git commit -m "Verify PostgreSQL timetable workflows" &&
git push origin feature/parallel-timetable-complete
```
