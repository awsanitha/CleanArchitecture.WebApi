# Migration Summary: CleanArchitecture.WebApi — .NET Core 3.1 → .NET 10

## Outcome

**Build: SUCCEEDED — 0 compilation errors, 0 Error(s)**

All 6 projects compile cleanly against `net10.0`. The remaining 28 warnings are all `NU19xx` vulnerability advisories for upstream packages where no non-vulnerable version was available in the environment (see "Remaining warnings" below).

---

## Changes Made

### Project File Updates

| Project | Before | After | Notes |
|---------|--------|-------|-------|
| `WebApi` | `net10.0` (already) | `net10.0` | Updated packages (Swashbuckle 5→6, Serilog 3→8, Asp.Versioning 8, FluentValidation 11) |
| `Application` | `netstandard2.1` | `net10.0;netstandard2.0` | Multi-targeted; conditional packages per TFM |
| `Domain` | `netstandard2.1` | `net10.0;netstandard2.0` | No packages; trivially compatible |
| `Infrastructure.Identity` | `net10.0` (already) | `net10.0` | Fixed package versions (JWT, removed MimeKit, pinned Crypto.Pkcs) |
| `Infrastructure.Persistence` | `net10.0;netstandard2.0` | `net10.0` | Collapsed to single TFM (EF Core 10 + ASP.NET Core Identity only support net10.0) |
| `Infrastructure.Shared` | `net10.0;netstandard2.0` | `net10.0;netstandard2.0` | Fixed missing Logging.Abstractions, updated MimeKit/MailKit |

### Package Updates

| Package | Old Version | New Version | Reason |
|---------|-------------|-------------|--------|
| `Swashbuckle.AspNetCore` | 5.5.1 | 6.9.0 | Vulnerability GHSA-qrmm-w75w-3wpx fixed |
| `Swashbuckle.AspNetCore.Swagger` | 5.5.1 | Removed (included in 6.x) | Redundant |
| `Serilog.AspNetCore` | 3.4.0 | 8.0.3 | Compatibility with .NET 10 |
| `Serilog.Settings.Configuration` | 3.1.0 | 8.0.4 | Required by Serilog.AspNetCore 8.x |
| `Microsoft.AspNetCore.Mvc.Versioning` | 4.1.1 | Removed | Deprecated; replaced by Asp.Versioning.Mvc |
| `Asp.Versioning.Mvc` | — | 8.1.0 | Modern API versioning package |
| `FluentValidation.AspNetCore` | 9.1.2 | 11.3.0 | Compatibility with .NET 10 |
| `System.IdentityModel.Tokens.Jwt` | 6.7.1 | 8.19.2 | Fixed NU1605 downgrade from JwtBearer 10 transitive |
| `AutoMapper` (net10.0) | 10.0.0 | 13.0.1 | Latest available net10.0-compatible version |
| `MediatR` (net10.0) | 8.1.0 | 12.2.0 | Modern, includes DI registration built-in |
| `FluentValidation` | 9.1.2 | 11.9.2 | .NET 10 compatible |
| `MailKit` | 2.8.0 | 4.12.0 | Latest available; vulnerability advisory still active |
| `MimeKit` | 2.9.1 | 4.12.0 | Latest available; vulnerability advisory still active |
| `System.Security.Cryptography.Pkcs` | 9.0.11 | 10.0.14 | Fixed high/moderate vulnerability advisories |
| `NuGet.Packaging` / `NuGet.Protocol` | 6.12.1 | 6.13.1 | Fixed low vulnerability advisory |

### Source Code Changes

