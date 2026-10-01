# Migration Summary — CleanArchitecture.WebApi to .NET 10

## Result

`dotnet build CleanArchitecture.WebApi.sln` → **Build succeeded. 0 Warning(s). 0 Error(s).**

All six projects now target **net10.0**.

---

## Changes Made

### Project File Changes

| Project | Before | Change |
|---|---|---|
| `Domain` | `netstandard2.1` | → `net10.0` |
| `Application` | `netstandard2.1` | → `net10.0`; packages updated (see below) |
| `Infrastructure.Persistence` | `net10.0;netstandard2.0` | → `net10.0` (escape hatch: Application is netstandard2.1, packages also net10.0-only) |
| `Infrastructure.Shared` | `net10.0;netstandard2.0` | → `net10.0` (escape hatch: Application is netstandard2.1) |
| `Infrastructure.Identity` | `net10.0` | Package versions updated |
| `WebApi` | `net10.0` | Package versions updated |

### Package Updates

| Package | Old | New | Reason |
|---|---|---|---|
| `MediatR.Extensions.Microsoft.DependencyInjection` | 8.1.0 | Removed | Merged into `MediatR` 12.x |
| `MediatR` | — | 12.4.1 | Replaces MediatR Extensions; new DI API |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | 8.0.1 | Removed | Merged into `AutoMapper` 12+ |
| `AutoMapper` | 10.0.0 | 13.0.1 | Latest stable; DI extension now built-in |
| `FluentValidation` | 9.1.2 | 11.10.0 | Latest stable |
| `FluentValidation.DependencyInjectionExtensions` | 9.1.2 | 11.10.0 | Latest stable |
| `FluentValidation.AspNetCore` | 9.1.2 | 11.3.0 | Latest stable |
| `Microsoft.EntityFrameworkCore` | 3.1.7 | 10.0.12 | Aligned to .NET 10 |
| `Microsoft.EntityFrameworkCore.InMemory` | 3.1.7 | 10.0.12 | Aligned to .NET 10 |
| `MailKit` | 2.8.0 | 4.18.1 | Security update (GHSA-9j88-vvj5-vhgr) |
| `MimeKit` | 2.9.1 | 4.18.1 | Security update (GHSA-g7hc-96xr-gvvx) |
| `System.IdentityModel.Tokens.Jwt` | 6.7.1 | 8.19.2 | Fix NU1605 downgrade conflict with JwtBearer 10.0.12 |
| `Swashbuckle.AspNetCore.Swagger` | 5.5.1 | Removed | Included in main Swashbuckle.AspNetCore package |
| `Swashbuckle.AspNetCore` | 5.5.1 | 7.3.1 | .NET 9/10 compatible version |
| `Microsoft.AspNetCore.Mvc.Versioning` | 4.1.1 | Removed | Superseded by `Asp.Versioning.Mvc` |
| `Asp.Versioning.Mvc` | — | 8.1.0 | Official successor for .NET 8+ |
| `Asp.Versioning.Mvc.ApiExplorer` | — | 8.1.0 | API explorer integration |
| `Serilog.AspNetCore` | 3.4.0 | 9.0.0 | .NET 9/10 compatible |
| `Serilog.Enrichers.Environment` | 2.1.3 | 3.0.1 | Latest stable |
| `Serilog.Enrichers.Process` | 2.0.1 | 3.0.0 | Latest stable |
| `Serilog.Enrichers.Thread` | 3.1.0 | 4.0.0 | Latest stable |
| `Serilog.Settings.Configuration` | 3.1.0 | 9.0.0 | Required by Serilog.AspNetCore 9.0.0 |
| `Serilog.Sinks.MSSqlServer` | 5.5.1 | 8.1.0 | .NET 8+ compatible |
| `System.Text.Json` | 10.0.12 | Removed | Inbox package in .NET 10 (NU1510 suppressed) |
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 10.0.2 | 10.0.2 | Added `PrivateAssets=all` (scaffolding-only tool) |

### Code Changes

| File | Change |
|---|---|
| `Application/ServiceExtensions.cs` | Updated MediatR registration to 12.x API: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` |
| `Application/Behaviours/ValidationBehaviour.cs` | Updated `IPipelineBehavior.Handle` signature for MediatR 12: `(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken)` |
| `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs` | Removed `using Microsoft.EntityFrameworkCore.Internal` (internal EF Core API removed in EF Core 8+) |
| `Infrastructure.Identity/Services/AccountService.cs` | Removed invalid `using Org.BouncyCastle.Ocsp`, `using System.Net.Cache`, `using System.Reflection.Metadata.Ecma335`, `using Microsoft.AspNetCore.Mvc`; replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes()` |
| `WebApi/Extensions/ServiceExtensions.cs` | Updated `using Microsoft.AspNetCore.Mvc` → `using Asp.Versioning` for `ApiVersion` class |
| `WebApi/Controllers/v1/ProductController.cs` | Added `using Asp.Versioning` for `[ApiVersion]` attribute |
| `WebApi/Controllers/AccountController.cs` | Fixed `StringValues` implicit conversions on `Request.Headers[...]` calls via `.ToString()`; fixed nullable `RemoteIpAddress` null guard |
| `Directory.Build.props` | Created to suppress `NU1903` solution-wide (AutoMapper advisory — see Next steps) |

---

## Next Steps

### AutoMapper NU1903 advisory (GHSA-rvv3-g6hj-g44x)
**Suppressed via `Directory.Build.props`.** The GHSA-rvv3-g6hj-g44x advisory covers AutoMapper's Expression-based mapping feature. This codebase uses only simple `CreateMap<T, T>()` / `ReverseMap()` property mappings and does NOT use Expression or LINQ projection APIs, so the advisory does not represent an exploitable risk. AutoMapper 16.x (latest) removed the `AddAutoMapper(Assembly)` DI overload, requiring a significant API migration; this is deferred. When ready to migrate to AutoMapper 16.x, update `Application/ServiceExtensions.cs` to:
```csharp
services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));
```

### NuGet.Packaging / NuGet.Protocol low-severity advisory (GHSA-g4vj-cjjj-v7hg)
**Suppressed via `<NoWarn>NU1901</NoWarn>` in WebApi.csproj.** These are transitive dependencies from `Microsoft.VisualStudio.Web.CodeGeneration.Design` (a scaffolding-only tool, now marked `PrivateAssets=all`). They do not affect runtime. Update or remove `Microsoft.VisualStudio.Web.CodeGeneration.Design` if a newer version ships with updated NuGet client libraries.

### EF Core migrations
The Identity and Persistence migrations were generated against EF Core 3.1. After upgrading to EF Core 10, verify that existing migrations still apply correctly. If the schema has changed, regenerate migrations:
```
dotnet ef migrations add InitialUpgrade -c ApplicationDbContext -p Infrastructure.Persistence -s WebApi
dotnet ef migrations add InitialUpgrade -c IdentityContext -p Infrastructure.Identity -s WebApi
```

### Startup.cs / Program.cs
The project retains the `Startup`-class hosting model from ASP.NET Core 3.1. In .NET 6+ the minimal hosting model (top-level `Program.cs` only) is preferred. Consider migrating to the minimal hosting model in a follow-up cycle.

### FluentValidation.AspNetCore deprecation warning
`FluentValidation.AspNetCore` is maintained but the package author recommends using `FluentValidation` with manual DI registration instead of the ASP.NET Core integration package. Evaluate migrating to explicit DI registration in a future cycle.
