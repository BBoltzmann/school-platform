# Authentication recovery and school onboarding

## Production configuration

Set these in Railway's environment; do not put provider credentials in source files:

| Variable | Value / purpose |
| --- | --- |
| `PASSWORD_RESET_BASE_URL` | The canonical Vercel frontend **origin**, e.g. `https://your-school-platform.vercel.app`. No path, query or fragment. Links use `/reset-password?token=...`. |
| `EMAIL_PROVIDER` | `smtp` |
| `SMTP_HOST` | Your email provider's SMTP hostname. |
| `SMTP_PORT` | STARTTLS submission port, normally `587` (the default). Implicit TLS on port 465 is not supported by this transport. |
| `SMTP_FROM` | An authorized, verified sender address. |
| `SMTP_USERNAME` | Provider SMTP username, environment only. |
| `SMTP_PASSWORD` | Provider SMTP credential, environment only. |
| `SMTP_ENABLE_TLS` | `true` (default). Required outside Development. |
| `ALLOW_PUBLIC_SCHOOL_SIGNUP` | `false` by default; set `true` only to allow creation of new schools. |

The existing `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer`, and
`Jwt__Audience` configuration remains required. The API must run in Production on
Railway; the development bootstrap and initial-password routes are not mapped there.

Set these in Vercel's environment, using `frontend/web` as the project root:

| Variable | Value / purpose |
| --- | --- |
| `SCHOOL_PLATFORM_API_URL` | `https://school-platform-production-09fa.up.railway.app` |
| `ALLOW_PUBLIC_SCHOOL_SIGNUP` | Set to the same `true` / `false` value as Railway. It gates the page, link, and BFF; the backend independently enforces its own flag. |

Redeploy after changing environment variables. The API URL is server-only. No
`NEXT_PUBLIC_` prefix is used. An ignored local `.env.local` does not configure Vercel.

**Antioch:** the existing `antioch-college` tenant is preserved and reserved from
public signup. `admin@antiochcollege.local` is not an internet-deliverable address.
Recovery for that account requires a deliverable, administrator-approved mailbox
or an internal SMTP route for that address. This implementation does not alter
that production record or reset its password.

## Development email

Use a local SMTP inbox such as Mailpit, with `EMAIL_PROVIDER=smtp`,
`SMTP_HOST=localhost`, `SMTP_PORT=1025`, `SMTP_FROM=noreply@school.test`,
`SMTP_ENABLE_TLS=false`, and `PASSWORD_RESET_BASE_URL=http://localhost:3000`.
Run the API in Development. Read reset links in that inbox. Reset URLs are never
printed to the console, including in Development, to honor the no-token-logging
requirement. Production requires configured credentials and STARTTLS.

## Recovery behavior

1. `POST /api/auth/forgot-password` accepts `{email, tenantSlug}`. It normalizes and
   queues a durable request without looking up the account on the HTTP request
   path. The response is always the same accepted-request message for known and
   unknown identities: `If an account exists, a password reset link has been sent.`
   Malformed JSON, rate limits, and service outages can return non-success statuses;
   they do not depend on whether the account exists.
2. The hosted worker claims a database-backed delivery request with an atomic
   two-minute lease. It verifies active user, tenant, and membership, creates 32
   random bytes, and stores only the SHA-256 hash of the resulting token. Tokens
   expire 30 minutes after issuance. A one-minute per-user delivery cooldown
   limits repeated mail. Unknown, inactive, and wrong-tenant requests send nothing.
3. The raw token exists only in application memory and the reset email. Queue
   records contain email and tenant slug, never reset tokens. Email delivery
   failures invalidate their token and retain the request for retry. There are
   up to five attempts, two minutes apart; jobs older than 24 hours are discarded.
   Successful jobs are deleted. Delivery is at-least-once after a process crash,
   so rare duplicate emails are possible; only one reset can succeed.
4. `POST /api/auth/reset-password` accepts `{token, newPassword}`. A serializable
   database transaction validates the hash, expiration, active user and membership,
   marks all outstanding tokens for that global user used, and hashes the password
   with the existing `IPasswordHasher<User>`. Concurrent reset losers roll back.
5. Passwords must have 12–128 characters, an ASCII letter, and an ASCII digit;
   spaces and punctuation are allowed. Frontend and backend use the same rule.
