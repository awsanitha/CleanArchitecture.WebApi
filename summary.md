# .NET 10 Migration Summary — CleanArchitecture.WebApi

## Result
`dotnet build CleanArchitecture.WebApi.sln` exits with **0 errors**, 24 warnings (all `NU190x` security advisories — see Next Steps).

---

## Changes Made

### Project Files

| Project | Change |
|---------|--------|
| `Domain/Domain.csproj` | Target framework: `netstandard2.1` → `net10.0` |
| `Application/Application.csproj` | Target framework: `netstandard2.1` → `net10.0`; updated packages (see below) |
| `Infrastructure.Identity/Infrastructure.Identity.csproj` | `System.IdentityModel.Tokens.Jwt` 6.7.1 → 8.19.2 (resolved NU1605 downgrade error); MimeKit 2.9.1 → 4.11.0; pinned `System.Security.Cryptography.Pkcs` to 10.0.14 |
| `Infrastructure.Persistence/Infrastructure.Persistence.csproj` | Pinned `System.Security.Cryptography.Pkcs` to 10.0.14 |
| `Infrastructure.Shared/Infrastructure.Shared.csproj` | MailKit 2.8.0 → 4.11.0; MimeKit 2.9.1 → 4.11.0; pinned `System.Security.Cryptography.Pkcs` to 10.0.14 |
| `WebApi/WebApi.csproj` | Swashbuckle 5.5.1 → 6.9.0; removed `Swashbuckle.AspNetCore.Swagger` (included in 6.x); Serilog packages updated to 8.x; `Microsoft.AspNetCore.Mvc.Versioning` 4.1.1 → `Asp.Versioning.Mvc` 8.1.0; removed `FluentValidation.AspNetCore` 9.1.2 (deprecated); pinned `System.Security.Cryptography.Pkcs` 10.0.14 |

### Application.csproj Package Updates

| Package | From | To |
|---------|------|----|
| `AutoMapper` | 10.0.0 | 13.0.1 |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | 8.0.1 | *removed* (merged into AutoMapper 12+) |
| `FluentValidation` | 9.1.2 | 11.11.0 |
| `FluentValidation.DependencyInjectionExtensions` | 9.1.2 | 11.11.0 |
| `MediatR.Extensions.Microsoft.DependencyInjection` | 8.1.0 | *removed* (merged into MediatR 12) |
| `MediatR` | — | 12.4.1 (added) |
| `Microsoft.EntityFrameworkCore` | 3.1.7 | 10.0.14 |
| `Microsoft.EntityFrameworkCore.InMemory` | 3.1.7 | 10.0.14 |
| `System.Text.Json` | 10.0.14 | *removed* (part of .NET shared framework, NU1510) |

### Code Changes

| File | Change |
|------|--------|
| `Application/ServiceExtensions.cs` | Updated `AddMediatR(Assembly)` to MediatR 12 API: `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` |
| `Application/Behaviours/ValidationBehaviour.cs` | Updated `IPipelineBehavior.Handle` signature — `next` parameter moved before `CancellationToken` per MediatR 12 interface; `next()` call unchanged |
| `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs` | Removed `using Microsoft.EntityFrameworkCore.Internal` (internal EF Core API, removed in 5+) |
| `Infrastructure.Identity/Services/AccountService.cs` | Removed `using Org.BouncyCastle.Ocsp` (BouncyCastle not referenced); removed `using System.Net.Cache`; replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes(40)` |
| `Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs` | Removed `using Org.BouncyCastle.Crypto.Prng.Drbg` (BouncyCastle not referenced) |
| `Infrastructure.Identity/ServiceExtensions.cs` | Removed `using System.Reflection.Metadata.Ecma335` (internal reflection API, unused) |
| `Infrastructure.Identity/Migrations/20200626175731_NewSchema.Designer.cs` | Replaced `.HasName(...)` with `.HasDatabaseName(...)` (removed in EF Core 5+) |
| `Infrastructure.Identity/Migrations/IdentityContextModelSnapshot.cs` | Same: `.HasName(...)` → `.HasDatabaseName(...)` |
| `WebApi/Extensions/ServiceExtensions.cs` | Updated `ApiVersion` using to `Asp.Versioning` namespace for `Asp.Versioning.Mvc` 8.x |
| `WebApi/Controllers/v1/ProductController.cs` | Added `using Asp.Versioning` for `[ApiVersion]` attribute |

---

## Next Steps

The following security vulnerability warnings remain. All are in packages where **no patched version exists on NuGet.org** — they cannot be resolved by version upgrades alone:

| Package | Advisory | Severity | Notes |
|---------|----------|----------|-------|
| `AutoMapper` 13.0.1 | GHSA-rvv3-g6hj-g44x | High | Latest available version; no fixed release published yet. Consider replacing AutoMapper with Mapster or manual mapping if this becomes a blocker. |
| `MailKit` 4.11.0 | GHSA-9j88-vvj5-vhgr | Moderate | Latest available version; monitor MailKit releases for a patch. |
| `MimeKit` 4.11.0 | GHSA-g7hc-96xr-gvvx | Moderate | Latest available version; monitor MimeKit releases for a patch. |
| `NuGet.Packaging` 6.12.1 | GHSA-g4vj-cjjj-v7hg | Low | Build-time tool, transitive from `Microsoft.VisualStudio.Web.CodeGeneration.Design 10.0.2`. Does not affect runtime. |
| `NuGet.Protocol` 6.12.1 | GHSA-g4vj-cjjj-v7hg | Low | Same as above. |

Other future work:
- **Database migrations**: The EF Core migrations were generated against EF Core 3.1.6. After migrating to EF Core 10, it is recommended to drop and regenerate the migrations or run `dotnet ef migrations add` to capture the new snapshot format. This does not affect compilation but may affect runtime schema generation.
- **Serilog.Sinks.MSSqlServer**: Updated to 8.1.0. Verify appsettings.json Serilog MSSqlServer sink configuration is still valid (column names, sink options may differ between v5 and v8).
- **EmailService**: Uses synchronous `Connect`/`Authenticate` within an async method. Consider migrating to `ConnectAsync`/`AuthenticateAsync` for MailKit 4.x best practice.
- **FluentValidation.AspNetCore** removal: The old package provided automatic model-state validation. The current code uses MediatR pipeline behavior for validation — no functionality was lost. If direct ASP.NET Core model binding validation is needed in future, call `services.AddFluentValidationAutoValidation()` from `FluentValidation.AspNetCore` (re-add the package).
