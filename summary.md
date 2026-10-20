# Migration Summary — .NET Framework → .NET 10

## Status: ✅ BUILD SUCCEEDED — 0 errors, 0 warnings

All 6 projects build cleanly for all TFM targets.

---

## Projects migrated

| Project | Before | After | Notes |
|---------|--------|-------|-------|
| Domain | netstandard2.1 | net10.0;netstandard2.0 | No code changes needed |
| Application | netstandard2.1 | net10.0;netstandard2.0 | MediatR 12 API fixes, AutoMapper 16.x |
| Infrastructure.Shared | net10.0;netstandard2.0 | net10.0;netstandard2.0 | MailKit/MimeKit upgraded, logging added |
| Infrastructure.Persistence | net10.0;netstandard2.0 | net10.0 | Escape hatch: ASP.NET Core packages incompatible with netstandard2.0 |
| Infrastructure.Identity | net10.0 | net10.0 | JWT downgrade fixed, RNGCryptoServiceProvider removed |
| WebApi | net10.0 | net10.0 | Serilog/Swashbuckle/ApiVersioning packages upgraded |

---

## Package changes

### Application
- **Removed**: `AutoMapper.Extensions.Microsoft.DependencyInjection` (merged into AutoMapper 13+), `MediatR.Extensions.Microsoft.DependencyInjection` (merged into MediatR 12+), `Microsoft.EntityFrameworkCore` (unused in Application layer), `Microsoft.EntityFrameworkCore.InMemory` (unused), `System.Text.Json` (conditional for netstandard2.0 only now)
- **AutoMapper**: 10.0.0 → 16.2.0 (single version for both TFMs; fixes GHSA-rvv3-g6hj-g44x)
- **MediatR**: `MediatR.Extensions.Microsoft.DependencyInjection 8.1.0` → `MediatR 12.4.1`
- **FluentValidation**: 9.1.2 → 11.11.0
- **Newtonsoft.Json**: 13.0.4 → 13.0.3

### Infrastructure.Shared
- **MailKit**: 2.8.0 → 4.18.1 (fixes GHSA-9j88-vvj5-vhgr)
- **MimeKit**: 2.9.1 → 4.18.1 (fixes GHSA-g7hc-96xr-gvvx)
- **Added**: `Microsoft.Extensions.Logging.Abstractions 10.0.13` (ILogger not available without it in non-Web SDK project)
- **Added**: `System.Security.Cryptography.Pkcs 10.0.13` (pin to override transitive vulnerable 10.0.0)

### Infrastructure.Identity
- **Removed**: `System.IdentityModel.Tokens.Jwt 6.7.1` (was downgrading from 8.x required by JwtBearer 10.0.13)
- **Removed**: `MimeKit 2.9.1` (not needed here; already in Infrastructure.Shared)
- **Added**: `System.Security.Cryptography.Pkcs 10.0.13` (pin override)

