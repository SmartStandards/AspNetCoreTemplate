# Swagger Security per API Group

## Abstract / Motivation

A Swagger UI should know, per API group, which authentication it offers - without a manual `securitySchemes` setup
and without proprietary token source ids in contracts or controllers. AuthTokenHandling describes every token source
with an `AuthTokenConfig` (issuing and validation). `SmartStandards.WebApiDependencies` therefore accepts a resolver
`apiGroupName -> token source id + AuthTokenConfig` and derives the OpenAPI security information from it.

Origin of the requirements: section 6 ("Tasks: SmartStandards.AspNetCoreTemplate") of
`AccessTokenManager-Refactoring.md` in the AuthTokenHandling repository (phase 2 of the implementation order).

Compared to the previous approach (one global `Bearer` definition plus one OAuth definition from config keys), the
delegate path delivers a precise, per-group description, supports several token sources per group and works with the
self-describing configuration of the Cheap OAuth Server (including the `{RequestBaseUrl}` placeholder).

## Getting Started

```csharp
// legacy / fallback (unchanged): global Bearer + OAuth from config keys
services.AddSwaggerGenSmartStandardsFlavored();

// convenience: one AuthTokenConfig instance for all API groups (token source id 'default')
services.AddSwaggerGenSmartStandardsFlavored(myDefaultAuthTokenConfig);

// delegate path: security per API group, derived from AuthTokenConfigs
services.AddSwaggerGenSmartStandardsFlavored(
	(string apiGroupName) => {
		return new KeyValuePair<string, AuthTokenConfig>[] {
			new KeyValuePair<string, AuthTokenConfig>("default", myDefaultAuthTokenConfig)
		};
	}
);
```

As soon as AuthTokenHandling provides it, the static Swagger bridge of `AccessTokenManager` is passed as resolver.

## Advanced Example

An API group offering two token sources: an OAuth code flow server (self-describing, relative to the host) and a raw
token input for technical callers.

```csharp
AuthTokenConfig oAuthServer = new AuthTokenConfig();
oAuthServer.DisplayLabel = "Login";
oAuthServer.IssueMode = WellknownIssuingModes.OAUTH_CIBA_CODEGRAND;
oAuthServer.ClientId = "swagger-ui";
oAuthServer.AuthEndpointUrl = "{RequestBaseUrl}/oauth2/authorize";
oAuthServer.RetrieveEndpointUrl = "{RequestBaseUrl}/oauth2/token";
oAuthServer.Claims = new Dictionary<string, string>();
oAuthServer.Claims["scope"] = "API:mcp API:read";

AuthTokenConfig rawInput = new AuthTokenConfig();
rawInput.DisplayLabel = "API-TOKEN";
rawInput.IssueMode = WellknownIssuingModes.RAW_INPUT;

services.AddSwaggerGenSmartStandardsFlavored(
	(string apiGroupName) => {
		return new KeyValuePair<string, AuthTokenConfig>[] {
			new KeyValuePair<string, AuthTokenConfig>("default", oAuthServer),
			new KeyValuePair<string, AuthTokenConfig>("technical", rawInput)
		};
	}
);
```

Result: the document of the group contains the schemes `default` (OAuth2 `authorizationCode` with the scopes `API:mcp`
and `API:read`, URLs resolved against the requesting host) and `technical` (HTTP `bearer`); the Swagger UI uses client
id `swagger-ui` with PKCE.

## Requirements

