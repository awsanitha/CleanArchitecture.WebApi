# Migration Summary: CleanArchitecture.WebApi — .NET Core 3.1 → .NET 10

## Final Build Status
✅ `dotnet build CleanArchitecture.WebApi.sln` exits with **code 0**  
✅ **0 errors, 0 warnings**  
✅ All 6 projects build cleanly targeting `net10.0` (Domain stays at `netstandard2.1`)

---

## Changes Made

### Project File Migrations

| Project | Before | After | Notes |
|---|---|---|---|
| `Domain` | `netstandard2.1` | `netstandard2.1` | Kept — no packages, no conflict |
| `Application` | `netstandard2.1` | `net10.0` | Changed: AutoMapper 16.x requires net6.0+ |
| `Infrastructure.Persistence` | `net10.0` | `net10.0` | Already migrated |
| `Infrastructure.Identity` | `net10.0` | `net10.0` | Package fixes only |
| `Infrastructure.Shared` | `net10.0` | `net10.0` | Package fixes only |
| `WebApi` | `net10.0` | `net10.0` | Package upgrades only |

---

### Package Upgrades

#### Application.csproj
| Package | Before | After | Reason |
|---|---|---|---|
| `AutoMapper` | 10.0.0 | **16.2.0** | Fixes NU1903 high severity vulnerability (GHSA-rvv3-g6hj-g44x); 15.1.3 and 16.2.0 are first patched releases |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | 8.0.1 | **Removed** | Merged into AutoMapper 13+ main package |
| `MediatR.Extensions.Microsoft.DependencyInjection` | 8.1.0 | **Removed** | Replaced by `MediatR` 12+ which includes DI registration |
| `MediatR` | _(transitive)_ | **12.4.1** | Direct reference; includes DI extensions |
| `FluentValidation` | 9.1.2 | **11.11.0** | Compatibility with FluentValidation.AspNetCore 11.x |
| `FluentValidation.DependencyInjectionExtensions` | 9.1.2 | **11.11.0** | Aligned upgrade |
| `Microsoft.EntityFrameworkCore` | 3.1.7 | **Removed** | Not used in Application layer (repos/DbContext are in Infrastructure) |
| `Microsoft.EntityFrameworkCore.InMemory` | 3.1.7 | **Removed** | InMemory DB is configured in Infrastructure.Persistence |
| `System.Text.Json` | 10.0.11 | **Removed** | NU1510: auto-included in `net10.0` SDK |

#### Infrastructure.Identity.csproj
| Package | Before | After | Reason |
|---|---|---|---|
| `System.IdentityModel.Tokens.Jwt` | 6.7.1 | **Removed** | NU1605: downgraded the 8.19.2 transitive from `JwtBearer 10.0.11`; removed and let transitive resolution supply 8.19.2 |
| `MimeKit` | 2.9.1 | **4.17.0** | Fixes NU1902 moderate severity vulnerability |

#### Infrastructure.Shared.csproj
| Package | Before | After | Reason |
|---|---|---|---|
| `MailKit` | 2.8.0 | **4.17.0** | Fixes NU1902 moderate severity vulnerability |
| `MimeKit` | 2.9.1 | **4.17.0** | Fixes NU1902 moderate severity vulnerability |
| `Microsoft.Extensions.Logging.Abstractions` | _(missing)_ | **10.0.11** | Added: `EmailService.cs` uses `ILogger<T>`; not auto-available in `Microsoft.NET.Sdk` (non-web) project |

#### WebApi.csproj
| Package | Before | After | Reason |
|---|---|---|---|
| `Swashbuckle.AspNetCore` | 5.5.1 | **6.9.0** | Fixes NU1902 vulnerability in SwaggerUI 5.5.1 |
| `Swashbuckle.AspNetCore.Swagger` | 5.5.1 | **Removed** | Included in main Swashbuckle.AspNetCore 6.x package |
| `Microsoft.AspNetCore.Mvc.Versioning` | 4.1.1 | **Removed** | Deprecated; replaced by `Asp.Versioning.Mvc` |
| `Asp.Versioning.Mvc` | _(missing)_ | **8.1.0** | Modern API versioning package |
| `Serilog.AspNetCore` | 3.4.0 | **8.0.3** | Latest 8.x release (8.0.4 does not exist on NuGet) |
| `Serilog.Enrichers.Environment` | 2.1.3 | **3.0.1** | Latest stable |
| `Serilog.Enrichers.Process` | 2.0.1 | **3.0.0** | Latest stable |
| `Serilog.Enrichers.Thread` | 3.1.0 | **4.0.0** | Latest stable |
| `Serilog.Settings.Configuration` | 3.1.0 | **8.0.4** | Required by Serilog.AspNetCore 8.0.3 |
| `Serilog.Sinks.MSSqlServer` | 5.5.1 | **10.0.0** | Latest stable |
| `FluentValidation.AspNetCore` | 9.1.2 | **11.3.1** | Aligned with FluentValidation 11.x |
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 10.0.2 | **Removed** | Dev-only scaffold tool; caused NU1901 for NuGet.Packaging/NuGet.Protocol |

