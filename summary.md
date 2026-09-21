# Migration Summary — CleanArchitecture.WebApi (.NET → net10.0)

## Build Result
`dotnet build CleanArchitecture.WebApi.sln` → **Build succeeded. 0 Warning(s). 0 Error(s).**

## Changes Made

### Project Files
| File | Change |
|------|--------|
| `Domain/Domain.csproj` | `netstandard2.1` → `net10.0` |
| `Application/Application.csproj` | `netstandard2.1` → `net10.0`; upgraded EF Core 3.1.7 → 10.0.12; replaced MediatR 8.x extensions package with `MediatR` 12.4.1; upgraded AutoMapper 10 → 16.2.0 (removed separate DI extensions package, now built-in); upgraded FluentValidation 9.1.2 → 11.3.0; removed explicit `System.Text.Json` pin (built into .NET 10) |
| `Infrastructure.Identity/Infrastructure.Identity.csproj` | Removed pinned `System.IdentityModel.Tokens.Jwt 6.7.1` (was causing NU1605 downgrade error vs JwtBearer 10.x transitive dependency); upgraded `MimeKit` 2.9.1 → 4.18.1 |
| `Infrastructure.Shared/Infrastructure.Shared.csproj` | Upgraded `MailKit` 2.8.0 → 4.18.0; `MimeKit` 2.9.1 → 4.18.1 |
| `WebApi/WebApi.csproj` | Replaced `Microsoft.AspNetCore.Mvc.Versioning` 4.1.1 with `Asp.Versioning.Mvc` 8.1.0 + `Asp.Versioning.Mvc.ApiExplorer` 8.1.0; upgraded `Swashbuckle.AspNetCore` 5.5.1 → 7.3.1; upgraded Serilog stack to current versions; upgraded `FluentValidation.AspNetCore` 9.1.2 → 11.3.0; removed `Microsoft.VisualStudio.Web.CodeGeneration.Design` (build tool not needed); removed `Swashbuckle.AspNetCore.Swagger` (included in main Swashbuckle package) |

### Code Fixes
| File | Change |
|------|--------|
| `Application/Behaviours/ValidationBehaviour.cs` | Updated `IPipelineBehavior` constraint from `where TRequest : IRequest<TResponse>` to `where TRequest : notnull` (MediatR 12 requirement); swapped `Handle` parameter order to `(request, next, cancellationToken)` |
| `Application/ServiceExtensions.cs` | Updated `AddMediatR(Assembly)` to MediatR 12 API: `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`; updated `AddAutoMapper` to AutoMapper 16.x API: `cfg => cfg.AddMaps(assembly)` |
| `Infrastructure.Identity/Services/AccountService.cs` | Removed dead `using Org.BouncyCastle.Ocsp;` and `using System.Net.Cache;`; replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes` |
| `Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs` | Removed dead `using Org.BouncyCastle.Crypto.Prng.Drbg;` |
| `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs` | Removed `using Microsoft.EntityFrameworkCore.Internal;` (internal EF Core API, not needed) |
| `WebApi/Extensions/ServiceExtensions.cs` | Added `using Asp.Versioning;`; chained `.AddMvc()` on `AddApiVersioning()` per new package API |
| `WebApi/Controllers/v1/ProductController.cs` | Added `using Asp.Versioning;` so `[ApiVersion]` attribute resolves from the new namespace |

## Next Steps
- The `.github/workflows/dotnet-core.yml` CI workflow still specifies `dotnet-version: 3.1.301` — update to `10.0.x` to match the target framework.
- SQL Server connection strings in `appsettings.json` point to `DESKTOP-QCM5AL0` (a developer machine) — update for real deployment environments.
- EF Core migrations under `Infrastructure.Identity/Migrations/` and `Infrastructure.Persistence/Migrations/` reference `ProductVersion = "3.1.5/3.1.6"`. While they build fine (EF Core is forward-compatible for migrations), regenerating them with `dotnet ef migrations add` against EF Core 10 would be cleaner. Two Persistence migrations are still excluded from compile via `<Compile Remove=...>` — review whether they should be deleted or regenerated.
- The `VerifyEmailRequest.cs` file actually defines `ResetPasswordRequest` (file name mismatch) — low-risk cosmetic issue.