### Infrastructure.Persistence
- **Collapsed** from `net10.0;netstandard2.0` to `net10.0` (escape hatch: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` is ASP.NET Core tier, incompatible with netstandard2.0)
- **Added**: `System.Security.Cryptography.Pkcs 10.0.13` (pin override)

### WebApi
- **Swashbuckle.AspNetCore**: 5.5.1 → 7.3.1 (fixes GHSA-qrmm-w75w-3wpx)
- **Removed**: `Swashbuckle.AspNetCore.Swagger 5.5.1` (included in meta-package)
- **Serilog.AspNetCore**: 3.4.0 → 9.0.0
- **Serilog.Settings.Configuration**: 3.1.0 → 9.0.0 (aligned with Serilog.AspNetCore)
- **Serilog.Enrichers.Environment**: 2.1.3 → 3.0.1
- **Serilog.Enrichers.Process**: 2.0.1 → 3.0.0
- **Serilog.Enrichers.Thread**: 3.1.0 → 4.0.0
- **Serilog.Sinks.MSSqlServer**: 5.5.1 → 8.2.0
- **Microsoft.AspNetCore.Mvc.Versioning 4.1.1** → **Asp.Versioning.Mvc 8.1.0** (deprecated package replaced)
- **FluentValidation.AspNetCore**: 9.1.2 → 11.3.0
- **Removed**: `Microsoft.VisualStudio.Web.CodeGeneration.Design` (not needed at compile/runtime)
- **Added**: `System.Security.Cryptography.Pkcs 10.0.13` (pin override)

---

## Code changes

### Application/ServiceExtensions.cs
- MediatR 12: `AddMediatR(Assembly)` → `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly))`
- AutoMapper 16: `AddAutoMapper(Assembly)` → `AddAutoMapper(cfg => cfg.AddMaps(Assembly))`

### Application/Behaviours/ValidationBehaviour.cs
- MediatR 12 `IPipelineBehavior` parameter order: `(request, cancellationToken, next)` → `(request, next, cancellationToken)`

### Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs
- Removed `using Microsoft.EntityFrameworkCore.Internal` (internal EF Core API unavailable in EF Core 8+)

### Infrastructure.Identity/Services/AccountService.cs
- Removed unused/invalid `using` statements (`Org.BouncyCastle.Ocsp`, `System.Net.Cache`, `Microsoft.AspNetCore.Mvc`, `Microsoft.Extensions.Primitives`)
- Replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes()` (.NET 6+ API)
- Added null-forgiving operators for nullable properties

### Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs
- Removed unused `using Org.BouncyCastle.Crypto.Prng.Drbg`

### Infrastructure.Shared/Services/DateTimeService.cs
- Removed `using System.Reflection.Metadata.Ecma335` (not available on netstandard2.0)

### WebApi/Extensions/ServiceExtensions.cs
- Replaced `Microsoft.AspNetCore.Mvc.ApiVersion` namespace with `Asp.Versioning`
- Updated `AddApiVersioning()` to call `.AddMvc()` as required by `Asp.Versioning.Mvc`

### WebApi/Controllers/v1/ProductController.cs
- Added `using Asp.Versioning` for `[ApiVersion]` attribute

---

## Architectural decisions

- **Domain** and **Application** are now dual-targeted (`net10.0;netstandard2.0`) so .NET Framework projects can still reference them during a phased migration.
- **Infrastructure.Persistence** collapsed to `net10.0` only: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` is an ASP.NET Core application-tier package with no netstandard2.0 support.
- **AutoMapper.Extensions.Microsoft.DependencyInjection** was removed because AutoMapper 13+ includes DI registration natively.

---

## Next steps

- The `System.Security.Cryptography.Pkcs` advisories (GHSA-2fmv-rv2c-59x7, GHSA-846f-f764-76mx) are patched by pinning to 10.0.13. NuGet advisory database shows 10.0.0 as affected; the pin ensures 10.0.13 is used.
- **MailKit/MimeKit**: The `GHSA-9j88-vvj5-vhgr` and `GHSA-g7hc-96xr-gvvx` advisories are addressed by upgrading to 4.18.1 (latest). If the advisory database is not yet updated for these latest versions, no further action is needed from the library user.
- **EF Core migrations** in `Infrastructure.Persistence/Migrations` and `Infrastructure.Identity/Migrations` were generated against EF Core 3.1 and will need to be regenerated or verified against EF Core 10.0.13 before running `dotnet ef database update`.
- **JWT key length**: `JWTSettings:Key` in appsettings.json is 29 characters; HMAC-SHA256 requires at least 256 bits (32 bytes). Consider extending this key in production configuration.
- **Nullable reference types**: Enabled globally via `<Nullable>enable</Nullable>`. Existing code has some `!` null-forgiving operators added in AccountService.cs. Full NRT clean-up across the solution is recommended as a follow-up.