---

### Code Changes

#### `Application/ServiceExtensions.cs`
- **MediatR 12 API**: `services.AddMediatR(Assembly)` → `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly))`
- **AutoMapper 16 API**: `services.AddAutoMapper(Assembly)` → `services.AddAutoMapper(cfg => cfg.AddMaps(Assembly))` (the `params Assembly[]` overload was removed in AutoMapper 16.x)

#### `Application/Behaviours/ValidationBehaviour.cs`
- **MediatR 12 breaking change**: `IPipelineBehavior.Handle` parameter order changed  
  - Before: `Handle(TRequest, CancellationToken, RequestHandlerDelegate<TResponse>)`  
  - After: `Handle(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken)`

#### `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs`
- Removed stale `using Microsoft.EntityFrameworkCore.Internal;` (EF Core internal API, unused in application layer)

#### `Infrastructure.Identity/ServiceExtensions.cs`
- Removed stale `using System.Reflection.Metadata.Ecma335;` (unused, wrong namespace for this class)

#### `Infrastructure.Identity/Services/AccountService.cs`
- Removed unused/invalid imports: `Org.BouncyCastle.Ocsp` (BouncyCastle not referenced), `System.Net.Cache`, `Microsoft.AspNetCore.Mvc`, `Microsoft.Extensions.Primitives`
- **SYSLIB0023**: Replaced deprecated `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes(40)` (modern .NET 6+ static API)

#### `WebApi/Extensions/ServiceExtensions.cs`
- Added `using Asp.Versioning;` for the new versioning package
- Removed unused `System.Linq` and `System.Threading.Tasks` imports
- Updated `AddApiVersioningExtension`: chained `.AddMvc()` on `AddApiVersioning()` (required by `Asp.Versioning.Mvc 8.x`)

#### `WebApi/Controllers/v1/ProductController.cs`
- Added `using Asp.Versioning;` so `[ApiVersion("1.0")]` resolves from the new package namespace

---

## Behavioral Notes

- **AutoMapper 16.2.0** includes `Microsoft.IdentityModel.JsonWebTokens >= 8.14.0` as a transitive dependency (added in AutoMapper 15.x for claim mapping support). This is resolved to `8.19.2` via the JwtBearer 10.0.11 transitive chain — no conflict.
- **`Application` project retargeted to `net10.0`**: Previously `netstandard2.1`. Changed because AutoMapper 13+ requires `net6.0+`. Since this library has no external consumers outside the solution and the entire runtime is .NET 10, this is safe.
- **MediatR 12 parameter reorder in `IPipelineBehavior.Handle`** is a silent behavioural fix: the old order placed `CancellationToken` before the `next` delegate, which could cause cancellation to not propagate correctly through the pipeline.

---

## Next Steps

- **AutoMapper GHSA-rvv3-g6hj-g44x**: Fixed by upgrading to 16.2.0. Monitor for any new advisories.
- **Serilog.Sinks.MSSqlServer 10.0.0**: May have configuration schema changes vs 5.5.1. Validate the Serilog JSON config in `appsettings.json` against the new sink's expected format if SQL Server logging is used in production.
- **Connection strings in `appsettings.json`**: Still point to `DESKTOP-QCM5AL0` (Windows machine-specific). Update for target deployment environment.
- **EF Core migrations**: Existing migrations in `Infrastructure.Persistence/Migrations` and `Infrastructure.Identity/Migrations` are EF Core 3.1-era migrations. They compile fine but should be validated against EF Core 10's migration engine before production deployment. Re-running `dotnet ef migrations add` may be needed if schema has drifted.
- **FluentValidation.AspNetCore deprecation**: The package is no longer maintained after v11.3.1. The validation pipeline in this project is handled by the MediatR `ValidationBehavior`, so `FluentValidation.AspNetCore` could be removed entirely. Evaluate and remove in a follow-up pass.
- **`Domain` project still targets `netstandard2.1`**: Functionally correct (net10.0 can consume netstandard2.1 libraries). Can be changed to `net10.0` in a future cleanup pass if needed.
