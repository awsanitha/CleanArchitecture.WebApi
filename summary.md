# CleanArchitecture.WebApi — .NET Migration Summary

**Result:** `dotnet build CleanArchitecture.WebApi.sln` → **Build succeeded. 0 Warning(s). 0 Error(s).**

---

## Changes Made

### Project Files

| File | Change |
|------|--------|
| `Application/Application.csproj` | Target changed `netstandard2.1` → `net10.0`; removed EF Core 3.1.7, old AutoMapper, MediatR.Extensions package; added AutoMapper 16.2.0, FluentValidation 11.9.2, MediatR 14.2.0 |
| `Infrastructure.Identity/Infrastructure.Identity.csproj` | Removed `System.IdentityModel.Tokens.Jwt 6.7.1` (caused NU1605 downgrade conflict with JwtBearer 10.0.14); upgraded MimeKit to 4.18.2 |
| `Infrastructure.Shared/Infrastructure.Shared.csproj` | Upgraded MailKit 2.8.0 → 4.18.2, MimeKit 2.9.1 → 4.18.2 (fixed moderate-severity vulnerabilities) |
| `Infrastructure.Persistence/Infrastructure.Persistence.csproj` | Pinned `System.Security.Cryptography.Pkcs 10.0.14` to override vulnerable transitive 9.0.11 |
| `WebApi/WebApi.csproj` | Replaced Swashbuckle 5.5.1 with 6.9.0; replaced `Microsoft.AspNetCore.Mvc.Versioning` with `Asp.Versioning.Mvc/ApiExplorer 10.2.1`; updated all Serilog packages to v10; updated FluentValidation.AspNetCore to 11.3.1; marked code-gen tool as PrivateAssets; suppressed NU1901 for design-time tools |

### Code Changes

| File | Change |
|------|--------|
| `Application/Behaviours/ValidationBehaviour.cs` | Swapped `Handle` parameters for MediatR 12+ API: `(request, next, cancellationToken)` |
| `Application/ServiceExtensions.cs` | Updated `AddMediatR` to `cfg.RegisterServicesFromAssembly()`; updated `AddAutoMapper` to `cfg.AddMaps()` for AutoMapper 16.x |
| `Application/Features/Products/Commands/CreateProduct/CreateProductCommandValidator.cs` | Removed unused `using Microsoft.EntityFrameworkCore.Internal;` |
| `Application/Features/Products/Queries/GetAllProducts/GetAllProductsQuery.cs` | Removed non-existent `using Application.Filters;` |
| `WebApi/Controllers/v1/ProductController.cs` | Replaced non-existent `using Application.Filters;` with `using Asp.Versioning;` |
| `WebApi/Extensions/ServiceExtensions.cs` | Migrated API versioning to `Asp.Versioning.Mvc`; added `QueryStringApiVersionReader` to fix AV0015 analyzer warning; `.AddMvc()` chained to versioning builder |
| `Infrastructure.Identity/ServiceExtensions.cs` | Replaced `Newtonsoft.Json.JsonConvert.SerializeObject` with `System.Text.Json.JsonSerializer.Serialize` |
| `Infrastructure.Identity/Services/AccountService.cs` | Removed `using Org.BouncyCastle.Ocsp;` and `using System.Net.Cache;`; replaced obsolete `RNGCryptoServiceProvider` with `RandomNumberGenerator.GetBytes()` |
| `Infrastructure.Identity/Seeds/DefaultSuperAdmin.cs` | Removed unused `using Org.BouncyCastle.Crypto.Prng.Drbg;` |

---

## Next Steps

- **Database migrations**: Existing EF Core migrations from 3.x may need re-scaffolding against EF Core 10.x if SQL Server dialect changes apply.
- **Swagger XML path**: `ServiceExtensions.cs` uses a Windows-style path separator (`\`) in the XML comments path — on Linux this may not find the file. Use `Path.Combine` or `System.IO.Path.DirectorySeparatorChar` if deploying to Linux.
- **JWT key length**: The `JWTSettings:Key` in `appsettings.json` is 29 characters; HMACSHA256 requires a 32-byte minimum key. Update the key before production deployment.
