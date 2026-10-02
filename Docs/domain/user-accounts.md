---
title: User Accounts and Sign-in
domain: User Accounts
topics:
  - Register an account
  - Sign in
  - Sign out
codeReferences:
  - src/platform/Platform.Common/Services/AuthService.cs
  - src/platform/Platform.Common/Identity/AppUser.cs
  - src/web/src/app/features/auth
generatedDate: 2026-09-18
lastVerified: 2026-10-01
---

# Overview of User Accounts

A person registers with an **email**, a **username**, a **first name** and a **surname**,
and signs in with their **email and password**.

## Rules

1. Email identifies the person. No two accounts may share one, compared case-insensitively,
   and it is the credential used to sign in.
2. Username is also unique, case-insensitively, but it is neither the database key nor a
   credential - it is the display name the UI shows. See `Docs/DECISIONS.md` D-0005.
3. Username is 3 to 64 characters, limited to letters, digits and `. _ @ + -`.
4. First name and surname are **required at registration** and at most 64 characters each,
   with no restricted character set - a name may carry apostrophes, hyphens, spaces and
   accents. They are **nullable in the database**, so the accounts that predate them have
   neither and nothing can be added to make them appear. See `Docs/DECISIONS.md` D-0007.
5. Password policy: at least 8 characters, with an upper-case letter, a lower-case letter,
   a digit and a symbol. The policy lives in Identity configuration, not in a validator,
   so there is one definition of it.
6. Every new account is granted the `User` role. `Moderator` and `Administrator` are
   granted by an administrator - see [user-permissions.md](user-permissions.md).
7. Ten failed sign-in attempts lock the account for 15 minutes.
8. A failed sign-in never reveals whether an account exists: an unknown email and a wrong
   password return the same error.

## Sessions

Signing in returns an access token that is valid for a bounded period, after which the
person signs in again. Signing out discards the token in the browser; it is not revoked
server-side, so it remains technically valid until it expires (`Docs/DECISIONS.md`
D-0006).

## Not in scope yet

- Email verification, and password reset.
- **Editing your own first name or surname after registration.** This is the only way an
  account that predates D-0007 could ever acquire a name, so until it exists those
  accounts read as username-only everywhere.
- Changing an email or username after registration. Changing an email changes a
  credential, so it needs the same care as a password change.
- Revoking a token before it expires.
