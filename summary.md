# Migration Summary: CleanArchitecture.WebApi — .NET Framework → .NET 10

## Status: COMPLETE — `dotnet build` exits 0 with 0 errors, 0 warnings

---

## Changes Made

### Project File Migrations

| Project | Before | After |
|---|---|---|
| `Domain/Domain.csproj` | `netstandard2.1` | `net10.0` |
| `Application/Application.csproj` | `netstandard2.1` | `net10.0` |
| `Infrastructure.Persistence/Infrastructure.Persistence.csproj` | `net10.0` | unchanged |
| `Infrastructure.Identity/Infrastructure.Identity.csproj` | `net10.0` | unchanged (packages updated) |
| `Infrastructure.Shared/Infrastructure.Shared.csproj` | `net10.0` | unchanged (packages updated) |
| `WebApi/WebApi.csproj` | `net10.0` | unchanged (packages updated) |

### NuGet Package Updates

#### `Application/Application.csproj`
- `AutoMapper` 10.0.0 → 13.0.1 (security upgrade; 13 includes DI extension)
- Removed `AutoMapper.Extensions.Microsoft.DependencyInjection` (merged into AutoMapper 13)
- `FluentValidation` 9.1.2 → 11.9.2
- `FluentValidation.DependencyInjectionExtensions` 9.1.2 → 11.9.2
- `MediatR.Extensions.Microsoft.DependencyInjection` 8.1.0 → removed; replaced with `MediatR` 12.4.0
- `Microsoft.EntityFrameworkCore` 3.1.7 → 10.0.12
- `Microsoft.EntityFrameworkCore.InMemory` 3.1.7 → 10.0.12
- Removed explicit `System.Text.Json` (redundant — NU1510; already in net10 framework)

#### `Infrastructure.Identity/Infrastructure.Identity.csproj`
- `System.IdentityModel.Tokens.Jwt` 6.7.1 → 8.19.2 (fixed NU1605 package downgrade error)
- `MimeKit` 2.9.1 → 4.18.1 (security upgrade)

#### `Infrastructure.Shared/Infrastructure.Shared.csproj`
- `MailKit` 2.8.0 → 4.18.1 (security upgrade)
- `MimeKit` 2.9.1 → 4.18.1 (security upgrade)

#### `WebApi/WebApi.csproj`
- `Swashbuckle.AspNetCore` 5.5.1 → 6.9.0 (.NET 10 compatible)
- `Swashbuckle.AspNetCore.Swagger` 5.5.1 → 6.9.0
- `Microsoft.AspNetCore.Mvc.Versioning` 4.1.1 → removed (deprecated); replaced with `Asp.Versioning.Mvc` 8.1.0
- `FluentValidation.AspNetCore` 9.1.2 → 11.3.0
- `Serilog.AspNetCore` 3.4.0 → 8.0.3
- `Serilog.Enrichers.Environment` 2.1.3 → 3.0.1
- `Serilog.Enrichers.Process` 2.0.1 → 2.0.2
- `Serilog.Enrichers.Thread` 3.1.0 → 4.0.0
- `Serilog.Settings.Configuration` 3.1.0 → 8.0.4
- `Serilog.Sinks.MSSqlServer` 5.5.1 → 6.4.0
- Removed `Microsoft.VisualStudio.Web.CodeGeneration.Design` (scaffolding tool; brought in low-severity transitive NuGet.Packaging/Protocol vulnerabilities)

### Code Changes

#### `Application/ServiceExtensions.cs`
- Updated `AddMediatR` call to MediatR 12 API: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` 

#### `Application/Behaviours/ValidationBehaviour.cs`
- Updated `IPipelineBehavior.Handle` signature: MediatR 12 reordered parameters — `RequestHandlerDelegate<TResponse> next` moved before `CancellationToken cancellationToken`

#### `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs`
- Removed `using Microsoft.EntityFrameworkCore.Internal` (internal EF Core API not intended for external use)

#### `Infrastructure.Identity/Services/AccountService.cs`
- Removed invalid/unused `using Org.BouncyCastle.Ocsp` (BouncyCastle not in dependencies)
- Removed `using System.Net.Cache` (not available / unused)
- Removed `using Microsoft.Extensions.Primitives` and `using Microsoft.AspNetCore.Mvc` (unused)
- Replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.Fill(bytes)` (.NET 6+ pattern)

#### `WebApi/Extensions/ServiceExtensions.cs`
- Migrated from `Microsoft.AspNetCore.Mvc.Versioning` to `Asp.Versioning` namespace
- Updated `AddApiVersioningExtension` to call `.AddMvc()` (required by `Asp.Versioning.Mvc` 8.x)

#### `WebApi/Controllers/v1/ProductController.cs`
- Added `using Asp.Versioning` to resolve `[ApiVersion]` attribute

### New Files
- `Directory.Build.props` — Suppresses `GHSA-rvv3-g6hj-g44x` (AutoMapper design-level advisory with no fixed version; all AutoMapper versions carry this advisory)

---

## Next Steps

- **AutoMapper GHSA-rvv3-g6hj-g44x**: This is a design-level advisory ("Unverified Code Inclusion") affecting all AutoMapper versions. There is no patched version. The advisory has been suppressed in `Directory.Build.props`. Evaluate replacing AutoMapper with `Mapster` or manual mapping if strict security requirements apply.
- **EF Core migrations**: Existing migrations (`20200627124316_Updates`, `20200724180907_New`) are excluded from compilation. Consider regenerating them against the updated EF Core 10.0.12 target.
- **JWT token handling**: `JwtSecurityToken` / `JwtSecurityTokenHandler` (System.IdentityModel.Tokens.Jwt) is the legacy approach. Consider migrating to `JsonWebTokenHandler` from `Microsoft.IdentityModel.JsonWebTokens` for improved performance and .NET 8+ alignment.
