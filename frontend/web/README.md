# School Platform web frontend

Run `npm install` when dependencies are missing, then `npm run dev` from
`frontend/web`. Use Node.js 20.9 or newer. Run `npm run build` before deploying.

## Backend configuration

Set the server-only variable `SCHOOL_PLATFORM_API_URL` to the backend origin.
For Vercel Production (and Preview if it should use production), set:

```text
SCHOOL_PLATFORM_API_URL=https://school-platform-production-09fa.up.railway.app
```

Use `frontend/web` as the Vercel project root. Redeploy after changing environment
variables so the deployment receives the new value. Do not append `/api` or
`/api/auth/login`. Trailing slashes and surrounding whitespace are normalized.
Production requires an explicit value; development defaults to
`http://localhost:5221`. An ignored `.env.local` only configures local runs.

## Login flow and diagnostics

The browser posts email, password, and `tenantSlug: "antioch-college"` to the
same-origin `/api/auth/login` BFF. The BFF posts to the configured backend's
`/api/auth/login` and stores the access token in an HttpOnly, SameSite=Lax cookie
with path `/`, Secure in production, and the backend expiry. The browser then
visits `/`, which redirects to the authenticated tenant dashboard. The tenant layout validates that cookie
by calling `/api/tenant/context` on the same backend. Authenticated API calls
use that same origin and send the token as a Bearer credential. Logout expires
the cookie at the same path.

Only a backend 401 is reported as invalid credentials; 429 is shown as a rate limit. Configuration errors
return 503; other upstream errors, timeouts, or malformed success responses
return 502. Vercel function logs record the upstream origin and HTTP error status
without credentials or tokens. If direct Railway login succeeds, compare the
BFF status and logged origin before changing any credentials. A successful
frontend build does not verify runtime connectivity or deployment environment
variables.

## Recovery and school creation

Password recovery currently uses a temporary recovery-code form at `/forgot-password`.
The legacy `/reset-password` page redirects there. See the
[temporary recovery setup](../../docs/temporary-password-reset.md) for Railway flags
and the disable switch. No recovery code is configured in the frontend. Public onboarding is at `/create-school` and requires
`ALLOW_PUBLIC_SCHOOL_SIGNUP=true` in both Vercel and Railway. Login defaults to
`antioch-college` and also accepts the slug of a newly created school.

See [authentication deployment and file-by-file changes](../../docs/authentication.md)
for the required EF migration, SMTP environment variables, tenant/email semantics,
and validation results. Run `npm test` for authentication regression tests.