| # | Requirement | Implementation |
| --- | --- | --- |
| 1 | New overload accepting `Func<string, KeyValuePair<string, AuthTokenConfig>[]>`; existing signature `(IServiceCollection, Func<string, string> oAuthUrlResolver = null)` unchanged; new overload without optional parameters. | `UjmwSwaggerSetupExtensions.AddSwaggerGenSmartStandardsFlavored` |
| 2 | One security definition per token source (scheme name = token source id) plus a document-specific requirement per group. | `SmartStandardsAuthTokenConfigDocumentFilter` (document name = API group name; one requirement per token source = alternatives) |
| 3 | `IssueMode` mapping: code flow -> `authorizationCode` (`AuthEndpointUrl`, `RetrieveEndpointUrl` as token URL, PKCE); implicit -> `implicit` (legacy); other modes -> Bearer input. | `SmartStandardsAuthTokenConfigDocumentFilter.CreateSecurityScheme` |
| 4 | Fallback without delegate: existing behavior (global `Bearer` plus OAuth from `OAuthClientIdForSwaggerUi`, `OAuthAuthorizeUrlForSwaggerUi`, `OAuthScopeExpressionForSwaggerUi`), output identical to before. | `SmartStandardsSwaggerGenOptionsConfigurator.Configure`, `SmartStandardsSwaggerStartupFilter` |
| 5 | Known limit: Swagger UI supports only one global `OAuthClientId`. | First non-empty `ClientId` of an OAuth config wins |
| 6 | `{RequestBaseUrl}` placeholder in config URLs is replaced per request in `UseSwagger(c => c.PreSerializeFilters...)`: `scheme://host[:port]` plus path base, without trailing slash, plain case-sensitive replacement. | `SmartStandardsAuthTokenConfigDocumentFilter.ResolveRequestBaseUrlPlaceholders` |
| 7 | Client id `swagger-ui` and `OAuthUsePkce()` only on the delegate path. | `SmartStandardsSwaggerStartupFilter` |
| 8 | No breaking change; version bump (minor) is done manually by the maintainer. | Additive members only |
| 9 | Scopes are transported via `AuthTokenConfig.Claims["scope"]` (blank-separated). | `SmartStandardsAuthTokenConfigDocumentFilter.GetScopes` |
| 10 | Convenience overload accepting a single `AuthTokenConfig` instance (no resolver) for all API groups. | `AddSwaggerGenSmartStandardsFlavored(AuthTokenConfig)` with token source id `default` |

## Behavior Details

- The resolver is evaluated as late as possible, so that the order of initialization (e.g. registration of token
	sources in the `AccessTokenManager` after `AddSwaggerGenSmartStandardsFlavored`) does not matter:
	- it is never called while services or the request pipeline are configured,
	- the documents call it per request of `docs/schema/<group>.json`, i.e. in real time,
	- the Swagger UI (client id, PKCE) calls it per (re)load of the UI (`/docs`, `/docs/`, `/docs/index.html`,
		`/docs/index.js`); a small middleware updates the `OAuthConfigObject` of the captured `SwaggerUIOptions`. This
		relies on the fact that `SwaggerUIMiddleware` (Swashbuckle 10.1.7) serializes its options per request without
		caching. Without any OAuth config, the initial values of the options are restored.
	- Changes become visible after reloading the Swagger UI page; an already opened page is not updated.
- An empty or `null` result for a group means: no security information in that document.
- An empty token source id, a `null` config, or a missing URL required by the `IssueMode` throws an
	`InvalidOperationException` (fail fast). Exceptions of the resolver are not caught.
- Duplicate token source ids within one result are emitted once.
- Scopes are transported within the `AuthTokenConfig`: if `Claims` is not null and contains the key `scope`, its value
  is split by blank (`" "`) into the scope array (empty entries and duplicates removed). The scopes fill the `scopes`
  of the OAuth flow (label `Scope: <scope>`) and the security requirement of OAuth2 schemes. Bearer schemes get no
  scopes, because OpenAPI 3.0 allows scopes in requirements only for OAuth2 / OpenID Connect. The values are used
  literally: a scope prefix (e.g. `API:`) must already be contained, placeholders like `%tenant%` are not resolved.
- Bearer input uses the standard HTTP `bearer` scheme (the Swagger UI adds the `Bearer ` prefix itself), unlike the
	legacy `ApiKey`-based `Bearer` definition.

## Compatibility

- Binary compatible: only new members (overload, constructors, `SmartStandardsAuthTokenConfigDocumentFilter`).
- Behavior without the delegate is unchanged.
- Source-level edge case: a call with a literal `null` (`AddSwaggerGenSmartStandardsFlavored(null)`) becomes ambiguous
	(CS0121). It is equivalent to calling the method without arguments, which stays unambiguous.

## Artifacts

| Artifact | Role |
| --- | --- |
| `UjmwSwaggerSetupExtensions.AddSwaggerGenSmartStandardsFlavored(...)` | Entry points (legacy, resolver and single-`AuthTokenConfig` convenience overload). |
| `SmartStandardsSwaggerGenOptionsConfigurator` | Configures `SwaggerGenOptions`; registers the document filter on the delegate path, otherwise the legacy definitions. |
| `SmartStandardsAuthTokenConfigDocumentFilter` | Emits schemes and requirements per group; maps `IssueMode`; resolves `{RequestBaseUrl}`. |
| `SmartStandardsSwaggerStartupFilter` | Maps Swagger JSON and UI under `/docs`; registers the placeholder filter and refreshes client id / PKCE per UI (re)load on the delegate path. |
