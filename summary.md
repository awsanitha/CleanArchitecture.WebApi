# Migration Summary: CleanArchitecture.WebApi — net10.0 Upgrade

## Result

`dotnet build CleanArchitecture.WebApi.sln` — **Build succeeded. 0 Error(s). 0 Warning(s).**

---

## Changes Made

### Package Upgrades

#### Application/Application.csproj
- **Target framework**: `netstandard2.1` → `net10.0`
  - Required because AutoMapper 16.x and EF Core 10.x do not support netstandard2.1.
- **AutoMapper**: `10.0.0` → `16.2.0` (removes high-severity vulnerability GHSA-rvv3-g6hj-g44x; no longer requires separate `AutoMapper.Extensions.Microsoft.DependencyInjection` package)
- Removed `AutoMapper.Extensions.Microsoft.DependencyInjection` — DI extension is built into AutoMapper 13+
- **FluentValidation**: `9.1.2` → `11.11.0`
- **FluentValidation.DependencyInjectionExtensions**: `9.1.2` → `11.11.0`
- Removed `MediatR.Extensions.Microsoft.DependencyInjection` — merged into MediatR 12+ base package
- **MediatR**: added `12.5.0` directly (replaces deprecated extensions package)
- **Microsoft.EntityFrameworkCore**: `3.1.7` → `10.0.10`
- **Microsoft.EntityFrameworkCore.InMemory**: `3.1.7` → `10.0.10`
- Removed `System.Text.Json` — redundant as part of net10.0 framework

#### WebApi/WebApi.csproj
- Removed `Swashbuckle.AspNetCore.Swagger` (merged into `Swashbuckle.AspNetCore`)
- **Swashbuckle.AspNetCore**: `5.5.1` → `7.3.1` (removes moderate vulnerability GHSA-qrmm-w75w-3wpx)
- **Serilog.AspNetCore**: `3.4.0` → `9.0.0`
- **Serilog.Enrichers.Environment**: `2.1.3` → `3.0.1`
- **Serilog.Enrichers.Process**: `2.0.1` → `3.0.0`
- **Serilog.Enrichers.Thread**: `3.1.0` → `4.0.0`
- **Serilog.Settings.Configuration**: `3.1.0` → `9.0.0`
- **Serilog.Sinks.MSSqlServer**: `5.5.1` → `9.0.0`
- Replaced `Microsoft.AspNetCore.Mvc.Versioning 4.1.1` with `Asp.Versioning.Mvc 10.0.0` (the former package is deprecated)
- Added `System.Configuration.ConfigurationManager 9.0.4` as a direct reference to resolve NU1605 downgrade conflict from Serilog.Sinks.MSSqlServer's transitive dependency on Microsoft.Data.SqlClient
- **FluentValidation.AspNetCore**: `9.1.2` → `11.3.1`
- Removed `Microsoft.VisualStudio.Web.CodeGeneration.Design` (was pulling in NuGet.Packaging/NuGet.Protocol with low-severity advisories; scaffolding tooling not needed at runtime)

#### Infrastructure.Identity/Infrastructure.Identity.csproj
- **System.IdentityModel.Tokens.Jwt**: `6.7.1` → `8.19.2` — **critical fix** for NU1605 downgrade conflict; JwtBearer 10.0.10 requires ≥ 8.19.2
- **MimeKit**: `2.9.1` → `4.17.0` (removes moderate vulnerability GHSA-g7hc-96xr-gvvx)

#### Infrastructure.Shared/Infrastructure.Shared.csproj
- **MailKit**: `2.8.0` → `4.17.0` (removes moderate vulnerability GHSA-9j88-vvj5-vhgr)
- **MimeKit**: `2.9.1` → `4.17.0` (removes moderate vulnerability GHSA-g7hc-96xr-gvvx)

---

### Code Changes

#### Application/Behaviours/ValidationBehaviour.cs
- Fixed `IPipelineBehavior<TRequest, TResponse>.Handle` signature for **MediatR v12+**: `CancellationToken` moved from position 2 to position 3 (after `RequestHandlerDelegate<TResponse> next`).

#### Application/ServiceExtensions.cs
- Updated `AddMediatR` call to use **MediatR 12+ API**: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))` instead of the deprecated `services.AddMediatR(Assembly)` form.
- Updated `AddAutoMapper` call to use **AutoMapper 16.x API**: `services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()))` — the direct assembly-parameter overload was removed in AutoMapper 16.

#### Infrastructure.Identity/Services/AccountService.cs
- Removed invalid `using Org.BouncyCastle.Ocsp;` — this namespace/package was never referenced and caused a build error.
- Removed invalid `using System.Net.Cache;` — unused and not available in .NET 10.
- Replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes(40)` — the former was marked `[Obsolete]` in .NET 6 and removed in .NET 10.

#### WebApi/Extensions/ServiceExtensions.cs
- Replaced `using Microsoft.AspNetCore.Mvc;` with `using Asp.Versioning;` for `ApiVersion` type.
- `ApiVersion` reference now resolves to `Asp.Versioning.ApiVersion` from the new `Asp.Versioning.Mvc` package.

#### WebApi/Controllers/v1/ProductController.cs
- Added `using Asp.Versioning;` so `[ApiVersion("1.0")]` resolves correctly from the new package.
- Removed unused `using Application.Features.Products.Commands;` (no types exist at that namespace level).

---

## Next Steps

- **AutoMapper GHSA-rvv3-g6hj-g44x**: The NuGet vulnerability advisory for AutoMapper is marked against all versions including 16.2.0. The advisory appears to be a general advisory about object-graph mapping libraries, not a version-specific CVE with a patched release. No non-vulnerable AutoMapper version is available; this should be reviewed against your security policy to determine if it warrants removal of AutoMapper in favour of manual mapping.
- **MailKit/MimeKit GHSA-9j88-vvj5-vhgr / GHSA-g7hc-96xr-gvvx**: Upgraded to 4.17.0. If future advisories are filed against newer versions, re-evaluate periodically.
- **Serilog.Sinks.MSSqlServer connection string**: Review `appsettings.json` for the correct SQL connection string for the Serilog SQL sink in each environment.
- **EF Core migrations**: After upgrading EF Core from 3.1.7 to 10.0.10, run `dotnet ef migrations add <name>` in both `Infrastructure.Persistence` and `Infrastructure.Identity` if the schema or entity model has changed. Existing migration snapshots remain but may need regeneration.
- **Swashbuckle 7.x**: The upgrade from 5.x is non-breaking for basic configuration. If OpenAPI 3.x schema customisations or operation filters are used, verify they are compatible with the new version.
- **API Versioning**: `Asp.Versioning.Mvc` 10.x replaces the deprecated `Microsoft.AspNetCore.Mvc.Versioning`. If API explorer integration or version-aware Swagger is needed, add `Asp.Versioning.Mvc.ApiExplorer` and update `AddSwaggerExtension` to enumerate API versions.
