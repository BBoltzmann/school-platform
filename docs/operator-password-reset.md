# Operator school-user password reset

The operator command uses the same `PasswordHasher<User>`, password policy,
security-stamp rotation, and reset-token invalidation as the normal reset/setup
service. It does not create users or change memberships and roles.

From the repository root, run a safe inspection first:

```sh
dotnet run --project backend/src/SchoolPlatform.Operator -- reset-school-user-password \
  --school-slug antioch-primary-school \
  --email antiochschools210@gmail.com \
  --dry-run
```

For a reset, omit `--dry-run`. The command reads `SCHOOL_OPERATOR_PASSWORD`
when set, otherwise it prompts without echoing the password. The password is
never a command-line argument or log value.

For Railway, use the existing Railway CLI environment injection from a trusted
operator workstation. Do not copy a database URL into source or logs:

```sh
railway run --service <backend-service> \
  dotnet run --project backend/src/SchoolPlatform.Operator -- \
  reset-school-user-password \
  --school-slug antioch-primary-school \
  --email antiochschools210@gmail.com
```

The command prompts for the password interactively. Run the dry-run first and
confirm the school, user, enabled state, setup state, and roles before making a
change. No migration is required.
