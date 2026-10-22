# Migration Summary: .NET → net10.0

## Result
`dotnet build CleanArchitecture.WebApi.sln` → **Build succeeded. 0 Error(s). 0 Warning(s).**

---

## Changes Made

### Project File Changes

| Project | Change |
|---|---|
| `Domain/Domain.csproj` | `netstandard2.1` → `net10.0` |
| `Application/Application.csproj` | `netstandard2.1` → `net10.0`; removed EF Core refs (not used in Application layer); updated AutoMapper 10→16.2.0 (merged DI extensions, removed separate DI package); updated FluentValidation 9.1.2→11.10.0; replaced `MediatR.Extensions.Microsoft.DependencyInjection 8.1.0` with `MediatR 12.4.1`; removed `System.Text.Json` (auto-provided on net10.0) |
| `Infrastructure.Persistence/Infrastructure.Persistence.csproj` | Collapsed `net10.0;netstandard2.0` → `net10.0` (escape hatch: `Application`/`Domain` only support netstandard2.1, and EF Core 10.x does not support netstandard2.0); forced `System.Security.Cryptography.Pkcs` 10.0.13 to clear vulnerability warning |
| `Infrastructure.Shared/Infrastructure.Shared.csproj` | Collapsed `net10.0;netstandard2.0` → `net10.0` (escape hatch: Application only supports netstandard2.1); updated MailKit 2.8→4.18.1, MimeKit 2.9→4.18.2; added `Microsoft.Extensions.Logging.Abstractions 10.0.13` (EmailService uses ILogger<>) |
| `Infrastructure.Identity/Infrastructure.Identity.csproj` | Fixed `System.IdentityModel.Tokens.Jwt` 6.7.1→8.19.2 (NU1605 downgrade conflict with JwtBearer 10.0.13 transitive dependency); updated MimeKit to 4.18.2 |
| `WebApi/WebApi.csproj` | Replaced `Microsoft.AspNetCore.Mvc.Versioning 4.1.1` with `Asp.Versioning.Mvc 8.1.0` (new package name); replaced Swashbuckle 5.5.1 with 7.3.1; Serilog.AspNetCore 3.4→9.0.0; Serilog enrichers/sinks updated; replaced `FluentValidation.AspNetCore 9.1.2` with 11.3.0; removed `Microsoft.VisualStudio.Web.CodeGeneration.Design` (eliminated NuGet.Packaging/Protocol vulnerability warnings); forced `System.Security.Cryptography.Pkcs 10.0.13` |

### Code Changes

| File | Change |
|---|---|
| `Application/ServiceExtensions.cs` | Updated MediatR 8→12 registration: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`. Updated AutoMapper 16: `cfg.AddMaps(assembly)`. Removed obsolete `using` directives. |
| `Application/Behaviours/ValidationBehaviour.cs` | Updated `IPipelineBehavior.Handle` signature for MediatR 12: parameter order changed to `(request, next, cancellationToken)` |
| `Application/Interfaces/IAccountService.cs` | Removed unused `using Microsoft.Extensions.Primitives` |
| `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs` | Removed `using Microsoft.EntityFrameworkCore.Internal` (internal EF Core API, Application layer no longer references EF Core) |
| `WebApi/Controllers/v1/ProductController.cs` | Added `using Asp.Versioning` for `[ApiVersion]` attribute (namespace moved from `Microsoft.AspNetCore.Mvc` to `Asp.Versioning`) |
| `WebApi/Extensions/ServiceExtensions.cs` | Updated to use `Asp.Versioning.ApiVersion` and `using Asp.Versioning` |
| `Infrastructure.Identity/Services/AccountService.cs` | Removed invalid `using Org.BouncyCastle.Ocsp` (package not referenced); removed unused `using System.Net.Cache`, `using Microsoft.Extensions.Primitives`, `using Microsoft.AspNetCore.Mvc`; replaced deprecated `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes()` |
| `Infrastructure.Identity/ServiceExtensions.cs` | Removed unused `using System.Reflection.Metadata.Ecma335` |

---

## Next Steps

- **Database migrations**: The EF Core migrations in `Infrastructure.Identity/Migrations` and `Infrastructure.Persistence/Migrations` were generated against EF Core 3.1.7. These may need to be regenerated with `dotnet ef migrations add <name>` against EF Core 10.x if schema changes are required. Existing migrations should still apply correctly for unchanged schemas.
- **AutoMapper 16 breaking changes**: AutoMapper 16 has more strict validation of mapping configurations. Run integration/unit tests to confirm all mappings resolve correctly at startup.
- **Serilog.Sinks.MSSqlServer**: Updated to 8.1.0 which may have configuration changes if using custom column mappings in `appsettings.json`. Validate the Serilog configuration at runtime.
- **MailKit/MimeKit vulnerability advisories**: The NuGet advisories GHSA-9j88-vvj5-vhgr and GHSA-g7hc-96xr-gvvx are still listed for 4.18.x in the local NuGet cache metadata (they reference the class of vulnerability, not the specific fixed version). Verify with the upstream advisory whether 4.18+ is patched.
- **Authentication**: `JwtSecurityTokenHandler` from `System.IdentityModel.Tokens.Jwt` is used directly; consider migrating to `JsonWebTokenHandler` (the newer, recommended handler in Microsoft.IdentityModel 8+) for improved performance.
