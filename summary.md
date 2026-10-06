# Migration Summary — CleanArchitecture.WebApi → .NET 10

## Status
✅ **Build succeeded: 0 errors, 0 warnings**  
All 6 projects target `net10.0`.

---

## Changes Made

### Project File Changes

| Project | Before | After | Reason |
|---|---|---|---|
| `Domain` | `netstandard2.1` | `net10.0` | Align all layers to net10.0; enables AutoMapper 15.x |
| `Application` | `netstandard2.1` | `net10.0` | Same; AutoMapper 13+ requires net6.0+ |
| `Infrastructure.Persistence` | `net10.0;netstandard2.0` | `net10.0` | ASP.NET Core / EF Core 10 packages are net10.0-only; Application/Domain don't support netstandard2.0 (NU1201/NU1202 escape-hatch applied) |
| `Infrastructure.Shared` | `net10.0;netstandard2.0` | `net10.0` | Dependency on Application (netstandard2.1) is not compatible with netstandard2.0 (NU1201 escape-hatch applied) |
| `Infrastructure.Identity` | `net10.0` | `net10.0` (no TFM change) | Removed explicit `System.IdentityModel.Tokens.Jwt 6.7.1` reference (NU1605 downgrade conflict with `JwtBearer 10.0.12` transitive `8.19.2` requirement); removed unused `MimeKit` reference |
| `WebApi` | `net10.0` | `net10.0` (no TFM change) | Package upgrades; removed `Microsoft.VisualStudio.Web.CodeGeneration.Design` (brought in low-severity NuGet vulnerabilities via transitive deps) |

### Package Upgrades

| Package | Old Version | New Version | Reason |
|---|---|---|---|
| `AutoMapper` | 10.0.0 | 15.1.3 | HIGH severity vulnerability GHSA-rvv3-g6hj-g44x; versions ≤ 13.0.1 still vulnerable |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | 8.0.1 | _removed_ | DI registration merged into AutoMapper 12+ main package |
| `MailKit` | 2.8.0 | 4.18.1 | MODERATE severity vulnerability GHSA-9j88-vvj5-vhgr |
| `MimeKit` | 2.9.1 | 4.18.1 | MODERATE severity vulnerability GHSA-g7hc-96xr-gvvx |
| `Swashbuckle.AspNetCore` | 5.5.1 | 6.9.0 | MODERATE severity vulnerability GHSA-qrmm-w75w-3wpx |
| `Swashbuckle.AspNetCore.Swagger` | 5.5.1 | _removed_ | Included in the main Swashbuckle.AspNetCore package |
| `Microsoft.AspNetCore.Mvc.Versioning` | 4.1.1 | _removed_ | Deprecated; replaced by `Asp.Versioning.Mvc 8.1.0` |
| `Asp.Versioning.Mvc` | _(new)_ | 8.1.0 | Modern API versioning package for .NET 8+ |
| `Microsoft.Extensions.Logging.Abstractions` | _(new)_ | 10.0.12 | Explicit dep for `ILogger<T>` in Infrastructure.Shared (previously came transitively through MailKit 2.x) |
| `System.ComponentModel.Annotations` | _(new)_ | _removed after Domain/Application net10.0 upgrade_ | Was temporarily needed; no longer required after targeting net10.0 |
| `Microsoft.EntityFrameworkCore` (Application) | 3.1.7 | _removed_ | Not used in Application layer code |
| `Microsoft.EntityFrameworkCore.InMemory` (Application) | 3.1.7 | _removed_ | Not used in Application layer code |

### Source Code Fixes

**`Infrastructure.Identity/Services/AccountService.cs`**
- Removed `using Org.BouncyCastle.Ocsp;` — no BouncyCastle package referenced; unused
- Removed `using System.Net.Cache;` — does not exist in .NET Core
- Removed `using Microsoft.Extensions.Primitives;` — unused
- Replaced deprecated `RNGCryptoServiceProvider` with `RandomNumberGenerator.Fill(bytes)` (SYSLIB0023 fix)

**`Infrastructure.Identity/ServiceExtensions.cs`**
- Removed `using System.Reflection.Metadata.Ecma335;` — unused

**`Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs`**
- Removed `using Org.BouncyCastle.Crypto.Prng.Drbg;` — no BouncyCastle package referenced; unused

**`Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs`**
- Removed `using Microsoft.EntityFrameworkCore.Internal;` — unused; internal EF Core namespace not accessible in EF Core 5+

**`Application/Interfaces/IAccountService.cs`**
- Removed `using Microsoft.Extensions.Primitives;` — unused

**`Application/ServiceExtensions.cs`**
- Updated `services.AddAutoMapper(Assembly)` → `services.AddAutoMapper(cfg => cfg.AddMaps(Assembly))` — required by AutoMapper 15.x API (Assembly overload removed)

**`WebApi/Extensions/ServiceExtensions.cs`**
- Added `using Asp.Versioning;` for new `ApiVersion` type location
- Updated `services.AddApiVersioning(...)` to chain `.AddMvc()` — required by `Asp.Versioning.Mvc`
- Fixed XML docs path from Windows backslash to `Path.Combine` (cross-platform fix)
- Removed unused `System.Linq`, `System.Threading.Tasks` usings

**`WebApi/Controllers/v1/ProductController.cs`**
- Added `using Asp.Versioning;` for `[ApiVersion]` attribute
- Removed unused `using Application.Features.Products.Commands;`

---

## Final Build Result

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

All 6 projects compiled to `net10.0`:
- `Domain`
- `Application`
- `Infrastructure.Identity`
- `Infrastructure.Persistence`
- `Infrastructure.Shared`
- `WebApi`

---

## Next Steps

1. **MediatR version** — `MediatR.Extensions.Microsoft.DependencyInjection 8.1.0` (MediatR 8.x) is used. Upgrading to MediatR 12.x is recommended long-term but requires:
   - Changing `ValidationBehavior<TRequest, TResponse>.Handle` signature (parameter order changed)
   - Changing `services.AddMediatR(Assembly)` to `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly))`
   - No security vulnerability warning for MediatR 8.x at this time; deferred to avoid breaking changes.

2. **Serilog packages** — Serilog.AspNetCore 3.4.0, Serilog.Settings.Configuration 3.1.0, and Serilog.Sinks.MSSqlServer 5.5.1 are old but compiling without warnings. Upgrade to Serilog.AspNetCore 8.x and Serilog.Settings.Configuration 8.x is recommended for .NET 10 compatibility when there is capacity for testing.

3. **FluentValidation.AspNetCore 9.1.2** — Old but working. Consider upgrading to 11.x; test for any breaking changes in model validation integration.

4. **Database migrations** — EF Core migrations in `Infrastructure.Persistence` and `Infrastructure.Identity` were created for EF Core 3.1. After running against an EF Core 10 database provider, verify migrations work correctly or regenerate them.

5. **netstandard2.0 removal recorded** — `Infrastructure.Persistence` and `Infrastructure.Shared` no longer support `netstandard2.0`. Any .NET Framework callers of these libraries (if any exist outside this solution) will need to be updated.
