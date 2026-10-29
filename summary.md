# Migration Summary: ASP.NET Core 3.1 → .NET 10

## Status: ✅ Complete — Build Succeeded with 0 Errors, 0 Warnings

All 6 projects build cleanly targeting `net10.0`:
- `Domain` → `net10.0`
- `Application` → `net10.0`
- `Infrastructure.Identity` → `net10.0`
- `Infrastructure.Persistence` → `net10.0`
- `Infrastructure.Shared` → `net10.0`
- `WebApi` → `net10.0`

---

## Changes Made

### Project File Updates

| Project | Change |
|---|---|
| `Domain.csproj` | `netstandard2.1` → `net10.0` |
| `Application.csproj` | `netstandard2.1` → `net10.0`; updated all packages; removed redundant `System.Text.Json` (NU1510) |
| `Infrastructure.Persistence.csproj` | Removed `netstandard2.0` TFM (NU1201/NU1202 — incompatible with EF Core 10.x and referenced projects); now `net10.0` only; pinned `System.Security.Cryptography.Pkcs` to `10.0.14` |
| `Infrastructure.Shared.csproj` | Removed `netstandard2.0` TFM (NU1201 — Application project does not support netstandard2.0); now `net10.0` only; updated MailKit/MimeKit |
| `Infrastructure.Identity.csproj` | Removed `System.IdentityModel.Tokens.Jwt 6.7.1` (NU1605 downgrade conflict with JwtBearer 10.x transitive dep 8.19.2); removed unused `MimeKit` reference; pinned `System.Security.Cryptography.Pkcs` to `10.0.14` |
| `WebApi.csproj` | Replaced deprecated `Microsoft.AspNetCore.Mvc.Versioning 4.1.1` with `Asp.Versioning.Mvc 8.1.0`; updated Swashbuckle to `6.9.0`; updated Serilog family; updated FluentValidation to `11.3.0`; removed `Microsoft.VisualStudio.Web.CodeGeneration.Design` (low-severity NuGet vulnerability from NuGet.Packaging/Protocol transitive deps, dev-only scaffolding tool not needed at runtime) |

### Package Version Updates

| Package | Old | New | Reason |
|---|---|---|---|
| `AutoMapper` | `10.0.0` | `16.2.0` | High-severity vulnerability GHSA-rvv3-g6hj-g44x; API update required |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | `8.0.1` | *(removed)* | Built into AutoMapper 12+ |
| `MediatR.Extensions.Microsoft.DependencyInjection` | `8.1.0` | *(removed)* | Merged into `MediatR` 12.x |
| `MediatR` | *(implicit)* | `12.4.0` | Explicit dependency; updated API |
| `FluentValidation` | `9.1.2` | `11.3.0` | .NET 10 compatibility |
| `FluentValidation.DependencyInjectionExtensions` | `9.1.2` | `11.3.0` | Matches FluentValidation |
| `FluentValidation.AspNetCore` | `9.1.2` | `11.3.0` | WebApi integration |
| `Microsoft.EntityFrameworkCore` | `3.1.7` | `10.0.14` | .NET 10 |
| `Microsoft.EntityFrameworkCore.InMemory` | `3.1.7` | `10.0.14` | .NET 10 |
| `MailKit` | `2.8.0` | `4.18.2` | Moderate-severity vulnerability GHSA-9j88-vvj5-vhgr |
| `MimeKit` | `2.9.1` | `4.18.2` | Moderate-severity vulnerability GHSA-g7hc-96xr-gvvx |
| `System.IdentityModel.Tokens.Jwt` | `6.7.1` | *(removed)* | Caused NU1605; now at `8.19.2` transitively via JwtBearer |
| `Swashbuckle.AspNetCore` | `5.5.1` | `6.9.0` | Moderate-severity vulnerability GHSA-qrmm-w75w-3wpx |
| `Microsoft.AspNetCore.Mvc.Versioning` | `4.1.1` | *(removed)* | Deprecated; replaced by `Asp.Versioning.Mvc 8.1.0` |
| `Asp.Versioning.Mvc` | *(new)* | `8.1.0` | Replacement for deprecated Mvc.Versioning |
| `Serilog.AspNetCore` | `3.4.0` | `8.0.3` | .NET 10 compatibility |
| `Serilog.Enrichers.Environment` | `2.1.3` | `3.0.1` | Updated |
| `Serilog.Enrichers.Process` | `2.0.1` | `3.0.0` | Updated |
| `Serilog.Enrichers.Thread` | `3.1.0` | `4.0.0` | Updated |
| `Serilog.Settings.Configuration` | `3.1.0` | `8.0.4` | Updated |
| `Serilog.Sinks.MSSqlServer` | `5.5.1` | `8.0.0` | Updated |
| `System.Security.Cryptography.Pkcs` | *(transitive 9.0.11)* | `10.0.14` | Pinned to fix GHSA-2fmv-rv2c-59b7 / GHSA-846f-f764-76mx in Identity and Persistence |

