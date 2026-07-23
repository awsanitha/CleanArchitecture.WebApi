# Migration Summary: CleanArchitecture.WebApi → net10.0

## Status: ✅ Build SUCCESSFUL — 0 errors, 0 warnings

---

## Changes Made

### Project File Changes

#### `Infrastructure.Identity/Infrastructure.Identity.csproj`
- **Removed** explicit `System.IdentityModel.Tokens.Jwt 6.7.1` reference
  - This was causing NU1605 (Warning As Error) downgrade conflict — `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.10` transitively requires `>= 8.19.2`; the explicit pin to 6.7.1 blocked resolution.
  - With the pin removed, NuGet resolves to 8.19.2+ as required by JwtBearer.
- **Upgraded** `MimeKit 2.9.1` → `4.17.0` (fixes NU1902 moderate vulnerability)

#### `Infrastructure.Shared/Infrastructure.Shared.csproj`
- **Upgraded** `MailKit 2.8.0` → `4.17.0` (fixes NU1902 moderate vulnerability)
- **Upgraded** `MimeKit 2.9.1` → `4.17.0` (fixes NU1902 moderate vulnerability)

#### `Application/Application.csproj`
- **Changed** `TargetFramework` from `netstandard2.1` → `net10.0`
  - Required because `Microsoft.EntityFrameworkCore 8+` dropped netstandard2.1 support.
- **Removed** `MediatR.Extensions.Microsoft.DependencyInjection 8.1.0`
  - Superseded by MediatR's built-in DI from v11+.
- **Added** `MediatR 12.4.1` (DI built-in; `RegisterServicesFromAssembly` API)
- **Upgraded** `AutoMapper 10.0.0` → `16.2.0`
  - Fixes NU1903 high severity vulnerability GHSA-rvv3-g6hj-g44x (DoS via uncontrolled recursion). Patched in 16.1.1.
  - Note: AutoMapper 15+ removed the `params Assembly[]` DI overload; 16.x DI registration now uses `cfg.AddMaps(Assembly)` pattern (see code changes below).
- **Removed** `AutoMapper.Extensions.Microsoft.DependencyInjection 8.0.1`
  - DI integration was merged into AutoMapper core from v13.0+.
- **Upgraded** `FluentValidation 9.1.2` → `11.11.0`
- **Upgraded** `FluentValidation.DependencyInjectionExtensions 9.1.2` → `11.11.0`
- **Upgraded** `Microsoft.EntityFrameworkCore 3.1.7` → `10.0.10`
- **Upgraded** `Microsoft.EntityFrameworkCore.InMemory 3.1.7` → `10.0.10`
- **Removed** `System.Text.Json 10.0.10` — NU1510 warned this package is part of the .NET 10 runtime and should not be explicitly referenced.

#### `WebApi/WebApi.csproj`
- **Removed** `Microsoft.AspNetCore.Mvc.Versioning 4.1.1` (deprecated for .NET 6+)
- **Added** `Asp.Versioning.Mvc 8.1.0` (the modern API versioning package by the same team)
- **Removed** `Swashbuckle.AspNetCore.Swagger 5.5.1` (already included in the main package)
- **Upgraded** `Swashbuckle.AspNetCore 5.5.1` → `7.0.0` (fixes NU1902 moderate vulnerability GHSA-qrmm-w75w-3wpx)
- **Upgraded** `Serilog.AspNetCore 3.4.0` → `9.0.0`
- **Upgraded** `Serilog.Settings.Configuration 3.1.0` → `9.0.0` (matches Serilog.AspNetCore 9.0.0 transitive requirement)
- **Upgraded** `Serilog.Sinks.MSSqlServer 5.5.1` → `9.0.0`
- **Upgraded** `Serilog.Enrichers.Environment 2.1.3` → `3.0.0`
- **Upgraded** `Serilog.Enrichers.Process 2.0.1` → `3.0.0`
- **Upgraded** `Serilog.Enrichers.Thread 3.1.0` → `4.0.0`
- **Upgraded** `FluentValidation.AspNetCore 9.1.2` → `11.3.1`
- **Removed** `Microsoft.VisualStudio.Web.CodeGeneration.Design 10.0.2`
  - Dev-time scaffolding tool; brought in `NuGet.Packaging 6.12.1` and `NuGet.Protocol 6.12.1` with known low severity vulnerabilities (GHSA-g4vj-cjjj-v7hg). Not needed for build/runtime.
- **Added** `System.Configuration.ConfigurationManager 9.0.4` as a direct pin
  - Resolves NU1605: `Serilog.Sinks.MSSqlServer 9.0.0` declared `>= 8.0.1` directly but its transitive chain via `Microsoft.Data.SqlClient 6.1.1` required `>= 9.0.4`. Pinning 9.0.4 satisfies both.

