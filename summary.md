# Migration Summary: CleanArchitecture.WebApi → .NET 10

## Result

`dotnet build CleanArchitecture.WebApi.sln` → **Build succeeded. 0 Warning(s). 0 Error(s).**

All 6 projects build cleanly targeting `net10.0`.

---

## Changes Made

### Project Files

| Project | Before | After | Reason |
|---------|--------|-------|--------|
| `Domain` | `netstandard2.1` | `net10.0;netstandard2.0` | Pure domain model, no deps — multi-targeting safe |
| `Application` | `netstandard2.1` | `net10.0` | AutoMapper 16.x dropped `netstandard2.0`; escape hatch applied |
| `Infrastructure.Persistence` | `net10.0;netstandard2.0` | `net10.0` | EF Core 10 does not support `netstandard2.0` (NU1202 escape hatch) |
| `Infrastructure.Identity` | `net10.0` | `net10.0` | No TFM change; package fixes only |
| `Infrastructure.Shared` | `net10.0;netstandard2.0` | `net10.0` | References Application which is `net10.0`-only (escape hatch) |
| `WebApi` | `net10.0` | `net10.0` | No TFM change; package updates only |

### Package Updates

| Package | Old Version | New Version | Notes |
|---------|------------|-------------|-------|
| `AutoMapper` | 10.0.0 | 16.2.0 | Fixes GHSA-rvv3-g6hj-g44x (high severity vulnerability) |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | 8.0.1 | *removed* | Merged into AutoMapper 12+ |
| `MediatR.Extensions.Microsoft.DependencyInjection` | 8.1.0 | *removed* | Merged into MediatR 12+ |
| `MediatR` | *(implicit)* | 12.4.1 | New direct reference with built-in DI |
| `FluentValidation` | 9.1.2 | 11.11.0 | Major version update |
| `FluentValidation.DependencyInjectionExtensions` | 9.1.2 | 11.11.0 | Version aligned with FluentValidation |
| `FluentValidation.AspNetCore` | 9.1.2 | 11.3.0 | Version update |
| `Microsoft.EntityFrameworkCore` (Application) | 3.1.7 | *removed* | Application layer should not reference EF Core directly |
| `Microsoft.EntityFrameworkCore.InMemory` (Application) | 3.1.7 | *removed* | Removed with above |
| `Newtonsoft.Json` | 13.0.4 | 13.0.3 | Pinned to non-vulnerable release (kept existing) |
| `System.IdentityModel.Tokens.Jwt` | 6.7.1 | *removed* | Caused NU1605 downgrade conflict with JwtBearer 10.0.14's transitive dep (8.19.2) |
| `MailKit` | 2.8.0 | 4.18.2 | Fixes GHSA-9j88-vvj5-vhgr (moderate vulnerability) |
| `MimeKit` | 2.9.1 | 4.18.2 | Fixes GHSA-g7hc-96xr-gvvx (moderate vulnerability) |
| `Swashbuckle.AspNetCore` | 5.5.1 | 6.9.0 | Fixes GHSA-qrmm-w75w-3wpx; compatible with .NET 10 |
| `Swashbuckle.AspNetCore.Swagger` | 5.5.1 | *removed* | Sub-package included in meta-package |
| `Microsoft.AspNetCore.Mvc.Versioning` | 4.1.1 | *removed* | Deprecated; replaced by `Asp.Versioning.Mvc` |
| `Asp.Versioning.Mvc` | *(new)* | 8.1.0 | Modern API versioning package |
| `Asp.Versioning.Mvc.ApiExplorer` | *(new)* | 8.1.0 | API Explorer for versioning |
| `Serilog.AspNetCore` | 3.4.0 | 8.0.3 | Major version update for .NET 10 compatibility |
| `Serilog.Settings.Configuration` | 3.1.0 | 8.0.4 | Version update |
| `Serilog.Enrichers.Environment` | 2.1.3 | 3.0.1 | Version update |
| `Serilog.Enrichers.Process` | 2.0.1 | 3.0.0 | Version update |
| `Serilog.Enrichers.Thread` | 3.1.0 | 4.0.0 | Version update |
| `Serilog.Sinks.MSSqlServer` | 5.5.1 | 8.0.0 | Major version update |
| `System.Security.Cryptography.Pkcs` | *(transitive 9.0.11)* | 10.0.14 (pinned) | Fixes GHSA-2fmv-rv2c-59x7 + GHSA-846f-f764-76mx (pinned in WebApi, Identity, Persistence) |
| `NuGet.Packaging` | *(transitive 6.12.1)* | 7.9.0 (pinned) | Fixes GHSA-g4vj-cjjj-v7hg low severity |
| `NuGet.Protocol` | *(transitive 6.12.1)* | 7.9.0 (pinned) | Fixes GHSA-g4vj-cjjj-v7hg low severity |
| `Microsoft.Extensions.Logging` | *(missing)* | 10.0.14 | Added to Infrastructure.Shared for `ILogger<T>` support |

