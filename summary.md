# Migration Summary: CleanArchitecture.WebApi — .NET Core 3.1 → .NET 10

## Status

`dotnet build CleanArchitecture.WebApi.sln` — **SUCCEEDED with 0 errors, 22 warnings (all NuGet security advisories, no compilation warnings)**

---

## Changes Made

### 1. Target Framework Upgrades

| Project | Before | After |
|---|---|---|
| `Domain` | `netstandard2.1` | `net10.0` |
| `Application` | `netstandard2.1` | `net10.0` |
| `Infrastructure.Identity` | `net10.0` | `net10.0` (unchanged) |
| `Infrastructure.Persistence` | `net10.0` | `net10.0` (unchanged) |
| `Infrastructure.Shared` | `net10.0` | `net10.0` (unchanged) |
| `WebApi` | `net10.0` | `net10.0` (unchanged) |

Rationale: `Domain` and `Application` were targeting `netstandard2.1`, but `System.ComponentModel.DataAnnotations` was not auto-resolved by the .NET 10 SDK without a transitive framework reference. Since all consumers target `net10.0`, retargeting to `net10.0` directly is the correct approach per KB guidance.

---

### 2. Package Upgrades

#### `Infrastructure.Identity/Infrastructure.Identity.csproj`
- `System.IdentityModel.Tokens.Jwt 6.7.1` → `8.0.1` — Fixed NU1605 downgrade conflict with `JwtBearer 10.0.9` which requires `>= 8.0.1`
- Removed `MimeKit 2.9.1` — not needed in Identity project

#### `Application/Application.csproj`
- `AutoMapper 10.0.0` → `12.0.1`
- `AutoMapper.Extensions.Microsoft.DependencyInjection 8.0.1` → `12.0.1`
- `FluentValidation 9.1.2` → `11.11.0`
- `FluentValidation.DependencyInjectionExtensions 9.1.2` → `11.11.0`
- `MediatR.Extensions.Microsoft.DependencyInjection 8.1.0` → Removed; replaced by `MediatR 12.0.1` (DI support merged into core package in v12)
- `Microsoft.EntityFrameworkCore 3.1.7` → Removed (Application layer has no EF Core dependencies; EF Core 8+ doesn't support `netstandard2.1`)
- `Microsoft.EntityFrameworkCore.InMemory 3.1.7` → Removed (same reason)
- `System.Text.Json` → Removed (redundant, included in `net10.0` BCL)

#### `WebApi/WebApi.csproj`
- `Swashbuckle.AspNetCore 5.5.1` → `6.9.0`
- `Swashbuckle.AspNetCore.Swagger 5.5.1` → Removed (bundled in `Swashbuckle.AspNetCore 6.x`)
- `Serilog.AspNetCore 3.4.0` → `8.0.3`
- `Serilog.Enrichers.Environment 2.1.3` → `3.0.1`
- `Serilog.Enrichers.Process 2.0.1` → `3.0.0`
- `Serilog.Enrichers.Thread 3.1.0` → `4.0.0`
- `Serilog.Settings.Configuration 3.1.0` → `8.0.4`
- `Serilog.Sinks.MSSqlServer 5.5.1` → `8.1.0`
- `Microsoft.AspNetCore.Mvc.Versioning 4.1.1` → Removed; replaced by `Asp.Versioning.Mvc 8.1.0` (official migration of the deprecated package)
- `FluentValidation.AspNetCore 9.1.2` → `11.3.0`

#### `Infrastructure.Shared/Infrastructure.Shared.csproj`
- `MailKit 2.8.0` → `4.12.0`
- `MimeKit 2.9.1` → `4.12.0`
- Added `Microsoft.Extensions.Logging.Abstractions 10.0.9` — required for `ILogger<T>` in non-Web SDK project

---

### 3. Source Code Changes

#### `Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs`
- Removed `using Org.BouncyCastle.Crypto.Prng.Drbg;` — BouncyCastle is not a dependency of this project

#### `Infrastructure.Identity/Services/AccountService.cs`
- Removed `using Org.BouncyCastle.Ocsp;` — invalid dependency
- Removed `using System.Net.Cache;` — namespace does not exist in .NET Core
- Removed unused `using Microsoft.Extensions.Primitives;` and `using Microsoft.AspNetCore.Mvc;`
- Replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.Fill(randomBytes)` (modern .NET 6+ API)

#### `Application/ServiceExtensions.cs`
- Updated `services.AddMediatR(Assembly.GetExecutingAssembly())` → `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))` (MediatR 12 API)
- Removed unused `using Application.Features.Products.Commands.CreateProduct;`

#### `Application/Behaviours/ValidationBehaviour.cs`
- Fixed `IPipelineBehavior.Handle` signature: MediatR 12 swapped `next` and `cancellationToken` parameters
  - Before: `Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)`
  - After: `Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)`

#### `WebApi/Extensions/ServiceExtensions.cs`
- Added `using Asp.Versioning;` replacing implicit `Microsoft.AspNetCore.Mvc` for `ApiVersion` type
- Added `.AddMvc()` chain after `AddApiVersioning()` (required for `Asp.Versioning.Mvc 8.x`)

#### `WebApi/Controllers/v1/ProductController.cs`
- Added `using Asp.Versioning;` for `[ApiVersion("1.0")]` attribute

#### `Application/Interfaces/IAccountService.cs`
- Removed unused `using Microsoft.Extensions.Primitives;`

#### `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs`
- Removed `using Microsoft.EntityFrameworkCore.Internal;` — internal API, inaccessible in external assemblies in EF Core 5+

---

## Remaining Warnings (Non-Blocking)

These are NuGet security audit warnings, not compilation errors:

| Package | Severity | Advisory |
|---|---|---|
| `AutoMapper 12.0.1` | High | GHSA-rvv3-g6hj-g44x |
| `MailKit 4.12.0` | Moderate | GHSA-9j88-vvj5-vhgr |
| `MimeKit 4.12.0` | Moderate | GHSA-g7hc-96xr-gvvx |
| `NuGet.Packaging 6.12.1` | Low | GHSA-g4vj-cjjj-v7hg (transitive) |
| `NuGet.Protocol 6.12.1` | Low | GHSA-g4vj-cjjj-v7hg (transitive) |

## Next Steps

1. **AutoMapper vulnerability**: Upgrade `AutoMapper` beyond `12.0.1` to address GHSA-rvv3-g6hj-g44x. AutoMapper 13+ has DI support built-in — remove `AutoMapper.Extensions.Microsoft.DependencyInjection` and verify `services.AddAutoMapper()` is still available. Test compilation and mapping behavior.

2. **MailKit/MimeKit**: Upgrade to latest stable (`4.17.0+`) to clear the moderate vulnerability warnings. Verify `EmailService.cs` still compiles (the sync `Connect`/`Authenticate` methods have been stable across versions but worth verifying).

3. **NuGet.Packaging/Protocol**: These are transitive dependencies from `Microsoft.VisualStudio.Web.CodeGeneration.Design`. Consider removing that package from `WebApi.csproj` if scaffolding is not needed in production; it is a design-time-only dependency.

4. **EF Core migrations**: The existing migration snapshots in `Infrastructure.Identity/Migrations` and `Infrastructure.Persistence/Migrations` were generated with EF Core 3.x. Run `dotnet ef migrations add <name>` after connecting to a real database to create fresh migrations against the `net10.0` target.

5. **`UseInMemoryDatabase`**: Currently set to `false` in `appsettings.json`. For local development without SQL Server, set `"UseInMemoryDatabase": true`.
