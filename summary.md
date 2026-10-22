# Migration Summary: .NET (netcoreapp3.1 / netstandard2.1) → net10.0

## Result

**Build succeeded — 0 errors, 0 warnings.**

```
dotnet build CleanArchitecture.WebApi.sln
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Projects migrated

| Project | Old TFM | New TFM |
|---|---|---|
| Domain | netstandard2.1 | net10.0 |
| Application | netstandard2.1 | net10.0 |
| Infrastructure.Persistence | netcoreapp3.1 | net10.0 |
| Infrastructure.Identity | netcoreapp3.1 | net10.0 |
| Infrastructure.Shared | netcoreapp3.1 | net10.0 |
| WebApi | netcoreapp3.1 | net10.0 |

All six project files are SDK-style (`Microsoft.NET.Sdk` / `Microsoft.NET.Sdk.Web`).

---

## Key changes applied (by ATX + this cycle)

### Project files
- All `.csproj` files converted to SDK-style and retargeted to `net10.0`.
- Legacy `packages.config`, `Web.config`-based configuration, `Global.asax`, and `AssemblyInfo.cs` artifacts removed or replaced.

### NuGet packages
- **EF Core**: `Microsoft.EntityFrameworkCore` and related packages upgraded to `10.0.13`.
- **ASP.NET Core Identity / JWT**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer` → `10.0.13`; `System.IdentityModel.Tokens.Jwt` → `8.23.0`.
- **Swashbuckle**: `Swashbuckle.AspNetCore` → `10.3.1` (OpenAPI v3 native in .NET 10).
- **Serilog**: `Serilog.AspNetCore` → `10.0.0` and related enrichers/sinks updated.
- **MediatR**: `MediatR.Extensions.Microsoft.DependencyInjection` `11.1.0` (last version with `AddMediatR(Assembly)` overload; kept to avoid breaking the call site).
- **AutoMapper**: `15.1.3`.
- **FluentValidation**: `FluentValidation` `11.12.0`, `FluentValidation.AspNetCore` `11.3.1`, `FluentValidation.DependencyInjectionExtensions` `11.12.0`.
- **MailKit / MimeKit**: `MailKit` `4.18.1`, `MimeKit` `4.18.2`.
- **API Versioning**: `Microsoft.AspNetCore.Mvc.Versioning` `5.1.0`.

### Vulnerability remediation (this cycle)
- `NuGet.Packaging` and `NuGet.Protocol` were pulled in transitively at version `6.12.1` by `Microsoft.VisualStudio.Web.CodeGeneration.Design`, triggering **NU1901** (low-severity, GHSA-g4vj-cjjj-v7hg).
- Fixed by adding explicit `PrivateAssets=all` overrides in `WebApi.csproj` that pin both packages to `6.13.1`, which does not carry the advisory. This cleared all 4 warnings and leaves the build at **0 warnings, 0 errors**.

### Application layer
- `Startup.cs` retained (still valid in .NET 10 with `IWebHostBuilder`) — no forced migration to minimal hosting API needed.
- `Program.cs` uses `CreateHostBuilder` / `UseSerilog` pattern, unchanged from the ATX output.
- Error handling: `UseExceptionHandler("/Error")` backed by a terminal `MapGet("/Error", ...)` endpoint returning `ProblemDetails` (replaces the old custom exception-handler middleware-only approach).
- Swagger: `AddSecurityRequirement` updated to use the `OpenApiSecuritySchemeReference` constructor introduced in Swashbuckle 10.x.

### Configuration
- `appsettings.json` retains all settings (JWTSettings, MailSettings, Serilog, ConnectionStrings). No `Web.config` remains.

### Authentication / Identity
- JWT Bearer auth, ASP.NET Core Identity, and `AccountService` fully migrated to ASP.NET Core patterns.
- `System.Net.Cache` import in `AccountService.cs` is unused but harmless — it resolves via `System.Net` in net10.0.

---

## Next steps

- **Connection strings**: `appsettings.json` still references a Windows SQL Server instance (`DESKTOP-QCM5AL0`). For Linux/container deployments, update to a reachable SQL Server or set `UseInMemoryDatabase: true` for development.
- **EF Core migrations**: The existing migrations target `netcoreapp3.1`-era EF Core. If the SQL Server schema has drifted, run `dotnet ef migrations add ...` against the current model before deploying.
- **MediatR 12 upgrade** (optional): The project uses `MediatR.Extensions.Microsoft.DependencyInjection` 11.1.0. MediatR 12 removes this package in favour of the built-in `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` API. Upgrading is straightforward but requires updating `Application/ServiceExtensions.cs`.
- **Serilog SQL Server sink**: `Serilog.Sinks.MSSqlServer` is referenced but `appsettings.json` only configures the Console sink. Confirm the sink is intentionally included or remove the package reference.
- **`Microsoft.VisualStudio.Web.CodeGeneration.Design`**: This is a scaffolding-only tool. It can be moved to a `<PrivateAssets>all</PrivateAssets>` design-time reference or removed entirely if scaffolding is not used in CI.
