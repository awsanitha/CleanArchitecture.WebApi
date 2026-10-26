# Migration Summary: .NET Framework → net10.0

## Status
**COMPLETE** — `dotnet build CleanArchitecture.WebApi.sln` exits with **0 errors, 0 warnings**.

---

## What Was Done

### Starting Point
AWS Transform Custom CLI (ATX) ran first and completed the bulk of the migration:
- All 5 non-domain projects upgraded to SDK-style `.csproj` targeting `net10.0`
- Legacy artifacts removed: `packages.config`, `Web.config`, `Global.asax`
- `System.Web.*` / EF6 / `System.Web.Http.*` references eliminated
- `Program.cs` rewritten as a minimal-hosting-model WebApplication bootstrap
- All NuGet packages updated to net10.0-compatible versions
- `Swashbuckle.AspNetCore` 10.x wired up with the new `OpenApiSecuritySchemeReference` API
- `Asp.Versioning.Mvc` 10.x configured with `.AddMvc().AddApiExplorer()`
- JWT Bearer authentication migrated to `Microsoft.AspNetCore.Authentication.JwtBearer` 10.x
- EF Core 10 in-memory and SQL Server providers in place
- Serilog 10 structured logging wired into `builder.Host.UseSerilog()`
- `/Error` terminal handler added to correctly return 500 ProblemDetails (fixes a latent 404 defect in the original `netcoreapp3.1` app where `UseExceptionHandler("/Error")` was configured but no `/Error` route existed)

### Kiro CLI Finishing Pass

#### Package hygiene — security pinning
ATX added three packages as explicit version pins to override vulnerable transitive versions pulled in by `Microsoft.VisualStudio.Web.CodeGeneration.Design`:
- `NuGet.Packaging` → pinned to 7.9.0 (transitive 6.12.1 had GHSA-g4vj-cjjj-v7hg low severity)
- `NuGet.Protocol` → pinned to 7.9.0 (same advisory)
- `System.Security.Cryptography.Pkcs` → pinned to 10.0.14 across WebApi, Infrastructure.Identity, and Infrastructure.Persistence (transitive 9.0.11 had GHSA-2fmv-rv2c-59x7 moderate and GHSA-846f-f764-76mx high severity)

These pins were preserved and redistributed to ensure every project that resolves the transitive dependency gets the patched version.

#### AV0015 — explicit ApiVersionReader
`WebApi/Extensions/ServiceExtensions.cs` now sets `config.ApiVersionReader = new UrlSegmentApiVersionReader()` in `AddApiVersioningExtension`. This matches the URL-segment versioning pattern already used by `BaseApiController` (`/api/v{version:apiVersion}/[controller]`) and eliminates the `AV0015` performance diagnostic.

---

## Final Project Targets

| Project | Target |
|---|---|
| Domain | netstandard2.1 (intentional — keeps domain portable; fully compatible with net10.0 consumers) |
| Application | net10.0 |
| Infrastructure.Persistence | net10.0 |
| Infrastructure.Identity | net10.0 |
| Infrastructure.Shared | net10.0 |
| WebApi | net10.0 |

---

## Key Package Versions

| Package | Version | Notes |
|---|---|---|
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.14 | |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.14 | |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.14 | |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.14 | |
| Swashbuckle.AspNetCore | 10.3.1 | |
| Asp.Versioning.Mvc | 10.2.1 | |
| Serilog.AspNetCore | 10.0.0 | |
| System.IdentityModel.Tokens.Jwt | 8.23.0 | |
| AutoMapper | 16.2.0 | |
| MediatR.Extensions.Microsoft.DependencyInjection | 11.1.0 | |
| FluentValidation.AspNetCore | 11.3.1 | |
| MailKit / MimeKit | 4.18.2 | replaces SmtpClient |

---

## Next Steps

- **Database migrations**: The EF Core migrations in `Infrastructure.Identity` and `Infrastructure.Persistence` were generated against `netcoreapp3.1`. They should be validated against the net10.0 runtime by running `dotnet ef database update` in a dev environment with a live SQL Server instance, or regenerated if the schema has changed.
- **Connection strings**: `appsettings.json` still references `DESKTOP-QCM5AL0` (a Windows developer machine). Update for the target deployment environment.
- **JWT Key strength**: `JWTSettings:Key` in `appsettings.json` is a short, low-entropy string (`C1CF4B7DC4C4175B6618DE4F55CA4`). Replace with a cryptographically random 256-bit key before any production deployment. Consider using environment variables or a secrets manager.
- **Email service**: `AccountService` sends verification emails with a hardcoded `From` address. Configure the `MailSettings` section in `appsettings.json` for the target environment.
- **`MediatR.Extensions.Microsoft.DependencyInjection` 11.1.0**: This package is deprecated in favour of the built-in `MediatR` 12+ DI registration (`services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`). Migration is non-breaking and straightforward, but was not performed here to avoid changing the application layer unnecessarily.
- **Domain project target**: Domain is on `netstandard2.1` which is fully compatible with net10.0. If the team prefers uniformity, it can be changed to `net10.0` without any code changes.
