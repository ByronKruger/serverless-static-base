---
title: User Accounts and Sign-in
domain: User Accounts
topics:
  - Register an account
  - Sign in
  - Sign out
codeReferences:
  - src/api/Services/AuthService.cs
  - src/api/Entities/AppUser.cs
  - src/web/src/app/features/auth
generatedDate: 2026-09-18
lastVerified: 2026-09-18
---

# Overview of User Accounts

A person registers with an **email**, a **username** and a **password**, and signs in with
their **email and password**.

## Rules

1. Email identifies the person. No two accounts may share one, compared case-insensitively,
   and it is the credential used to sign in.
2. Username is also unique, case-insensitively, but it is neither the database key nor a
   credential - it is the display name the UI shows. See `Docs/DECISIONS.md` D-0009.
3. Username is 3 to 64 characters, limited to letters, digits and `. _ @ + -`.
4. Password policy: at least 8 characters, with an upper-case letter, a lower-case letter,
   a digit and a symbol. The policy lives in Identity configuration, not in a validator,
   so there is one definition of it.
5. Every new account is granted the `User` role. `Moderator` and `Administrator` are
   granted by an administrator - see [user-permissions.md](user-permissions.md).
6. Ten failed sign-in attempts lock the account for 15 minutes.
7. A failed sign-in never reveals whether an account exists: an unknown email and a wrong
   password return the same error.

## Sessions

Signing in returns an access token that is valid for a bounded period, after which the
person signs in again. Signing out discards the token in the browser; it is not revoked
server-side, so it remains technically valid until it expires (`Docs/DECISIONS.md`
D-0010, D-0011).

## Not in scope yet

- Email verification, and password reset.
- Changing an email or username after registration. Changing an email changes a
  credential, so it needs the same care as a password change.
- Revoking a token before it expires.
