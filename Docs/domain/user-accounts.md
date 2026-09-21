---
title: User Accounts, Sign-in and Finding People
domain: User Accounts
topics:
  - Register an account
  - Sign in
  - Sign out
  - Find another person
  - Read somebody's profile
codeReferences:
  - src/api/Services/AuthService.cs
  - src/api/Services/UserService.cs
  - src/api/Entities/AppUser.cs
  - src/web/src/app/features/auth
  - src/web/src/app/core/user-search
  - src/web/src/app/features/users
generatedDate: 2026-09-18
lastVerified: 2026-09-21
---

# Overview of User Accounts

A person registers with an **email**, a **username**, a **first name** and a **surname**,
and signs in with their **email and password**.

## Rules

1. Email identifies the person. No two accounts may share one, compared case-insensitively,
   and it is the credential used to sign in.
2. Username is also unique, case-insensitively, but it is neither the database key nor a
   credential - it is the display name the UI shows. See `Docs/DECISIONS.md` D-0009.
3. Username is 3 to 64 characters, limited to letters, digits and `. _ @ + -`.
4. First name and surname are **required at registration** and at most 64 characters each,
   with no restricted character set - a name may carry apostrophes, hyphens, spaces and
   accents. They are **nullable in the database**, so the accounts that predate them have
   neither and nothing can be added to make them appear. See `Docs/DECISIONS.md` D-0016.
5. Password policy: at least 8 characters, with an upper-case letter, a lower-case letter,
   a digit and a symbol. The policy lives in Identity configuration, not in a validator,
   so there is one definition of it.
6. Every new account is granted the `User` role. `Moderator` and `Administrator` are
   granted by an administrator - see [user-permissions.md](user-permissions.md).
7. Ten failed sign-in attempts lock the account for 15 minutes.
8. A failed sign-in never reveals whether an account exists: an unknown email and a wrong
   password return the same error.

## Finding other people

Anybody signed in can search for anybody else and open their profile. The search is an
autocomplete in the application header, and a profile lists that person's saved beverage
preferences - which is what the search is *for*
(see [beverage-preference.md](beverage-preference.md) rule 9 and `Docs/DECISIONS.md`
D-0017).

### What a term matches

One term is matched, case-insensitively, against **username**, **first name** or
**surname**, anywhere in the value rather than only at the start. So "gust" finds Augusta.

| Rule | Behaviour |
| --- | --- |
| Shortest term | 2 characters. One letter would match most of the table. |
| Longest term | 64 characters. |
| Matches returned | 10 by default, at most 25. |
| The searcher | Never in their own results. |
| Nobody matches | An empty list, not an error. |
| Ordering | A username starting with the term first, then the rest, each alphabetical by username - so the dropdown does not reshuffle as somebody types. |

An account with no names is still found by its username, which every account has.

### What is never exposed

**Email.** It is the sign-in credential (D-0009), so a search that returned it would let
anybody confirm which addresses are registered - the same leak that
`auth.invalid_credentials` exists to prevent. Neither the search results nor a profile
carries one, and there is a test asserting it on the raw response body rather than through
a DTO, because a DTO only proves what it declares.

**Roles**, too: what somebody else is allowed to do is not a searcher's business.

## Sessions

Signing in returns an access token that is valid for a bounded period, after which the
person signs in again. Signing out discards the token in the browser; it is not revoked
server-side, so it remains technically valid until it expires (`Docs/DECISIONS.md`
D-0010, D-0011).

## Not in scope yet

- Email verification, and password reset.
- **Editing your own first name or surname after registration.** This is the only way an
  account that predates D-0016 could ever acquire a name, so until it exists those
  accounts read as username-only everywhere.
- Changing an email or username after registration. Changing an email changes a
  credential, so it needs the same care as a password change.
- Revoking a token before it expires.
- Searching by email. Deliberately never, for the reason above.
- Paging the search results. A capped limit is the whole of it; an autocomplete that pages
  is a different interaction.
