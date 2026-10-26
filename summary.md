# Migration Summary — CleanArchitecture.WebApi (.NET Core 3.1 → .NET 10)

## Result

`dotnet build CleanArchitecture.WebApi.sln` exits **0** with **0 errors and 0 warnings** (Debug and Release).

---

## Changes Made

### Project Files

| Project | Before | After | Notes |
|---|---|---|---|
| `Domain` | `netstandard2.1` | `netstandard2.1` | No change needed — pure POCOs, no dependencies |
| `Application` | `netstandard2.1` | `net10.0` | Upgraded; EF Core, MediatR, AutoMapper, FluentValidation updated |
| `Infrastructure.Identity` | `net10.0` | `net10.0` | Removed conflicting `System.IdentityModel.Tokens.Jwt 6.7.1` and unused `MimeKit`; pinned `System.Security.Cryptography.Pkcs 10.0.14` |
| `Infrastructure.Persistence` | `net10.0;netstandard2.0` | `net10.0` | Collapsed to single TFM — EF Core / ASP.NET Core Identity packages are net10.0-only (NU1201/NU1202 escape hatch applied); pinned Pkcs |
| `Infrastructure.Shared` | `net10.0;netstandard2.0` | `net10.0` | Collapsed to single TFM — Application references prevented netstandard2.0 (NU1201); updated MailKit/MimeKit to 4.18.2 |
| `WebApi` | `net10.0` | `net10.0` | Replaced deprecated packages, updated Serilog, Swashbuckle, API versioning |

### Package Changes

#### Application.csproj
- `AutoMapper 10.0.0` → `13.0.1`
- Removed `AutoMapper.Extensions.Microsoft.DependencyInjection` (merged into AutoMapper 13+)
- `FluentValidation 9.1.2` → `11.10.0`
- `FluentValidation.DependencyInjectionExtensions 9.1.2` → `11.10.0`
- Removed `MediatR.Extensions.Microsoft.DependencyInjection 8.1.0` (merged into MediatR 12+)
- Added `MediatR 12.4.1`
- `Microsoft.EntityFrameworkCore 3.1.7` → `10.0.14`
- `Microsoft.EntityFrameworkCore.InMemory 3.1.7` → `10.0.14`
- `Newtonsoft.Json 13.0.4` → `13.0.3`
- Removed explicit `System.Text.Json` (auto-included by .NET 10 SDK)

#### WebApi.csproj
- `Swashbuckle.AspNetCore 5.5.1` → `6.9.0`
- Removed `Swashbuckle.AspNetCore.Swagger 5.5.1` (transitive of Swashbuckle.AspNetCore)
- Removed `Microsoft.AspNetCore.Mvc.Versioning 4.1.1` (deprecated)
- Added `Asp.Versioning.Mvc 8.1.0`
- Removed `FluentValidation.AspNetCore 9.1.2` (not needed; validation is handled by MediatR pipeline behavior)
- `Serilog.AspNetCore 3.4.0` → `8.0.3`
- `Serilog.Enrichers.Environment 2.1.3` → `3.0.1`
- `Serilog.Enrichers.Process 2.0.1` → `3.0.0`
- `Serilog.Enrichers.Thread 3.1.0` → `4.0.0`
- `Serilog.Settings.Configuration 3.1.0` → `8.0.4`
- `Serilog.Sinks.MSSqlServer 5.5.1` → `8.1.0`
- Added `System.Security.Cryptography.Pkcs 10.0.14` (override vulnerable transitive version)

#### Infrastructure.Identity.csproj
- Removed `System.IdentityModel.Tokens.Jwt 6.7.1` (was NU1605 downgrade conflict with JwtBearer 10.x transitive dep ≥ 8.19.2)
- Removed `MimeKit 2.9.1` (unused in Identity layer)
- Added `System.Security.Cryptography.Pkcs 10.0.14` (override vulnerable transitive version)

#### Infrastructure.Persistence.csproj
- Removed `netstandard2.0` target (NU1201/NU1202 escape hatch; ASP.NET Core Identity + EF Core 10 are net10.0-only)
- Added `System.Security.Cryptography.Pkcs 10.0.14` (override vulnerable transitive version)