6. Password changes rotate the user's security stamp. New JWTs carry that stamp;
   token validation checks it against the database and rejects older sessions.
   Existing users receive a nullable stamp in the migration; their current
   sessions remain compatible until a password change rotates the stamp.
7. The browser removes the reset token from the address bar after loading, does
   not persist it, uses a no-referrer/no-store reset page, clears any old login
   cookie on reset, and redirects to `/login?tenantSlug=...&reset=success`.

Provider failures are logged without exception payloads, credentials, email bodies,
raw tokens, passwords, or JWTs. Configure deployment/access-log systems to redact
reset query strings and authentication request bodies as well. Do not enable EF
sensitive-data logging or HTTP body logging for these routes.

## School creation and tenant isolation

`/create-school` posts to the same-origin `/api/auth/signup` BFF, then to Railway.
The backend returns 404 when public signup is disabled. It validates names,
password, normalized slug, and email; reserves `antioch-college`; and rejects
existing school slugs with 409. Database unique indexes handle concurrent requests.

Creation reuses `SchoolBootstrapService` to save the tenant, first campus, new user,
membership, Administrator role, membership role, and existing bootstrap permission
catalogue in one atomic save. The service retries a shared-permission insert race
once. It never joins or overwrites an existing tenant through public signup.

Emails remain globally unique, consistent with the current database and login
service. A public signup using an existing email creates nothing, changes no
password, and returns the same generic response as a new signup. This avoids
revealing global email registration. Existing identities must use an authenticated,
controlled invitation or administrator workflow; public signup is not an invitation
endpoint. No invitation-management feature is added here.

The login form defaults to `antioch-college` and permits another school slug.
After login it visits `/`, where the server redirects using the authenticated tenant
context. A reset password changes the global user's password across memberships;
it grants no new tenant memberships or permissions.

## Rate limits and login errors

ASP.NET Core's built-in rate limiter applies a 120-request/minute instance-wide
ceiling to public authentication routes. Login additionally allows 10 attempts per
normalized email/tenant pair per 15 minutes; forgot-password allows three. Limiter
keys are hashes, and account existence does not change limiting behavior. The
limiter does not trust arbitrary forwarded-IP headers, so it works behind the
Vercel BFF without treating that shared egress address as an individual user.
Limits are in-process: multiple Railway replicas multiply the ceiling. Use shared
edge/distributed limits if scaling beyond one instance.

The login BFF preserves 401 as invalid credentials and 429 as too many attempts.
Upstream routing/server/network failures return a sanitized service error (502),
and missing/invalid API configuration returns 503. It does not label every failure
as bad credentials. All login, recovery, session and protected API calls use the
same configured API origin. Cookies remain HttpOnly, SameSite=Lax, path `/`, and
Secure in production. Public BFF submissions reject cross-origin Origin headers.

## Migration and deployment

The additive EF migration `20260909194530_AddPasswordRecovery` creates
`password_recovery_jobs` and `password_reset_tokens`, their indexes and foreign keys,
and adds nullable `users.SecurityStamp`. It contains no tenant recreation, password
updates, or seed-data changes. Apply it **before** deploying the updated API because
session validation reads the new stamp column.

With .NET 10 and matching EF tools, run from the repository root against the
intended deployment database using environment-provided credentials:

```sh
dotnet ef database update \
  --project backend/src/SchoolPlatform.Infrastructure \
  --startup-project backend/src/SchoolPlatform.Api
```

Then deploy the backend with email configuration, deploy the frontend, and verify
recovery using a deliverable test mailbox. No production migration, email delivery,
password reset, commit, push, or deployment was performed during implementation.

## Validation

- Backend tests use isolated SQLite by default. The PostgreSQL concurrency tests
  explicitly skip unless an isolated test socket is supplied.
- With a temporary PostgreSQL instance owned by `school_auth_test`, listening only
  on a Unix socket under `/private/tmp/school-platform-auth-*`, the test suite can
  run with `SCHOOL_AUTH_TEST_POSTGRES_SOCKET=/private/tmp/school-platform-auth-...`.
  Each factory creates, migrates, and deletes only its randomly named test database.
- Verified on local PostgreSQL: 27 backend tests passed, including concurrent
  same-token and different-token reset attempts, old-session revocation, all
  required recovery/signup cases, and preservation of existing Antioch tenant,
  membership and password-hash data through the additive migration.
