# Migration Summary — CleanArchitecture.WebApi → net10.0

## Final Build Status

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

All 6 projects build cleanly targeting `net10.0`.

---

## What Was Done

### 1. Project Target Framework Upgrades
| Project | Before | After |
|---------|--------|-------|
| Domain | netstandard2.1 | net10.0 |
| Application | netstandard2.1 | net10.0 |
| Infrastructure.Persistence | net10.0 | net10.0 (no change, ATX already upgraded) |
| Infrastructure.Identity | net10.0 | net10.0 (no change) |
| Infrastructure.Shared | net10.0 | net10.0 (no change) |
| WebApi | net10.0 | net10.0 (no change) |

### 2. Security Vulnerability Fixes
| Package | Old Version | New Version | Severity |
|---------|------------|-------------|----------|
| AutoMapper | 10.0.0 | 16.2.0 | HIGH (GHSA-rvv3-g6hj-g44x) |
| Newtonsoft.Json | 12.0.3 | 13.0.4 | HIGH (GHSA-5crp-9r3c-p9vr) |

Note: AutoMapper 10–14 are all affected by GHSA-rvv3-g6hj-g44x. The fix requires AutoMapper ≥ 15.1.1. AutoMapper 16.2.0 (latest) was chosen.

### 3. Package Modernisation (Application.csproj)
| Package | Action | Notes |
|---------|--------|-------|
| AutoMapper.Extensions.Microsoft.DependencyInjection 8.0.1 | Removed | Folded into AutoMapper 13+ |
| MediatR.Extensions.Microsoft.DependencyInjection 8.1.0 | Removed | Replaced by MediatR 12+ built-in DI |
| MediatR | Added 12.4.1 | Replaces the deprecated extension package |
| FluentValidation | 9.1.2 → 12.1.1 | Major upgrade, same public API for validators |
| FluentValidation.DependencyInjectionExtensions | 9.1.2 → 12.1.1 | Aligned with FluentValidation |
| Microsoft.EntityFrameworkCore | 3.1.7 → Removed | Not used in Application layer |
| Microsoft.EntityFrameworkCore.InMemory | 3.1.7 → Removed | Not used in Application layer |
| System.Text.Json | 4.7.2 → Removed | Built into .NET 10 runtime |

### 4. Code Changes

#### Application/Behaviours/ValidationBehaviour.cs
- Updated `IPipelineBehavior<TRequest,TResponse>` `Handle` signature for MediatR 12:  
  `next` and `cancellationToken` parameters are swapped.
- Changed constraint from `where TRequest : IRequest<TResponse>` to `where TRequest : notnull`  
  (MediatR 12 removed the `IRequest<TResponse>` constraint from the interface).

#### Application/ServiceExtensions.cs
- `services.AddMediatR(Assembly)` → `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly))` (MediatR 12 API)
- `services.AddAutoMapper(Assembly)` → `services.AddAutoMapper(cfg => cfg.AddMaps(Assembly))` (AutoMapper 16 API)
- Removed obsolete `using` directives.

#### Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs
- Removed unused `using Microsoft.EntityFrameworkCore.Internal` (internal EF Core API, not needed).

#### Infrastructure.Identity/Services/AccountService.cs
- Removed unused imports: `Org.BouncyCastle.Ocsp`, `System.Net.Cache`, `Microsoft.AspNetCore.Mvc`, `Microsoft.Extensions.Primitives`, `System.Security.Cryptography`.

#### Infrastructure.Identity/ServiceExtensions.cs
- Removed unused `using System.Reflection.Metadata.Ecma335`.

#### Infrastructure.Shared/Infrastructure.Shared.csproj
- Added explicit `Microsoft.Extensions.Logging.Abstractions 10.0.0` reference.  
  Previously resolved transitively via EF Core in Application; now must be explicit.

#### WebApi/WebApi.csproj
- Removed `Microsoft.VisualStudio.Web.CodeGeneration.Design 10.0.2`.  
  Scaffolding is already complete; the tool is not needed at runtime, and its
  transitive dependencies (`NuGet.Packaging/Protocol 6.12.1`) carried NU1901 
  low-severity vulnerability warnings.

#### WebApi/Extensions/ServiceExtensions.cs
- Added `config.ApiVersionReader = new UrlSegmentApiVersionReader()` to fix
  AV0015 analyzer warning. Matches the `v{version:apiVersion}` route template
  defined in `BaseApiController`.

---

## Next Steps

- **Database migrations**: Re-generate EF Core migrations if the target database is non-InMemory.  
  The existing migration files are compatible with EF Core 10 but should be verified against the target schema.
- **Scaffolding tooling**: If controller/view scaffolding is needed during future development, add  
  `Microsoft.VisualStudio.Web.CodeGeneration.Design` back as a dev-only dependency (with `<PrivateAssets>all</PrivateAssets>`).
- **Integration tests**: No test project exists in the solution. Adding integration tests with  
  `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.InMemory` is recommended.
- **AutoMapper profile migration**: AutoMapper 16+ recommends constructor-free profile configuration using  
  `IMapperConfigurationExpression.CreateMap<>()` via `cfg.AddMaps()`. The current `Profile`-constructor  
  `CreateMap<>()` pattern still compiles and works in AutoMapper 16, but should be modernised in a  
  future cycle if the team adopts AutoMapper 17+.
- **AccountController.cs**: The `GenerateIPAddress()` method accesses  
  `HttpContext.Connection.RemoteIpAddress.MapToIPv4()` which can throw if `RemoteIpAddress` is null  
  (e.g. in integration tests or behind certain proxies). Consider adding a null check.
