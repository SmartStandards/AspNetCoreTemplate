---
name: most-important-details-for-smartstandards-aspnetcore-template
description: Mandatory compact entry skill for SmartStandards.AspNetCoreTemplate and its package SmartStandards.WebApiDependencies (.NET 10) - Program.Main startup pattern, LaunchProfileHelper (applying a launchSettings.json profile when an EXE is started directly with "LaunchProfile=<name>"), branch-specific appsettings files, and the SmartStandardsCheapInitializer. Use before changing code in this repository, and when writing or debugging Program.Main of a SmartStandards-style ASP.NET Core Web API, when a service behaves differently started from Visual Studio vs. as EXE (e.g. falls back to port 5000), or when appsettings.<Branch>.json files are involved.
---

# Most important details for SmartStandards.AspNetCoreTemplate

Verified against the source at package version 10.0.14 (2026-10-19; source read, no runtime test).

## Artifacts

| Artifact | Content |
| --- | --- |
| `dotnet/src/AspNetCoreTemplate` | Reference Web API; its `Program.cs` is the canonical startup pattern. |
| `dotnet/src/WebApiDependencies.CheapInitializer` | Package `SmartStandards.WebApiDependencies` (consumed as `SmartStandards.WebApiDependencies.net10.0`), assembly `SmartStandards.WebApiDependencies.CheapInitializer`, namespace `Microsoft.AspNetCore`. `net10.0` only; requires ASP.NET Core. |
| `dotnet/src/AspNetCoreTemplate.Demo`, `AspNetCoreTemplateOLD`, `WebView2Template` | Demo, previous template and WebView2 template; not the reference pattern. |

Classes: `LaunchProfileHelper` and `SmartStandardsLaunchProfileExtensions` (file `SmartStandardsLaunchProfileExtensions.cs`), `SmartStandardsCheapInitializer`, `UjmwSwaggerSetupExtensions`, `SmartStandardsSwaggerGenOptionsConfigurator`, `SmartStandardsSwaggerStartupFilter`, `SmartStandardsAuthTokenConfigDocumentFilter` (file `SwaggerSetupExtensions.cs`).

## Why Launch Profiles Need a Helper

Visual Studio applies the selected profile of `Properties/launchSettings.json` (URLs, environment); IIS uses `web.config`. A directly started EXE ignores `launchSettings.json` and falls back to ASP.NET Core defaults (e.g. `http://localhost:5000`, environment `Production`). With the helper, a launcher or developer passes `LaunchProfile=<name>` on the command line and the application applies that profile itself while building the host.

## Startup Pattern (`Program.Main`)

The order matters: the environment must be known before `CreateBuilder`, the branch files before the configuration is consumed.

```csharp
string explicitLaunchProfile = null;
LaunchProfileHelper launchProfileHelper;
if (LaunchProfileHelper.TryPickLaunchProfileFromCommandlineArgs(args, out explicitLaunchProfile)) {
  launchProfileHelper = LaunchProfileHelper.CreateForCurrentLaunchsettingsJson();
}
else {
  launchProfileHelper = LaunchProfileHelper.CreateEmpty();
}

WebApplicationBuilder builder;
if (launchProfileHelper.TryGetAspEnvironmentNameFromLaunchProfile(explicitLaunchProfile, out string aspEnvironmentName)) {
  builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, EnvironmentName = aspEnvironmentName });
}
else {
  builder = WebApplication.CreateBuilder(args);
}

builder.AddBranchSpecificConfigurationFiles();

if (launchProfileHelper.TryGetUrlsFromLaunchProfile(explicitLaunchProfile, out string[] explicitUrls)) {
  builder.WebHost.UseUrls(explicitUrls);
}
```

Shortcut for URLs only: `webHostBuilder.UseUrlsFromLaunchProfileIfRequested(args)` (see pitfalls).

## LaunchProfileHelper Behavior

- **Argument:** the first argument starting with `LaunchProfile=` (case-insensitive) wins. An empty value counts as "not given".
- **Profile selection:** by name (case-insensitive) or, if the value is an integer, by index in the `profiles` section (`LaunchProfile=0` = first profile). An unknown name or an index out of range throws (fail-fast).
- **File lookup:** `launchSettings.json` is searched relative to the directory of the helper assembly (normally the output directory): `./`, `./Properties/`, `../Properties`, `../../Properties`. Set `Properties/launchSettings.json` to be copied to the output directory. If a profile was requested and no file is found, startup throws.
- **Applied values - only these two:** the environment name from `environmentVariables.ASPNETCORE_ENVIRONMENT` (fallback `DOTNET_ENVIRONMENT`) and the listening URLs from `applicationUrl` (split at `;`).
- **Not applied:** `commandName`, `launchBrowser`, `launchUrl`, `iisSettings`, and all other `environmentVariables` of the profile.
- **No argument:** nothing is changed; ASP.NET Core defaults apply.

## Branch-Specific Configuration Files

`builder.AddBranchSpecificConfigurationFiles()` determines the active Git branch (`git rev-parse --abbrev-ref HEAD`, run in the directory of the helper assembly) and adds, both optional, `appsettings.<Branch>.json` and `appsettings.<Environment>.<Branch>.json`.

