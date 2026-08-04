# Migration Summary: CleanArchitecture.WebApi — .NET 3.1 → net10.0

## Final Build Status
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Changes Made

### 1. Package Upgrades — Critical Build Errors Fixed

**Infrastructure.Identity.csproj**
- `System.IdentityModel.Tokens.Jwt` 6.7.1 → 8.19.2  
  **Root cause:** `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.10` requires JWT 8.19.2 via its transitive chain, but the project pinned 6.7.1 causing `NU1605` (package downgrade) which was treated as an error.
- Removed `MimeKit 2.9.1` (vulnerable, not needed in this project)
- Fixed package casing: `microsoft.extensions.dependencyinjection` → `Microsoft.Extensions.DependencyInjection`

### 2. Application.csproj — Target Framework & Package Updates
- **TargetFramework**: `netstandard2.1` → `net8.0`  
  **Root cause:** AutoMapper 13+ and EF Core 8+ dropped `netstandard` support. The library must target `net8.0` to consume these packages. The solution is net10.0-hosted so `net8.0` libraries are fully compatible.
- `AutoMapper` 10.0.0 → 16.2.0 (latest; resolves `GHSA-rvv3-g6hj-g44x` high-severity vulnerability)
- Removed `AutoMapper.Extensions.Microsoft.DependencyInjection` (DI extensions are now built into AutoMapper 12+)
- `MediatR.Extensions.Microsoft.DependencyInjection` 8.1.0 → removed (obsolete in MediatR 12+; DI registration is built into `MediatR`)
- `MediatR` 8.x → `MediatR 12.5.0`
- `Microsoft.EntityFrameworkCore` 3.1.7 → 9.0.7
- `Microsoft.EntityFrameworkCore.InMemory` 3.1.7 → 9.0.7
- `FluentValidation` 9.x → 11.11.0
- `FluentValidation.DependencyInjectionExtensions` 9.x → 11.11.0
- `System.Text.Json` → 9.0.7
- `Newtonsoft.Json` → 13.0.3

### 3. WebApi.csproj — Package Updates
- `Swashbuckle.AspNetCore` 5.5.1 → 6.9.0 (fixes `GHSA-qrmm-w75w-3wpx` moderate vulnerability in SwaggerUI)
- `Microsoft.AspNetCore.Mvc.Versioning` 4.1.1 → replaced with `Asp.Versioning.Mvc` 8.1.0 (the original package was abandoned; Asp.Versioning.Mvc is the official successor)
- `Serilog.AspNetCore` 3.4.0 → 9.0.0
- `Serilog.Enrichers.Environment` 2.1.3 → 3.0.1
- `Serilog.Enrichers.Process` 2.0.1 → 3.0.0
- `Serilog.Enrichers.Thread` 3.1.0 → 4.0.0
- `Serilog.Settings.Configuration` 3.1.0 → 9.0.0
- `Serilog.Sinks.MSSqlServer` 5.5.1 → 9.0.0
- `Microsoft.VisualStudio.Web.CodeGeneration.Design`: marked `PrivateAssets=all`
- Pinned `NuGet.Packaging` and `NuGet.Protocol` to 6.13.2 (fixes `NU1901` low-severity warnings from CodeGeneration.Design transitive deps)

### 4. Infrastructure.Shared.csproj
- `MailKit` 2.8.0 → 4.17.0 (fixes `GHSA-9j88-vvj5-vhgr` moderate vulnerability)
- `MimeKit` 2.9.1 → 4.17.0 (fixes `GHSA-g7hc-96xr-gvvx` moderate vulnerability)

### 5. Code Changes

**Application/ServiceExtensions.cs**
- Updated `services.AddMediatR(Assembly.GetExecutingAssembly())` → `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` (MediatR 12 breaking change)
- Updated `services.AddAutoMapper(Assembly.GetExecutingAssembly())` → `services.AddAutoMapper(cfg => cfg.AddMaps(...))` (AutoMapper 16 breaking change)

**Application/Behaviours/ValidationBehaviour.cs**
- Updated `IPipelineBehavior<TRequest,TResponse>.Handle` signature: in MediatR 12, `RequestHandlerDelegate<TResponse> next` moved before `CancellationToken cancellationToken`

**WebApi/Extensions/ServiceExtensions.cs**
- Added `using Asp.Versioning;` for `ApiVersion` type (new package namespace)

**WebApi/Controllers/v1/ProductController.cs**
- Added `using Asp.Versioning;` for `[ApiVersion("1.0")]` attribute

**Infrastructure.Identity/Services/AccountService.cs**
- Removed invalid `using Org.BouncyCastle.Ocsp;` (BouncyCastle not referenced)
- Removed `using System.Net.Cache;` (not available on .NET Core)
- Removed unused `using Microsoft.AspNetCore.Mvc;`, `using Microsoft.Extensions.Primitives;`
- Replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.Fill()` (.NET 5+ API)

**Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs**
- Removed invalid `using Org.BouncyCastle.Crypto.Prng.Drbg;`
- Removed unused `using Microsoft.Extensions.Logging;`

## Next Steps

- **Application target framework**: Application.csproj was changed from `netstandard2.1` to `net8.0` because AutoMapper 16+ and EF Core 8+ dropped netstandard support. If multi-targeting is needed (e.g., for sharing the Application layer with other net4x projects), this would need additional consideration.
- **EF Core migrations**: The EF Core packages were upgraded from 3.1.7 to 9.0.7. Existing migrations in `Infrastructure.Persistence/Migrations` and `Infrastructure.Identity/Migrations` should be validated against the new EF Core version. Re-generating migrations is recommended for production deployments.
- **MediatR behavior ordering**: The `ValidationBehavior` registration order relative to other pipeline behaviors should be reviewed if additional behaviors are added.
- **Serilog.Sinks.MSSqlServer 9.0.0**: Verify MSSqlServer sink configuration compatibility — v9 may have configuration API changes relative to v5.
- **FluentValidation.AspNetCore 11.3.0**: In FluentValidation 11, automatic validation was removed from ASP.NET Core. If automatic model validation was relied upon, manual validation or the `AddFluentValidationAutoValidation()` extension must be explicitly added in `Startup.cs`.