---

### Source Code Changes

#### `Infrastructure.Identity/ServiceExtensions.cs`
- **Removed** unused `using System.Reflection.Metadata.Ecma335;` directive.

#### `Infrastructure.Identity/Services/AccountService.cs`
- **Removed** invalid using directives not available in .NET Core / .NET 10:
  - `using Org.BouncyCastle.Ocsp;` — BouncyCastle not referenced in any project; this import was stale.
  - `using System.Net.Cache;` — `WebRequestCachePolicy` etc. are .NET Framework-only and don't exist in .NET Core.
  - `using Microsoft.AspNetCore.Mvc;` — not appropriate in an infrastructure service.
  - `using Microsoft.Extensions.Primitives;` — unused import.
- **Replaced** `RNGCryptoServiceProvider` (deprecated, SYSLIB0023) in `RandomTokenString()` with `RandomNumberGenerator.GetBytes()` static API (available since .NET 6).

#### `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs`
- **Removed** `using Microsoft.EntityFrameworkCore.Internal;`
  - This internal EF Core namespace is not accessible from external assemblies in EF Core 8+ and was unused in this file.

#### `Application/Behaviours/ValidationBehaviour.cs`
- **Updated** `IPipelineBehavior.Handle` method signature for MediatR 12+:
  - Old (MediatR 8.x): `Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)`
  - New (MediatR 12+): `Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)`
  - `CancellationToken` moved from parameter 2 to parameter 3.

#### `Application/ServiceExtensions.cs`
- **Updated** `AddMediatR` call for MediatR 12+ API:
  - Old (MediatR 8.x): `services.AddMediatR(Assembly.GetExecutingAssembly())`
  - New (MediatR 12+): `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))`
- **Updated** `AddAutoMapper` call for AutoMapper 16.x API:
  - AutoMapper 15+ removed the `params Assembly[]` DI overload.
  - Old: `services.AddAutoMapper(Assembly.GetExecutingAssembly())`
  - New: `services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()))`

#### `WebApi/Extensions/ServiceExtensions.cs`
- **Added** `using Asp.Versioning;` directive
  - The `ApiVersion` class moved from `Microsoft.AspNetCore.Mvc` namespace (old `Microsoft.AspNetCore.Mvc.Versioning` package) to `Asp.Versioning` namespace (new `Asp.Versioning.Mvc` package).

#### `WebApi/Controllers/v1/ProductController.cs`
- **Added** `using Asp.Versioning;` directive for the `[ApiVersion]` attribute.

---

## Next Steps

1. **NuGet.Packaging / NuGet.Protocol vulnerabilities**: `NuGet.Packaging 6.12.1` and `NuGet.Protocol 6.12.1` (GHSA-g4vj-cjjj-v7hg, low severity) were previously brought in by `Microsoft.VisualStudio.Web.CodeGeneration.Design`. That package has been removed, eliminating this path. If these packages reappear via other transitive paths in future dependency updates, pin them to their latest patched versions.

2. **EF Core packages in Application layer**: `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.InMemory` references in `Application.csproj` follow clean architecture principles loosely — the Application layer should ideally only reference abstractions, not EF Core. Consider extracting these to `Infrastructure.Persistence.csproj` in a future refactor.

3. **Serilog.Sinks.MSSqlServer**: The `appsettings.json` only configures a Console sink. The MSSqlServer sink package is present but unused at runtime. Consider removing it if MSSqlServer logging is not planned, to reduce dependency surface.

4. **FluentValidation.AspNetCore**: The `WebApi` project references `FluentValidation.AspNetCore 11.3.1` but does not call `AddFluentValidationAutoValidation()` anywhere. Validation is handled via the MediatR pipeline behavior. Consider removing this package in a future cleanup if auto-validation is not needed.

5. **AutoMapper vulnerability context**: GHSA-rvv3-g6hj-g44x is a DoS via self-referential/deeply nested mapping. This application only maps simple, flat Product objects; no self-referential graphs are mapped. The vulnerability is theoretical for this codebase but upgrading to 16.2.0 (fixed in 16.1.1) is the correct long-term position.

6. **EF Core migrations**: The existing migration files were generated with EF Core 3.1.x. After upgrading to EF Core 10.0.x, the migrations will compile but should be regenerated for production to get accurate EF Core 10 annotations. Run `dotnet ef migrations add InitialCreate` after clearing old migrations if the database is being set up fresh.

7. **JWT key configuration**: `appsettings.json` contains a short JWT signing key (`C1CF4B7DC4C4175B6618DE4F55CA4`). For production, use a key of at least 256 bits (32+ characters) and store it in a secure secrets store (Azure Key Vault, AWS Secrets Manager, or user secrets).
