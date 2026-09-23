# .NET Framework → .NET 10 Migration Summary

## Result
`dotnet build CleanArchitecture.WebApi.sln` exits with **0 errors, 0 warnings**.

All 6 projects compile cleanly targeting `net10.0`.

---

## Changes Made

### Project Files

| Project | Change |
|---|---|
| `Domain/Domain.csproj` | `netstandard2.1` → `net10.0` |
| `Application/Application.csproj` | `netstandard2.1` → `net10.0`; replaced `MediatR.Extensions.Microsoft.DependencyInjection 8.1.0` (deprecated) with `MediatR 12.4.1`; upgraded `AutoMapper 10.0.0` → `16.2.0` (DI extensions now built-in, removed separate `AutoMapper.Extensions.Microsoft.DependencyInjection`); upgraded `FluentValidation` → `11.11.0`; upgraded EF Core → `10.0.12`; removed redundant `System.Text.Json` (auto-available in net10.0) |
| `Infrastructure.Identity/Infrastructure.Identity.csproj` | Upgraded `System.IdentityModel.Tokens.Jwt 6.7.1` → `8.19.2` (was causing NU1605 package downgrade error); upgraded `MimeKit` → `4.18.1` |
| `Infrastructure.Shared/Infrastructure.Shared.csproj` | Upgraded `MailKit 2.8.0` → `4.18.0`; upgraded `MimeKit 2.9.1` → `4.18.1` |
| `WebApi/WebApi.csproj` | Replaced `Microsoft.AspNetCore.Mvc.Versioning 4.1.1` (deprecated) with `Asp.Versioning.Mvc 8.1.0`; replaced `Swashbuckle.AspNetCore 5.5.1`/`Swashbuckle.AspNetCore.Swagger 5.5.1` with `Swashbuckle.AspNetCore 7.2.0`; upgraded all Serilog packages to current versions (AspNetCore 8.0.3, Enrichers 3/4.x, Settings 8.0.4, Sinks.MSSqlServer 8.0.0); upgraded `FluentValidation.AspNetCore` → `11.3.0`; marked `Microsoft.VisualStudio.Web.CodeGeneration.Design` as private/build-only; added `NuGetAuditSuppress` for transitive NuGet.Packaging/Protocol advisory (GHSA-g4vj-cjjj-v7hg, low severity, from dev-tool dependency) |

### Code Changes

| File | Change |
|---|---|
| `Infrastructure.Identity/Services/AccountService.cs` | Removed invalid imports (`Org.BouncyCastle.Ocsp`, `System.Net.Cache`, `Microsoft.AspNetCore.Mvc`, `System.Reflection.Metadata.Ecma335`, `Microsoft.Extensions.Primitives`); replaced deprecated `RNGCryptoServiceProvider` with `RandomNumberGenerator.Fill()` (modern, no disposal needed) |
| `Infrastructure.Identity/ServiceExtensions.cs` | Removed unused `using System.Reflection.Metadata.Ecma335` |
| `Infrastructure.Shared/Services/DateTimeService.cs` | Removed unused `using System.Reflection.Metadata.Ecma335`, `System.Collections.Generic`, `System.Text` |
| `Application/ServiceExtensions.cs` | Updated MediatR registration from `services.AddMediatR(Assembly)` (MediatR 8.x API) to `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly))` (MediatR 12.x API); updated AutoMapper registration from `services.AddAutoMapper(Assembly)` to `services.AddAutoMapper(cfg => cfg.AddMaps(Assembly))` (AutoMapper 16.x API) |
| `Application/Behaviours/ValidationBehaviour.cs` | Updated `IPipelineBehavior.Handle` signature: MediatR 12.x moved `CancellationToken` parameter after `RequestHandlerDelegate<TResponse> next` |
| `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs` | Removed unused and inaccessible `using Microsoft.EntityFrameworkCore.Internal` |
| `WebApi/Extensions/ServiceExtensions.cs` | Replaced `using Microsoft.AspNetCore.Mvc` (for `ApiVersion`) with `using Asp.Versioning` to match the new `Asp.Versioning.Mvc` package |
| `WebApi/Controllers/v1/ProductController.cs` | Added `using Asp.Versioning` for `[ApiVersion]` attribute |

---

## Next Steps

- **AutoMapper mapping validation**: AutoMapper 16.x may be stricter about unmapped properties. Run integration tests to verify all `CreateMap<>` profiles in `Application/Mappings/GeneralProfile.cs` produce correct results at runtime.
- **FluentValidation auto-validation**: `FluentValidation.AspNetCore 11.x` removed the automatic ASP.NET Core model-binding validation integration (`AddFluentValidation`). The current code relies solely on the MediatR pipeline behavior (`ValidationBehavior`) for validation — this is already the correct pattern and no change is needed.
- **API versioning URL**: The route template `api/v{version:apiVersion}/[controller]` in `BaseApiController` will now be resolved by `Asp.Versioning.Mvc 8.x`. Verify endpoints are accessible at `/api/v1/product`.
- **Database migrations**: Existing EF Core Identity migrations target the `net10.0` runtime; verify they apply correctly against a real SQL Server instance.
- **JWT key configuration**: `appsettings.json` must have `JWTSettings:Key` of at least 256 bits (32 bytes) for HMAC-SHA256; `Microsoft.IdentityModel.Tokens 8.x` enforces this strictly.