### Code Changes

| File | Change |
|------|--------|
| `Application/ServiceExtensions.cs` | Updated MediatR DI: `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` for MediatR 12. Updated AutoMapper: `AddAutoMapper(cfg => cfg.AddMaps(...))` for AutoMapper 16.x. Removed obsolete using statements. |
| `Application/Behaviours/ValidationBehaviour.cs` | Updated `IPipelineBehavior<TRequest, TResponse>` for MediatR 12: changed type constraint from `IRequest<TResponse>` to `notnull`, reordered Handle parameters (`next` before `cancellationToken`) |
| `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs` | Removed `Microsoft.EntityFrameworkCore.Internal` using (EF Core removed from Application) |
| `Infrastructure.Identity/Services/AccountService.cs` | Removed invalid usings (`Org.BouncyCastle.Ocsp`, `System.Net.Cache`, `Microsoft.AspNetCore.Mvc`, `Microsoft.Extensions.Primitives`). Replaced deprecated `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes()` |
| `Infrastructure.Identity/ServiceExtensions.cs` | Removed stray `using System.Reflection.Metadata.Ecma335;` |
| `Infrastructure.Shared/Services/DateTimeService.cs` | Removed stray `using System.Reflection.Metadata.Ecma335;` |
| `WebApi/Extensions/ServiceExtensions.cs` | Updated API versioning to `Asp.Versioning` namespace; chained `.AddMvc()` on `AddApiVersioning()` call per new package requirements |
| `WebApi/Controllers/v1/ProductController.cs` | Added `using Asp.Versioning;` for `[ApiVersion]` attribute; removed unused `Application.Features.Products.Commands` import |
| `WebApi/Startup.cs` | Added `services.AddEndpointsApiExplorer()` required for Swashbuckle 6.x+ with .NET 6+ |

---

## Architecture Notes

- **Application layer** removed its EF Core dependency — it was a clean-architecture violation. The Application layer now correctly only references domain types, MediatR, AutoMapper, and FluentValidation.
- **MediatR 12** merged `MediatR.Extensions.Microsoft.DependencyInjection` into the main package; `RegisterServicesFromAssembly` is the new way to register handlers.
- **AutoMapper 16** merged `AutoMapper.Extensions.Microsoft.DependencyInjection` and changed the DI registration API to `cfg.AddMaps(...)`.
- **API versioning** migrated from deprecated `Microsoft.AspNetCore.Mvc.Versioning` to the maintained `Asp.Versioning.Mvc` package.
- The `Startup.cs` / `IHostBuilder` pattern is retained (still fully supported in .NET 10); no forced migration to minimal hosting model was performed.

---

## Next Steps

1. **EF Core migrations**: The existing migrations in `Infrastructure.Identity/Migrations` and `Infrastructure.Persistence/Migrations` reference the old EF Core 3.1 schema. If the database schema has changed, run `dotnet ef migrations add <name>` to generate new migrations.
2. **Connection strings**: Update `appsettings.json` with valid SQL Server or InMemory configuration. The app currently uses `UseInMemoryDatabase` when `"UseInMemoryDatabase": true` is set in config.
3. **Serilog.Sinks.MSSqlServer 8.0.0**: If MSSqlServer sink is used, verify the `MSSqlServerSinkOptions` configuration in `appsettings.json` matches the new sink API (the table/schema configuration changed in v7→v8).
4. **FluentValidation.AspNetCore 11.x**: The `AddFluentValidation()` extension was removed in v11. This project uses MediatR pipeline validation (not ASP.NET Core auto-validation), so no `AddFluentValidation()` call is needed. If server-side auto-validation is later desired, use `services.AddFluentValidationAutoValidation()`.
5. **AutoMapper 16 profile registration**: The codebase uses basic `CreateMap<>()` which is fully compatible. If more advanced features (dynamic LINQ queries, `MapperConfiguration.AssertConfigurationIsValid`) are added in future, review AutoMapper 16's changelog for behavioral changes.
6. **JWT token library**: `System.IdentityModel.Tokens.Jwt` is now resolved at version 8.19.2 from the `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.14` transitive chain. The `JwtSecurityToken` / `JwtSecurityTokenHandler` APIs used in AccountService remain backward-compatible.
