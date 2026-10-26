# Migration Summary: CleanArchitecture.WebApi — .NET Framework → .NET 10

## Result
`dotnet build CleanArchitecture.WebApi.sln` → **0 errors, 0 warnings**

---

## Changes Made

### Package / Project File Changes

| File | Change |
|---|---|
| `Infrastructure.Identity/Infrastructure.Identity.csproj` | Removed `System.IdentityModel.Tokens.Jwt 6.7.1` (downgrade conflict with JWT Bearer 10.0.14); bumped to `8.19.2`; updated `MimeKit` 2.9.1 → 4.18.2 |
| `Application/Application.csproj` | Retargeted from `netstandard2.1` → `net10.0`; removed `MediatR.Extensions.Microsoft.DependencyInjection` (merged into MediatR 12); removed `AutoMapper.Extensions.Microsoft.DependencyInjection` (merged into AutoMapper 13+); upgraded `AutoMapper` 10→16.2.0, `FluentValidation` 9→11.11.0, `FluentValidation.DependencyInjectionExtensions` 9→11.11.0, `Microsoft.EntityFrameworkCore` 3.1.7→10.0.14, `Microsoft.EntityFrameworkCore.InMemory` 3.1.7→10.0.14, added `MediatR 12.4.1`; removed redundant `System.Text.Json` (built into SDK) |
| `Infrastructure.Shared/Infrastructure.Shared.csproj` | Updated `MailKit` 2.8.0→4.18.2, `MimeKit` 2.9.1→4.18.2 |
| `Infrastructure.Persistence/Infrastructure.Persistence.csproj` | Added explicit `System.Security.Cryptography.Pkcs 10.0.14` to override vulnerable transitive 9.0.11 |
| `WebApi/WebApi.csproj` | Replaced `Swashbuckle.AspNetCore 5.5.1` → `8.1.1` (removed separate `Swashbuckle.AspNetCore.Swagger`); replaced `Microsoft.AspNetCore.Mvc.Versioning 4.1.1` → `Asp.Versioning.Mvc 8.1.0`; removed `FluentValidation.AspNetCore` (not used — validation is via MediatR pipeline); updated Serilog packages to latest compatible versions (AspNetCore 8.0.3, Enrichers, Settings.Configuration 8.0.4, Sinks.MSSqlServer 8.1.0); added `NuGet.Packaging/Protocol 7.9.0` to override vulnerable 6.12.1 transitive deps; marked `Microsoft.VisualStudio.Web.CodeGeneration.Design` as `PrivateAssets=all` |

### Code Changes

| File | Change |
|---|---|
| `Application/ServiceExtensions.cs` | Updated `AddMediatR(Assembly)` → `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` (MediatR 12 API) |
| `Application/ServiceExtensions.cs` | Updated `AddAutoMapper(Assembly)` → `AddAutoMapper(cfg => cfg.AddMaps(Assembly))` (AutoMapper 16 API) |
| `Application/Behaviours/ValidationBehaviour.cs` | Fixed `IPipelineBehavior.Handle` signature: moved `CancellationToken` to last parameter position (MediatR 10+ API change) |
| `Infrastructure.Identity/Services/AccountService.cs` | Removed invalid `using Org.BouncyCastle.Ocsp;` (BouncyCastle not referenced); removed `using System.Net.Cache;` (removed in .NET 5+); removed unused `using Microsoft.AspNetCore.Mvc;`; replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes()` (.NET 6+ API) |
| `Infrastructure.Identity/ServiceExtensions.cs` | Removed invalid `using System.Reflection.Metadata.Ecma335;` |
| `WebApi/Extensions/ServiceExtensions.cs` | Added `using Asp.Versioning;`; updated `AddApiVersioning()` call to include `.AddMvc()` (Asp.Versioning.Mvc 8.x requirement) |
| `WebApi/Controllers/v1/ProductController.cs` | Added `using Asp.Versioning;` for `[ApiVersion]` attribute; removed duplicate `using Application.Filters;` |

---

## Next Steps

- **AutoMapper 16.x breaking changes**: If complex mapping expressions (ValueTransformers, custom resolvers) are added in future, review AutoMapper 16.x changelog.
- **BouncyCastle.Cryptography**: Transitive dependency of MimeKit 4.x with known moderate vulnerabilities. No direct fix available; MimeKit itself is updated to latest (4.18.2). Monitor MimeKit releases for a BouncyCastle upgrade.
- **Database migrations**: EF Core and Identity migrations in `/Migrations` folders should be verified against a target database after deployment. The migration files were excluded from compilation in `Infrastructure.Persistence.csproj` and may need regeneration for a fresh database.
- **Serilog.Sinks.MSSqlServer**: Confirm appsettings.json Serilog configuration is compatible with v8.1.0 (column options changed in v7+).
