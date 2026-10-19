# Migration Summary: ASP.NET Core 3.1 → .NET 10 Upgrade

## Status: ✅ Build Succeeds — 0 Compilation Errors

All six target projects build successfully against `net10.0`:
- `Domain/Domain.csproj`
- `Application/Application.csproj`
- `Infrastructure.Shared/Infrastructure.Shared.csproj`
- `Infrastructure.Persistence/Infrastructure.Persistence.csproj`
- `Infrastructure.Identity/Infrastructure.Identity.csproj`
- `WebApi/WebApi.csproj`

---

## Changes Made

### Project Files

| Project | Before | After | Notes |
|---|---|---|---|
| Domain | `netstandard2.1` | `net10.0;netstandard2.0` | Multi-targeted for downstream .NET Framework callers |
| Application | `netstandard2.1` | `net10.0;netstandard2.0` | Same; kept EF Core 3.1.7 refs (supports both TFMs) |
| Infrastructure.Persistence | `net10.0;netstandard2.0` | same (fixed) | Removed unneeded `AspNetCore.Identity.EF` ref; made EF Core SqlServer/InMemory conditional (v10 for net10.0, v3.1.7 for netstandard2.0) |
| Infrastructure.Identity | `net10.0` | same (fixed) | Removed downgraded `System.IdentityModel.Tokens.Jwt 6.7.1` (caused NU1605); removed redundant `MimeKit`, `Microsoft.Extensions.DependencyInjection` refs |
| Infrastructure.Shared | no changes to TFMs | — | NU1201 resolved by fixing Application to support netstandard2.0 |
| WebApi | `net10.0` | no change | Already on net10.0 |

### Code Fixes

| File | Change |
|---|---|
| `Infrastructure.Identity/Services/AccountService.cs` | Removed invalid `using Org.BouncyCastle.Ocsp` and `using System.Net.Cache` (not available in .NET Core). Removed unused `using Microsoft.AspNetCore.Mvc` and `using Microsoft.Extensions.Primitives`. Replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes()` |
| `Infrastructure.Identity/ServiceExtensions.cs` | Removed unused `using System.Reflection.Metadata.Ecma335` |
| `Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs` | Removed invalid `using Org.BouncyCastle.Crypto.Prng.Drbg` and unused logging/collection imports |
| `Infrastructure.Shared/Services/DateTimeService.cs` | Removed invalid `using System.Reflection.Metadata.Ecma335` |
| `Application/Interfaces/IAccountService.cs` | Removed unused `using Microsoft.Extensions.Primitives` |

---

## Remaining Warnings (All NU190x — Package Vulnerabilities)

All 49 build warnings are NuGet security audit warnings (`NU1901`/`NU1902`/`NU1903`). None of these can be resolved without newer package versions that are not currently present in the NuGet cache.

---

## Next Steps

The following items require upgraded package versions (not available in local cache) or a human/product decision:

1. **AutoMapper 10.0.0** — High severity vulnerability (GHSA-rvv3-g6hj-g44x). Upgrade to AutoMapper 12.x (with `AutoMapper.Extensions.Microsoft.DependencyInjection 12.x`). `AddMediatR(Assembly)` call in `Application/ServiceExtensions.cs` may need updating for v12+.
2. **MailKit 2.8.0** — Moderate severity (GHSA-9j88-vvj5-vhgr). Upgrade to MailKit 4.x.
3. **MimeKit 2.9.1** — Moderate severity (GHSA-g7hc-96xr-gvvx). Upgrade to MimeKit 4.x.
4. **Swashbuckle.AspNetCore.SwaggerUI 5.5.1** — Moderate severity (GHSA-qrmm-w75w-3wpx). Upgrade to Swashbuckle.AspNetCore 7.x or migrate to `Microsoft.AspNetCore.OpenApi` (built-in since .NET 9).
5. **Microsoft.AspNetCore.Mvc.Versioning 4.1.1** — Deprecated. Migrate to `Asp.Versioning.Mvc` 8.x and update `ServiceExtensions.AddApiVersioningExtension()` accordingly.
6. **System.Security.Cryptography.Pkcs 9.0.11** — Moderate/high severity (GHSA-2fmv-rv2c-59x7, GHSA-846f-f764-76mx). Pulled in transitively by `Microsoft.Data.SqlClient 6.1.7`. Resolved once `Microsoft.Data.SqlClient 6.2+` depends on Pkcs ≥ 9.0.12.
7. **Microsoft.Data.SqlClient 1.1.3** — Moderate/high severity (GHSA-8g2p-5pqh-5jmc, GHSA-98g6-xh36-x2p7). Pulled in on the `netstandard2.0` leg by `Microsoft.EntityFrameworkCore.SqlServer 3.1.7`. Resolved by upgrading the netstandard2.0 conditional package reference for SqlServer (requires a version that supports netstandard2.0 AND uses a non-vulnerable SqlClient).
8. **MediatR 8.1.0** — Old package (pre-DI-merge era). When upgrading to MediatR 12.x: rename `MediatR.Extensions.Microsoft.DependencyInjection` → no longer needed (merged into `MediatR`); update `services.AddMediatR(Assembly)` to `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly))`; update `IPipelineBehavior.Handle` parameter order in `ValidationBehavior<TRequest, TResponse>` (cancellationToken moved to 3rd position after `next`).
9. **Serilog.AspNetCore 3.4.0 / Serilog.Sinks.MSSqlServer 5.5.1** — Old versions; upgrade to Serilog.AspNetCore 8.x and Serilog.Sinks.MSSqlServer 8.x for full .NET 10 support.
10. **NuGet.Packaging / NuGet.Protocol 6.12.1** — Low severity (GHSA-g4vj-cjjj-v7hg). These are tool-chain transitive dependencies; resolved by SDK/tooling update.
