# Temporary direct password recovery

This is a TEMPORARY, operator-controlled mechanism for recovery without SMTP or
email links. `/forgot-password` now asks for email, school slug, recovery code,
new password, and confirmation. Antioch defaults to `antiochcollege41@gmail.com`
and `antioch-college`. No recovery code is embedded in frontend code, defaults,
URLs, local storage, or browser environment variables.

## Required Railway environment variables

| Variable | Configuration |
| --- | --- |
| `TEMP_PASSWORD_RESET_ENABLED` | Must be `true` to enable this endpoint. Unset or `false` disables it. |
| `TEMP_PASSWORD_RESET_CODE` | A cryptographically random secret, 32–256 characters. Prefer 32 random bytes encoded as 64 hex characters. Configure it only in Railway, never in source, appsettings, Vercel, or a `NEXT_PUBLIC_` variable. |

Disable the mechanism entirely with `TEMP_PASSWORD_RESET_ENABLED=false` and
redeploy/apply the Railway environment change. The server checks the flag on
requests, not only at startup. Missing, blank, or short configured codes fail
closed. Comparisons are exact: the recovery code is not trimmed or case-normalized.

The code is a deployment-wide privileged credential, not proof of mailbox ownership
or a per-user code. A holder can reset any active account with a matching active
school membership. Keep it with trusted operators, deliver it through a secure
channel if needed, and disable this temporary path once normal recovery is restored.
Tenant matching prevents using one school's membership to reset an account that
has no active membership there; it does not restrict this shared secret to Antioch.

Vercel still requires the server-only `SCHOOL_PLATFORM_API_URL` pointing to Railway.
No new Vercel environment variable and no SMTP configuration are needed for direct
reset. Existing database/JWT configuration remains required. The previous additive
`20260909194530_AddPasswordRecovery` migration must already be applied because the
shared reset code consumes outstanding reset tokens and rotates `users.SecurityStamp`.
This change adds no new migration and performs no manual production database work.

## Endpoint and security behavior

`POST /api/auth/direct-password-reset` accepts `email`, `tenantSlug`, `recoveryCode`,
and `newPassword`. Email plus new password alone cannot reset an account.

The backend reads the code exclusively through `Environment.GetEnvironmentVariable`
in its production access implementation. It compares SHA-256 digests using
`CryptographicOperations.FixedTimeEquals`, before account lookup. The access reader
can be replaced only through server dependency injection for isolated tests.

The endpoint returns 404 when disabled. Bad code, unknown account, wrong tenant,
inactive user/tenant/membership, or invalid password receive the same generic 400
response. Successful responses contain only the authenticated recovery tenant slug.
The password policy remains 12–128 characters, including an ASCII letter and digit.
The frontend also validates confirmation; passwords are never trimmed.

A serializable transaction uses the same shared password-update method as token
recovery. It hashes with the existing ASP.NET `IPasswordHasher<User>`, calls
`User.SetPasswordHash` to rotate SecurityStamp, and consumes all outstanding reset
tokens for that global user. Existing JWTs then fail the existing stamp check.
Passwords are global to a user across their memberships; no tenant, membership,
role, or permission is created or changed by reset.

The BFF uses same-origin submission checks, sends credentials only in the POST
body, returns sanitized service errors, and clears the old HttpOnly session cookie
on success. It redirects to login with a success message and no sensitive values
in the URL. Neither the backend nor the BFF logs codes, passwords, hashes, JWTs,
or database credentials. Keep platform HTTP body logging disabled for auth routes.

The endpoint has an instance-wide limit of ten attempts/minute, including attempts
across different account names. It additionally shares the recovery quota of three
attempts per email/tenant pair per 15 minutes. Limits are in-process; multiple API
replicas multiply the global quota.

## Retained email recovery

The token model, migration, mail queue/worker, SMTP sender, token endpoints, and
legacy `RecoveryForm` remain in the repository. No current page renders the legacy
form. `/reset-password` redirects to `/forgot-password` without forwarding the token.
To restore email recovery later, reconnect the original form to its pages and disable
the temporary endpoint. Previously queued email jobs may still be processed by the
retained worker; the direct-reset path does not enqueue or send mail.

## Changed files

Paths are relative to the repository root.

| File | Change |
| --- | --- |
| `backend/src/SchoolPlatform.Application/Authentication/RecoveryContracts.cs` | Direct-reset request and service/access contracts. |
| `backend/src/SchoolPlatform.Infrastructure/Authentication/TemporaryPasswordResetAccess.cs` | Environment-only flag/code reader and constant-time digest comparison. |
| `backend/src/SchoolPlatform.Infrastructure/Authentication/PasswordRecoveryService.cs` | Active tenant/account lookup, transactional direct reset, and shared password/token/stamp update. |
| `backend/src/SchoolPlatform.Api/Endpoints/AuthenticationRecoveryEndpoints.cs` | Gated direct-reset endpoint with generic failures and account quota. |
| `backend/src/SchoolPlatform.Api/Program.cs` | Registers temporary access and endpoint-wide rate limiting. |
| `backend/tests/SchoolPlatform.IntegrationTests/AuthenticationFactory.cs` | Injects random test-only recovery codes and isolated feature settings. |
| `backend/tests/SchoolPlatform.IntegrationTests/DirectPasswordResetTests.cs` | Covers disabled/missing/wrong codes, unknown/inactive/wrong-tenant accounts, password changes, session/token revocation, and quotas. |
| `frontend/web/src/components/auth/direct-recovery-form.tsx` | New form with Antioch defaults, empty masked code field, confirmation, and login redirect. |
| `frontend/web/src/app/(auth)/forgot-password/page.tsx` | Uses temporary direct-reset form. |
| `frontend/web/src/app/(auth)/reset-password/page.tsx` | Temporarily redirects legacy token UI to forgot-password. |
| `frontend/web/src/app/api/auth/direct-password-reset/route.ts` | Same-origin BFF entry point. |
| `frontend/web/src/lib/api/public-auth.ts` | Proxies the new endpoint, sanitizes failures, and expires the session cookie on success. |
| `frontend/web/src/lib/auth/recovery.ts` | Builds validated direct-reset payloads and checks password confirmation. |
| `frontend/web/tests/recovery.mjs` | Tests payload validation, endpoint forwarding, errors, cookie clearing and form rendering. |
| `frontend/web/README.md` | Identifies the current temporary flow and links configuration instructions. |
| `docs/authentication.md` | Marks the email UI documentation as superseded for now. |
| `docs/temporary-password-reset.md` | Temporary flow, disable switch, deployment prerequisites and file summary. |
