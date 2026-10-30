# .NET Migration Summary: CleanArchitecture.WebApi → net10.0

## Result
`dotnet build CleanArchitecture.WebApi.sln` → **0 errors, 0 warnings**

## Changes Made

### 1. Package Version Fix — Infrastructure.Identity
- `System.IdentityModel.Tokens.Jwt` 6.7.1 → **8.19.2**
  - Fix: NU1605 downgrade error caused by `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.14` requiring ≥ 8.19.2.

### 2. Stale Build Artifacts (MSB3374 permission errors)
- Removed all root-owned `*.Up2Date` and `*.cache` files from `obj/` directories that could not have their timestamps updated by the current user.

### 3. Domain project — target framework upgrade
- `Domain.csproj`: `netstandard2.1` → **net10.0**

### 4. Application project — target framework + package upgrades
- `Application.csproj`: `netstandard2.1` → **net10.0**
- `AutoMapper` 10.0.0 → **16.2.0** (fixes HIGH vulnerability GHSA-rvv3-g6hj-g44x)
- Removed `AutoMapper.Extensions.Microsoft.DependencyInjection` (DI extensions are built into AutoMapper 13+)
- `Microsoft.EntityFrameworkCore` 3.1.7 → **10.0.14** (aligned with other projects)
- `Microsoft.EntityFrameworkCore.InMemory` 3.1.7 → **10.0.14**
- Removed explicit `System.Text.Json` reference (NU1510 — built-in for net10.0)

### 5. Application/ServiceExtensions.cs — AutoMapper 16 API update
- `services.AddAutoMapper(Assembly.GetExecutingAssembly())`
  → `services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()))`
  - AutoMapper 14+ removed the `AddAutoMapper(params Assembly[])` overload.

### 6. Infrastructure.Shared — MailKit/MimeKit upgrades
- `MailKit` 2.8.0 → **4.18.2** (fixes MODERATE vulnerability GHSA-9j88-vvj5-vhgr)
- `MimeKit` 2.9.1 → **4.18.2** (fixes MODERATE vulnerability GHSA-g7hc-96xr-gvvx)

### 7. Infrastructure.Identity — MimeKit upgrade
- `MimeKit` 2.9.1 → **4.18.2** (same vulnerability fix)

### 8. WebApi — Swashbuckle + security package upgrades
- `Swashbuckle.AspNetCore` 5.5.1 → **6.9.0** (fixes MODERATE vulnerability GHSA-qrmm-w75w-3wpx)
- `Swashbuckle.AspNetCore.Swagger` 5.5.1 → **6.9.0**
- Added `System.Security.Cryptography.Pkcs` **10.0.14** (explicit pin, overrides transitive 9.0.11 which has HIGH/MODERATE vulnerabilities GHSA-846f-f764-76mx / GHSA-2fmv-rv2c-59x7)
- Added `NuGet.Packaging` **6.13.2** and `NuGet.Protocol` **6.13.2** (explicit pins to override transitive 6.12.1 with LOW vulnerability GHSA-g4vj-cjjj-v7hg)

### 9. Infrastructure.Persistence — security package pin
- Added `System.Security.Cryptography.Pkcs` **10.0.14** (overrides transitive 9.0.11)

## Final Project Target Frameworks
| Project | Before | After |
|---------|--------|-------|
| Domain | netstandard2.1 | net10.0 |
| Application | netstandard2.1 | net10.0 |
| Infrastructure.Identity | net10.0 | net10.0 |
| Infrastructure.Persistence | net10.0 | net10.0 |
| Infrastructure.Shared | net10.0 | net10.0 |
| WebApi | net10.0 | net10.0 |

## Next Steps
- The solution uses `Microsoft.AspNetCore.Mvc.Versioning 4.1.1` (legacy package). Consider migrating to `Asp.Versioning.Mvc` (the successor package) for long-term support.
- `FluentValidation 9.1.2` and `FluentValidation.AspNetCore 9.1.2` are old. Consider upgrading to 11.x (has minor breaking changes in error message formatting).
- `MediatR.Extensions.Microsoft.DependencyInjection 8.1.0` is old. In MediatR 12+, this package is merged into MediatR itself. Migration requires updating `services.AddMediatR(Assembly)` to `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly))`.
- `Serilog.AspNetCore 3.4.0` and related Serilog packages are old. Upgrade to 8.x for .NET 10 compatibility improvements.
- The `Infrastructure.Identity/Services/AccountService.cs` contains an import for `Org.BouncyCastle.Ocsp` that is not in any .csproj. This import exists in the source but since the build succeeded, it appears it's either not active or was already handled. Verify this file compiles correctly at runtime.