Precedence (later wins): `appsettings.json` → `appsettings.<Environment>.json` → (user secrets, environment variables, command-line args) → `appsettings.<Branch>.json` → `appsettings.<Environment>.<Branch>.json`.

- Branch files are appended after `CreateBuilder`, so they **override environment variables and command-line arguments**.
- Without Git (not installed, not in `PATH`, no repository - the normal case on deployed servers) nothing is added; branch files only affect developer checkouts.
- Branch names are used verbatim: `feature/x` yields a path into a subfolder (effectively unusable); a detached HEAD yields `appsettings.HEAD.json`.
- File paths are relative to the content root.

## SmartStandardsCheapInitializer (Overview)

`SmartStandardsCheapInitializer.OnConfigureServices` / `OnRunApplication` provide a default wiring: SmartStandards logging, controllers, UJMW (`UseCombinedDynamicAssembly`), SmartStandards-flavored Swagger (`AddSwaggerGenSmartStandardsFlavored`), static files, SmartAmbience `UseAmbientFieldAdapterMiddleware`, developer exception page unless setting `ProdMode` is `true`, HTTPS redirection, permissive CORS, authorization, `MapControllers`.

## Swagger Security per API Group (`AddSwaggerGenSmartStandardsFlavored`)

Two overloads; details and requirements in `doc/Swagger-Security-per-ApiGroup.md`. Origin: section 6 of
`AccessTokenManager-Refactoring.md` in the AuthTokenHandling repository.

- `AddSwaggerGenSmartStandardsFlavored(Func<string, string> oAuthUrlResolver = null)` - legacy/fallback, unchanged:
  global `Bearer` (ApiKey header) plus OAuth implicit flow from the config keys `OAuthClientIdForSwaggerUi`,
  `OAuthAuthorizeUrlForSwaggerUi`, `OAuthScopeExpressionForSwaggerUi`. Used by the CheapInitializer.
- `AddSwaggerGenSmartStandardsFlavored(Func<string, KeyValuePair<string, AuthTokenConfig>[]> authTokenConfigResolver)` -
  delegate path (apiGroupName -> token source id + config; intended implementation: static method of
  `AccessTokenManager`). Config keys above are ignored. Per document (= API group): one security scheme per token
  source (scheme name = token source id) plus one requirement per token source (alternatives).
- `AddSwaggerGenSmartStandardsFlavored(AuthTokenConfig authTokenConfig)` - convenience: one instance for all groups,
  token source id `default` (`UjmwSwaggerSetupExtensions.DefaultTokenSourceId`); delegates to the resolver overload.
  The instance is captured by reference, later property changes are reflected.
- `IssueMode` mapping: `OAUTH_CIBA_CODEGRAND[_HTTPGETONLY]` -> `authorizationCode` (`AuthEndpointUrl`,
  `RetrieveEndpointUrl` as token URL); `OAUTH_IMPLICIT_FLOW` -> `implicit`; others -> HTTP `bearer`. Missing URLs throw.
- `{RequestBaseUrl}` in flow URLs is replaced per request (`PreSerializeFilters`) with `scheme://host[:port]` plus
  path base, no trailing slash; plain, case-sensitive.
- Swagger UI: client id = first non-empty `ClientId` of an OAuth config (only one global client id possible);
  `OAuthUsePkce()` if any code flow exists.
- Late evaluation (avoids initialization order problems): the resolver is never called while services or the pipeline
  are configured. Documents: per request of `docs/schema/<group>.json` (document filter). Swagger UI client id / PKCE:
  per (re)load of the UI (`/docs`, `/docs/`, `/docs/index.html`, `/docs/index.js`) via a small middleware that updates
  the captured `SwaggerUIOptions.OAuthConfigObject`; this works because `SwaggerUIMiddleware` (Swashbuckle 10.1.7)
  serializes its options per request without caching (verified via reflection, no internal cache field). Re-check
  this when upgrading Swashbuckle.
- Compatibility rules (keep!): never change the legacy overload or its output; no optional parameters on the new
  overload. Only source-level edge case: a literal `AddSwaggerGenSmartStandardsFlavored(null)` is ambiguous (CS0121).
- Scopes are transported via `AuthTokenConfig.Claims["scope"]` (if `Claims` is not null and contains the key; value
  split by blank). They fill the `scopes` of the OAuth flow (label `Scope: <scope>`) and the requirement of OAuth2
  schemes; Bearer schemes get no scopes (OpenAPI 3.0). Values are used literally (no prefix handling, no
  placeholder resolution).

## Known Pitfalls

- `CreateForCurrentLaunchsettingsJson(throwIfNotFound: false)` still throws when no file is found, because the internal lookup `TryFindLaunchSettingsFile` throws instead of returning `false`. This also affects `UseUrlsFromLaunchProfileIfRequested`. Known defect in 10.0.14; fix it here, do not work around it in consuming code.
- The helper writes probe paths to the console during lookup.

## Related Skills

- `ujmw`, `smart-ambience`, `smartstandards-logging` (components wired by the CheapInitializer).
- Consumer-specific usage (e.g. mandatory launch profiles, launcher tools) belongs in consumer-owned skill collections and only references this skill.
