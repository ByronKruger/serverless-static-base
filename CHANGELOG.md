# Changelog

What changed for a consumer, per release. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html), where "public API" means the
types and members a consumer compiles against and the configuration sections it binds.

## [Unreleased]

### Changed - breaking

- **Renamed the library and every namespace.** `Platform.Common` is now
  `HopeTech.Common`, and the public surface renamed with it:

  | Before | After |
  | --- | --- |
  | `Platform.Common.*` (namespaces) | `HopeTech.Common.*` |
  | `AddPlatformApi<TDbContext>` | `AddHopeTechApi<TDbContext>` |
  | `UsePlatformApiPipeline` | `UseHopeTechApiPipeline` |
  | `PlatformIdentityDbContext` | `HopeTechIdentityDbContext` |
  | `CorsOptions.PolicyName` = `"PlatformWebClient"` | `"HopeTechWebClient"` |

  The CORS policy name is a string constant, so a consumer that passed it to
  `UseCors` by value rather than through the constant has to change that call too.
  Everything else is a compile error, which is the intent.

- **Configuration is unchanged.** `Authentication:Jwt`, `Cors` and `Database` bind exactly
  as before. A rename of the library is not a reason to invalidate a deployment's settings
  or its user-secrets store.

### Added

- **`HopeTech.Common.Templates`**, a `dotnet new` item-template package
  (`dotnet new install HopeTech.Common.Templates`). Seven templates: `htc-host`,
  `htc-resource`, `htc-contracts`, `htc-service`, `htc-controller`, `htc-entity`,
  `htc-service-tests`. See `README.md` for what each emits.

  `htc-host` carries the register and sign-in controller. The library has always supplied
  `IAuthService` and the auth contracts without exposing them over HTTP, on the grounds that
  route templates and `[ProducesResponseType]` sets are a product's own contract (D-0008).
  Until now every consumer wrote that controller from scratch; now it is scaffolded, and it
  is still the consumer's file to edit.

### Removed

- **The reference host and the client application.** `src/api` and `src/web` are gone from
  this repository, along with the API integration test project that booted the host
  (`tests/Api.Tests`). The host shape they demonstrated is now `htc-host` and
  `htc-resource`, which a consumer runs rather than reads (D-0010).

  Nothing a consumer depends on was in either project. If you forked this repository for the
  reference host, it is in the history of `src/api` and the templates emit the same files.

### Fixed

- `Roles.User` documented itself in terms of a specific product's nouns. The library names
  no product noun anywhere, including in its comments.