- Frontend: 11 tests passed with `npm test`; authentication-scoped ESLint; `npm run build -- --webpack`.
  Node 20.9+ is required. No dependency install was needed for the frontend.
- Default Turbopack encountered a local port-permission error even after escalation;
  the webpack production build passed. The default build script is unchanged.
- Full `npm run lint` found 24 pre-existing errors and one warning in unrelated
  administration components (primarily state updates in effects and unescaped
  apostrophes). Authentication-scoped lint passes; the full-repository lint gate is
  not green. These unrelated screens have not been modified or rules suppressed.

Implementation references: [ASP.NET Core rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
and [EF Core concurrency and isolation](https://learn.microsoft.com/en-us/ef/core/saving/concurrency).

## File-by-file changes

Paths are relative to the repository root. Generated build outputs were restored; the four pre-existing untracked sourcelink files were left in place.

| File | Change |
| --- | --- |
| `backend/src/SchoolPlatform.Api/Endpoints/AuthenticationRecoveryEndpoints.cs` | Adds generic forgot-password, token reset and gated signup endpoints. |
| `backend/src/SchoolPlatform.Api/Program.cs` | Registers recovery, SMTP, signup and rate limiting; validates JWT security stamps; preserves Development-only bootstrap routes. |
| `backend/src/SchoolPlatform.Api/Security/AuthAccountLimiter.cs` | Adds hashed per-account login/recovery quotas. |
| `backend/src/SchoolPlatform.Api/Services/PasswordRecoveryWorker.cs` | Processes durable recovery jobs with atomic leases, retries and sanitized failure logs. |
| `backend/src/SchoolPlatform.Api/Services/SmtpEmailSender.cs` | Sends reset emails through environment-configured SMTP; requires production credentials and TLS. |
| `backend/src/SchoolPlatform.Application/Authentication/AuthInput.cs` | Validates email addresses and normalized school slugs. |
| `backend/src/SchoolPlatform.Application/Authentication/RecoveryContracts.cs` | Defines recovery requests/service and shared backend password policy. |
| `backend/src/SchoolPlatform.Application/Email/IEmailSender.cs` | Defines the email delivery abstraction. |
| `backend/src/SchoolPlatform.Application/Platform/BootstrapSchoolRequest.cs` | Adds an optional initial administrator password for atomic bootstrap signup. |
| `backend/src/SchoolPlatform.Application/Platform/CreateSchoolRequest.cs` | Defines signup request/outcomes/service and explicit bootstrap conflict exception. |
| `backend/src/SchoolPlatform.Domain/Identity/PasswordRecoveryJob.cs` | Models a durable delivery request without a raw token. |
| `backend/src/SchoolPlatform.Domain/Identity/PasswordResetToken.cs` | Models hashed, user/tenant-bound expiring tokens and usage timestamp. |
| `backend/src/SchoolPlatform.Domain/Identity/User.cs` | Rotates a security stamp whenever the password hash changes. |
| `backend/src/SchoolPlatform.Infrastructure/Authentication/AuthenticationService.cs` | Includes the current security stamp in issued JWTs. |
| `backend/src/SchoolPlatform.Infrastructure/Authentication/PasswordRecoveryService.cs` | Issues hash-only tokens and resets passwords transactionally, invalidating all outstanding tokens. |
| `backend/src/SchoolPlatform.Infrastructure/Persistence/Configurations/PasswordRecoveryConfiguration.cs` | Maps recovery tables, indexes and foreign keys. |
| `backend/src/SchoolPlatform.Infrastructure/Persistence/Configurations/UserConfiguration.cs` | Maps nullable security stamp without changing existing user data. |
| `backend/src/SchoolPlatform.Infrastructure/Persistence/Migrations/20260909194530_AddPasswordRecovery.Designer.cs` | Records the EF target model for the migration. |
| `backend/src/SchoolPlatform.Infrastructure/Persistence/Migrations/20260909194530_AddPasswordRecovery.cs` | Adds recovery tables and nullable user security stamp; no tenant or password data changes. |
| `backend/src/SchoolPlatform.Infrastructure/Persistence/Migrations/SchoolPlatformDbContextModelSnapshot.cs` | Updates the EF snapshot for recovery entities and security stamps. |
| `backend/src/SchoolPlatform.Infrastructure/Persistence/SchoolPlatformDbContext.cs` | Exposes recovery token and delivery-job sets. |
| `backend/src/SchoolPlatform.Infrastructure/Platform/SchoolBootstrapService.cs` | Uses the existing password hasher and permission catalogue; saves the entire school atomically and guards existing identities. |
| `backend/src/SchoolPlatform.Infrastructure/Platform/SchoolSignupService.cs` | Enforces public-signup flag, slug reservation, global email semantics and safe uniqueness-race handling. |
| `backend/tests/SchoolPlatform.IntegrationTests/AuthenticationFactory.cs` | Builds isolated Production-mode API test hosts with test mail delivery, clock and SQLite/PostgreSQL databases. |
| `backend/tests/SchoolPlatform.IntegrationTests/AuthenticationRecoveryTests.cs` | Tests recovery, token lifecycle, login/password/session changes, signup isolation, gating and rate limits. |
| `backend/tests/SchoolPlatform.IntegrationTests/PostgresRecoveryTests.cs` | Forces concurrent reset snapshots and verifies additive migration data preservation on PostgreSQL. |
| `backend/tests/SchoolPlatform.IntegrationTests/SchoolPlatform.IntegrationTests.csproj` | Adds ASP.NET test hosting, relational SQLite and API project reference. |
| `backend/tests/SchoolPlatform.UnitTests/AuthenticationInputTests.cs` | Replaces the placeholder unit test with password-policy and slug-validation cases. |
| `backend/tests/SchoolPlatform.UnitTests/SchoolPlatform.UnitTests.csproj` | References Application for authentication input-policy tests. |
| `backend/tests/SchoolPlatform.UnitTests/UnitTest1.cs` | Removes the original empty unit-test placeholder, replaced by AuthenticationInputTests.cs. |
| `docs/authentication.md` | Documents configuration, behavior, rollout, validation limitations and this file manifest. |
| `frontend/web/README.md` | Links deployment instructions and documents recovery, signup and tenant-aware login. |
| `frontend/web/next.config.ts` | Adds no-referrer/no-store reset-page and authentication response headers. |
| `frontend/web/package.json` | Adds npm test using the existing Node test runner; no new frontend dependencies. |
| `frontend/web/src/app/(auth)/create-school/page.tsx` | Adds the server-gated new-school page. |
| `frontend/web/src/app/(auth)/forgot-password/page.tsx` | Adds the email/school recovery page with Antioch default. |
| `frontend/web/src/app/(auth)/login/page.tsx` | Shows reset success, seeds the school slug and conditionally links to school creation. |
| `frontend/web/src/app/(auth)/reset-password/page.tsx` | Adds the dynamic reset page with no-index/no-referrer metadata. |
| `frontend/web/src/app/api/auth/forgot-password/route.ts` | Adds the same-origin forgot-password BFF route. |
| `frontend/web/src/app/api/auth/login/route.ts` | Preserves 401/429 versus service errors and rejects cross-origin login requests. |
| `frontend/web/src/app/api/auth/reset-password/route.ts` | Adds the same-origin reset-password BFF route. |
| `frontend/web/src/app/api/auth/signup/route.ts` | Adds the same-origin signup BFF route. |
| `frontend/web/src/components/auth/auth-card.tsx` | Provides accessible shared recovery/onboarding page structure. |
| `frontend/web/src/components/auth/create-school-form.tsx` | Collects new school/admin details, validates confirmation and shows a generic signup outcome. |
| `frontend/web/src/components/auth/login-form.tsx` | Makes Forgot password functional, accepts a school slug and uses server-resolved post-login routing. |
| `frontend/web/src/components/auth/recovery-form.tsx` | Implements forgot/reset interactions, password confirmation, token removal from URL and successful-reset navigation. |
| `frontend/web/src/lib/api/public-auth.ts` | Proxies recovery/signup with feature gating, origin checks, sanitized failures and session clearing after reset. |
| `frontend/web/src/lib/auth/recovery.ts` | Shares frontend password rules, generic messages and safe login destinations. |
| `frontend/web/tests/login.mjs` | Extends login BFF tests for 429 handling and cross-origin rejection. |
| `frontend/web/tests/recovery.mjs` | Tests password rules, recovery/signup proxy behavior, safe errors, gating and cookie clearing. |