### Code Changes

| File | Change |
|---|---|
| `Application/ServiceExtensions.cs` | Updated `AddMediatR` to MediatR 12.x API: `cfg.RegisterServicesFromAssembly(...)`; updated `AddAutoMapper` to AutoMapper 16.x API: `cfg.AddMaps(...)` |
| `Application/Behaviours/ValidationBehaviour.cs` | Fixed `IPipelineBehavior.Handle` signature for MediatR 12.x (parameter order: `next` before `cancellationToken`) |
| `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs` | Removed `using Microsoft.EntityFrameworkCore.Internal` (internal API removed in EF Core 10.x) |
| `Application/DTOs/Account/ResetPasswordRequest.cs` | **Created** — was missing; referenced by `IAccountService`, `AccountService`, and `AccountController` |
| `Application/DTOs/Account/VerifyEmailRequest.cs` | **Fixed** — file was incorrectly containing `ResetPasswordRequest` class; replaced with correct `VerifyEmailRequest` class |
| `Infrastructure.Identity/Services/AccountService.cs` | Removed invalid `using Org.BouncyCastle.Ocsp` (no BouncyCastle dependency); removed `using System.Net.Cache`; removed `using Microsoft.AspNetCore.Mvc`; replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.Fill()` |
| `Infrastructure.Identity/ServiceExtensions.cs` | Removed invalid `using System.Reflection.Metadata.Ecma335`; replaced `Newtonsoft.Json.JsonConvert.SerializeObject` with `System.Text.Json.JsonSerializer.Serialize` (Newtonsoft.Json was not a declared dependency of Infrastructure.Identity) |
| `Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs` | Removed invalid `using Org.BouncyCastle.Crypto.Prng.Drbg` |
| `Infrastructure.Persistence/ServiceRegistration.cs` | Added `using Infrastructure.Persistence.Repositories` for `ProductRepositoryAsync` type resolution; removed unused `using` directives |
| `WebApi/Extensions/ServiceExtensions.cs` | Updated `using Microsoft.AspNetCore.Mvc` → `using Asp.Versioning` for `ApiVersion` type |
| `WebApi/Controllers/v1/ProductController.cs` | Added `using Asp.Versioning` and `using System.Threading.Tasks`; removed unused `using Application.Features.Products.Commands` (empty namespace) |

---

## Architecture Notes

- **netstandard2.0 targets removed** from `Infrastructure.Persistence` and `Infrastructure.Shared`: these projects reference packages (EF Core 10.x, ASP.NET Core Identity, MailKit 4.x) that are .NET 6+ only and do not support `netstandard2.0`. This satisfies the NU1201/NU1202 escape-hatch condition per migration rules.
- **Domain and Application** moved from `netstandard2.1` to `net10.0` because they transitively depend on `net10.0`-only packages (EF Core 10.x, MediatR 12.x, AutoMapper 16.x).
- The solution remains a clean monolith — no architectural changes were made to the CQRS/MediatR/Clean Architecture pattern.

---

## Next Steps

- **Database migrations**: The EF Core migrations (`Infrastructure.Persistence/Migrations/` and `Infrastructure.Identity/Migrations/`) were generated with EF Core 3.x and should be regenerated for EF Core 10.x if the schema needs to evolve. Existing migrations should still apply correctly for historical tracking. Run `dotnet ef migrations add Initial` only if a full schema reset is intended.
- **Connection strings**: `appsettings.json` still contains Windows-authentication SQL Server connection strings for a `DESKTOP-QCM5AL0` machine. Update for the target deployment environment (Linux/Docker/cloud).
- **JWT Key security**: The `JWTSettings:Key` in `appsettings.json` is a hardcoded plaintext value. Move secrets to environment variables or Azure Key Vault/AWS Secrets Manager before production deployment.
- **Swagger on .NET 10**: Swashbuckle 6.x is the last Swashbuckle series. Microsoft deprecated Swashbuckle in the ASP.NET Core 9.0 templates in favour of `Microsoft.AspNetCore.OpenApi` + `Scalar`. Migrating to the new built-in OpenAPI support is recommended for long-term maintenance but is a separate task.
- **FluentValidation.AspNetCore 11.x deprecation**: `FluentValidation.AspNetCore` is deprecated by the FluentValidation maintainer in favour of manual validation wiring. Consider migrating validators to use `FluentValidation.DependencyInjectionExtensions` with manual `IValidator<T>` injection in a future iteration.
- **EmailService uses synchronous Connect/Authenticate**: `Infrastructure.Shared/Services/EmailService.cs` calls `smtp.Connect(...)` and `smtp.Authenticate(...)` synchronously inside an async method. Consider using `await smtp.ConnectAsync(...)` and `await smtp.AuthenticateAsync(...)` for proper async IO.