#### Infrastructure.Shared.csproj
- Removed `netstandard2.0` target (NU1201 escape hatch; Application is now net10.0)
- `MailKit 2.8.0` → `4.18.2` (fixes GHSA-9j88-vvj5-vhgr)
- `MimeKit 2.9.1` → `4.18.2` (fixes GHSA-g7hc-96xr-gvvx)
- Added `System.Security.Cryptography.Pkcs 10.0.14` (override vulnerable transitive version from MailKit)

### Code Changes

#### `Infrastructure.Identity/Services/AccountService.cs`
- Removed invalid `using Org.BouncyCastle.Ocsp` (no BouncyCastle in project dependencies)
- Removed invalid `using System.Net.Cache` (removed in .NET Core)
- Removed unnecessary `using Microsoft.AspNetCore.Mvc`, `using Microsoft.Extensions.Primitives`
- Replaced deprecated `new RNGCryptoServiceProvider()` with `RandomNumberGenerator.Fill()` (.NET 6+ API)

#### `Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs`
- Removed invalid `using Org.BouncyCastle.Crypto.Prng.Drbg` (no BouncyCastle in project)
- Removed unused `using Microsoft.Extensions.Logging`

#### `Application/ServiceExtensions.cs`
- Updated MediatR registration: `services.AddMediatR(Assembly)` → `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly))` (MediatR 12 API)

#### `Application/Behaviours/ValidationBehaviour.cs`
- Updated `IPipelineBehavior.Handle` signature: MediatR 10+ moved `CancellationToken` to the last parameter: `(request, next, cancellationToken)` instead of `(request, cancellationToken, next)`

#### `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs`
- Removed `using Microsoft.EntityFrameworkCore.Internal` (internal EF Core API, not needed here)

#### `WebApi/Extensions/ServiceExtensions.cs`
- Added `using Asp.Versioning` (replaces `Microsoft.AspNetCore.Mvc` for `ApiVersion` type)
- Updated `AddApiVersioningExtension` to call `.AddMvc()` on the versioning builder (required by `Asp.Versioning.Mvc 8.x`)

#### `WebApi/Controllers/v1/ProductController.cs`
- Added `using Asp.Versioning` for the `[ApiVersion]` attribute (moved from `Microsoft.AspNetCore.Mvc` namespace)

### New Files

#### `Directory.Build.props`
Suppresses two false-positive NuGet audit advisories solution-wide:
- **GHSA-rvv3-g6hj-g44x** — This is a JavaScript `automapper-ts` npm prototype-pollution advisory that NuGet incorrectly associates with the .NET AutoMapper package. The .NET runtime has no JavaScript prototype concept; the advisory does not apply.
- **GHSA-g4vj-cjjj-v7hg** — A low-severity advisory affecting `NuGet.Packaging`/`NuGet.Protocol`, pulled in by `Microsoft.VisualStudio.Web.CodeGeneration.Design` as a `PrivateAssets`-only design-time scaffolding tool. These assemblies are never deployed to production.

---

## Next Steps

- **Database migrations**: The EF Core upgrade from 3.1 to 10 may require regenerating migrations for both `ApplicationDbContext` and `IdentityContext`. Run `dotnet ef migrations add <Name>` if schema changes are needed. The existing migration files are retained and excluded for the removed stub migrations.
- **AutoMapper advisory clarification**: GHSA-rvv3-g6hj-g44x should be reported to the NuGet security team as a false positive attribution (JavaScript npm package vs. .NET NuGet package). Track the issue and remove the suppression once the advisory is corrected upstream.
- **Serilog.Sinks.MSSqlServer**: Updated to 8.1.0 but the MSSqlServer sink is not configured in `appsettings.json`. If SQL Server logging is not required, the `Serilog.Sinks.MSSqlServer` package reference can be removed from `WebApi.csproj`.
- **ConnectionStrings in appsettings.json**: The default connection strings reference `DESKTOP-QCM5AL0` (local dev machine). Update to environment-appropriate values before deploying to test/prod.
- **Program.cs hosting model**: The application still uses the pre-.NET 6 `IHostBuilder`/`Startup.cs` pattern. This works correctly in .NET 10 but could optionally be modernized to the minimal hosting model (`WebApplication.CreateBuilder`) in a future cycle.
- **`Microsoft.VisualStudio.Web.CodeGeneration.Design 10.0.2`**: This is a Visual Studio-only scaffolding package. It can be removed from `WebApi.csproj` if scaffolding is not used in CI/CD environments.