| File | Change |
|------|--------|
| `Application/Behaviours/ValidationBehaviour.cs` | Updated `IPipelineBehavior.Handle` signature for MediatR 12 (`next` and `cancellationToken` parameter order swapped), with `#if NET10_0_OR_GREATER` guard for netstandard2.0 leg (MediatR 8 signature) |
| `Application/ServiceExtensions.cs` | Updated `AddMediatR` call to MediatR 12 API (`RegisterServicesFromAssembly`) with `#if NET10_0_OR_GREATER` guard |
| `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs` | Removed stale `using Microsoft.EntityFrameworkCore.Internal;` (unused import) |
| `Infrastructure.Identity/ServiceExtensions.cs` | Removed stale `using System.Reflection.Metadata.Ecma335;` |
| `Infrastructure.Identity/Services/AccountService.cs` | Removed `using Org.BouncyCastle.Ocsp;`, `using System.Net.Cache;`, `using Microsoft.Extensions.Primitives;` (all stale/removed APIs); replaced deprecated `RNGCryptoServiceProvider` with `RandomNumberGenerator.Fill()` |
| `Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs` | Removed stale `using Org.BouncyCastle.Crypto.Prng.Drbg;` |
| `Infrastructure.Shared/Services/DateTimeService.cs` | Removed stale `using System.Reflection.Metadata.Ecma335;` |
| `WebApi/Extensions/ServiceExtensions.cs` | Migrated `ApiVersion` using from `Microsoft.AspNetCore.Mvc.Versioning` → `Asp.Versioning` namespace |
| `WebApi/Controllers/v1/ProductController.cs` | Added `using Asp.Versioning;` for `[ApiVersion]` attribute |

### Architecture Notes

- **Infrastructure.Persistence** collapsed to `net10.0` only: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` and EF Core packages only support `net10.0` in their 10.x releases. The only netstandard2.0-compatible versions are from 2018 (2.x/3.x), which would require a near-complete rewrite of the data access layer. This is the accepted pragmatic trade-off per KB rule for infrastructure projects.
- **Application multi-targeting strategy**: The Application project uses conditional `<PackageReference>` per TFM: modern packages (AutoMapper 13, MediatR 12) for `net10.0`; legacy netstandard2.0-compatible packages (AutoMapper 10.1.1, MediatR 8.1.0) for the `netstandard2.0` leg. Code guards with `#if NET10_0_OR_GREATER` handle API differences.

---

## Remaining Warnings (Cannot Be Resolved Without New Upstream Releases)

| Warning | Package | Advisory | Reason unresolvable |
|---------|---------|----------|---------------------|
| NU1903 | `AutoMapper` 13.0.1 | GHSA-rvv3-g6hj-g44x | Latest available version (13.0.1) still carries advisory; no fix version published to NuGet.org at migration time |
| NU1903 | `AutoMapper` 10.1.1 (netstandard2.0 leg) | GHSA-rvv3-g6hj-g44x | No netstandard2.0-compatible version of AutoMapper is free of this advisory |
| NU1902 | `MailKit` 4.12.0 | GHSA-9j88-vvj5-vhgr | Latest available version; no newer non-vulnerable version in environment |
| NU1902 | `MimeKit` 4.12.0 | GHSA-g7hc-96xr-gvvx | Same as MailKit |

---

## Next Steps

1. **AutoMapper advisory**: Monitor https://github.com/advisories/GHSA-rvv3-g6hj-g44x for a patched release. When a fixed AutoMapper version is published, update `Application.csproj` (net10.0 group) and downstream projects.
2. **MailKit/MimeKit advisory**: Monitor https://github.com/jstedfast/MailKit/releases for a patched release. Update Infrastructure.Shared when available.
3. **Infrastructure.Persistence netstandard2.0**: If .NET Framework 4.6.1 consumers ever need to use this library, the data access layer would need to be refactored with TFM-conditional code. Currently collapsed to `net10.0` only as a pragmatic trade-off.
4. **Serilog.Sinks.MSSqlServer 5.5.1**: This version is old; consider upgrading to 8.x for better .NET 10 support (not updated in this pass since no vulnerability advisory was present and the sink is an optional SQL Server logging target).
5. **FluentValidation.AspNetCore usage review**: The `FluentValidation.AspNetCore` package is present but not actively used in controllers (validation happens via MediatR pipeline). Consider removing it to simplify the dependency graph.
