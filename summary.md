# Migration Summary: .NET 3.1 / netstandard2.1 → .NET 10

## Result
`dotnet build CleanArchitecture.WebApi.sln` → **0 errors, 0 warnings**

## Changes Made

### Project Files
| Project | Before | After |
|---|---|---|
| Domain | netstandard2.1 | net10.0 |
| Application | netstandard2.1 | net10.0 |
| Infrastructure.Persistence | net10.0 | net10.0 (already correct) |
| Infrastructure.Identity | net10.0 | net10.0 (already correct) |
| Infrastructure.Shared | net10.0 | net10.0 (already correct) |
| WebApi | net10.0 | net10.0 (already correct) |

### Package Updates

**Application.csproj**
- Removed `AutoMapper.Extensions.Microsoft.DependencyInjection` (absorbed into AutoMapper 12+)
- Removed `MediatR.Extensions.Microsoft.DependencyInjection` (absorbed into MediatR 12+)
- `AutoMapper` 10.0.0 → 16.2.0 (fixed GHSA-rvv3-g6hj-g44x high severity vulnerability)
- `MediatR` new (replaces old extensions package), version 12.4.1
- `Microsoft.EntityFrameworkCore` 3.1.7 → 10.0.12
- `Microsoft.EntityFrameworkCore.InMemory` 3.1.7 → 10.0.12
- `FluentValidation` 9.1.2 → 11.11.0
- `FluentValidation.DependencyInjectionExtensions` 9.1.2 → 11.11.0
- Removed `System.Text.Json` (implicit in net10.0 SDK)

**Infrastructure.Identity.csproj**
- Removed `System.IdentityModel.Tokens.Jwt 6.7.1` (caused NU1605 downgrade error; now resolved transitively at correct version from JwtBearer 10.0.12)
- `MimeKit` 2.9.1 → 4.18.1 (fixed GHSA-g7hc-96xr-gvvx moderate severity vulnerability)

**Infrastructure.Shared.csproj**
- `MailKit` 2.8.0 → 4.18.0 (fixed GHSA-9j88-vvj5-vhgr moderate severity vulnerability)
- `MimeKit` 2.9.1 → 4.18.1 (fixed GHSA-g7hc-96xr-gvvx moderate severity vulnerability)

**WebApi.csproj**
- `Swashbuckle.AspNetCore` 5.5.1 → 7.0.0 (fixed GHSA-qrmm-w75w-3wpx moderate severity vulnerability)
- Removed `Swashbuckle.AspNetCore.Swagger` (included in Swashbuckle.AspNetCore 6+)
- `Serilog.AspNetCore` 3.4.0 → 9.0.0
- `Serilog.Enrichers.Environment` 2.1.3 → 3.0.1
- `Serilog.Enrichers.Process` 2.0.1 → 3.0.0
- `Serilog.Enrichers.Thread` 3.1.0 → 4.0.0
- `Serilog.Sinks.MSSqlServer` 5.5.1 → 10.0.0
- Removed `Serilog.Settings.Configuration` (resolved transitively from Serilog.AspNetCore 9.0.0)
- `Microsoft.AspNetCore.Mvc.Versioning` 4.1.1 → removed; replaced with `Asp.Versioning.Mvc` 8.1.0
- `FluentValidation.AspNetCore` 9.1.2 → 11.3.0
- Removed `Microsoft.VisualStudio.Web.CodeGeneration.Design` (design-time scaffolding tool; transitive NuGet.Packaging/NuGet.Protocol 6.12.1 deps caused low-severity NU1901 vulnerability warnings; tool not needed at build/runtime)

### Code Changes

**Application/Behaviours/ValidationBehaviour.cs**
- Updated `IPipelineBehavior<TRequest, TResponse>.Handle` signature for MediatR 12: reordered `next` and `cancellationToken` parameters and changed constraint from `where TRequest : IRequest<TResponse>` to `where TRequest : notnull`

**Application/ServiceExtensions.cs**
- Updated `AddMediatR(Assembly)` to MediatR 12 syntax: `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly))`
- Updated `AddAutoMapper(Assembly)` to AutoMapper 16 syntax: `AddAutoMapper(cfg => cfg.AddMaps(Assembly))`
- Removed unused imports

**Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs**
- Removed `using Microsoft.EntityFrameworkCore.Internal` (namespace removed in EF Core 5+)

**Infrastructure.Identity/Services/AccountService.cs**
- Removed unused imports: `Org.BouncyCastle.Ocsp`, `System.Net.Cache`, `System.Reflection.Metadata.Ecma335`, `Microsoft.AspNetCore.Mvc`, `Microsoft.Extensions.Primitives`
- Replaced deprecated `RNGCryptoServiceProvider` with `RandomNumberGenerator.Fill()` (.NET 6+ API)

**Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs**
- Removed unused `using Org.BouncyCastle.Crypto.Prng.Drbg` (BouncyCastle not referenced)

**Infrastructure.Identity/ServiceExtensions.cs**
- Removed unused `using System.Reflection.Metadata.Ecma335`
- Removed unused `using Application.Exceptions`

**WebApi/Extensions/ServiceExtensions.cs**
- Updated namespace from `Microsoft.AspNetCore.Mvc` to `Asp.Versioning` for `ApiVersion`
- Updated `AddApiVersioningExtension` to chain `.AddMvc()` as required by `Asp.Versioning.Mvc` 8.x

**WebApi/Controllers/v1/ProductController.cs**
- Added `using Asp.Versioning` for `[ApiVersion]` attribute
- Removed unused imports

## Next Steps

- **Serilog.Sinks.MSSqlServer 10.0.0**: Major version upgrade from 5.x. The appsettings.json Serilog MSSqlServer sink configuration should be reviewed for any configuration key changes in the 10.x version before production deployment.
- **AutoMapper 16.x**: Uses `cfg.AddMaps(Assembly)` API. Profile mapping is unchanged; test the mapping behavior in integration tests.
- **Asp.Versioning.Mvc 8.x**: Replaces `Microsoft.AspNetCore.Mvc.Versioning`. API version route constraints still work. Consider adding `services.AddApiVersioning().AddApiExplorer()` if Swagger version enumeration is needed.
- **Microsoft.VisualStudio.Web.CodeGeneration.Design**: Removed from production project file. If scaffolding is needed during development, add it back to a local `Directory.Build.props` or developer-specific project file.
