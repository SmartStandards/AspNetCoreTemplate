# WebView2Template - Open Issues of the WebViewAspNetCoreAdapter

Open defects and limitations of `dotnet/src/WebView2Template/WebViewAspNetCoreAdapter.cs`, found during a code review
on 2026-11-25 (source read only, no runtime test). Consumers embed the file via `WebViewAspNetCoreAdapter.tt`
directly from the branch `10.0`, so every fix reaches all consumers on their next transformation.

Background: the stack overflow fix from BEYOND-light (2026-11-18, `OnWebResourceRequested` is now `async void`
instead of `Invoke` plus an `Application.DoEvents()` wait loop) has been ported. The issues below are NOT addressed
by that fix. Remove an entry as soon as it is fixed.

## 1. Redirects of fetch/XHR requests navigate the whole page away

**Where:** `OnWebResourceRequested`, manual redirect handling for custom schemes (`_BaseAddressHasCustomScheme`).

**Problem:** On a redirect status (301, 302, 303, 307, 308) the adapter calls `coreWebView.Navigate(redirectUri)`
regardless of the request type. `e.ResourceContext` is not checked.

**Effect:** If an API answers a `fetch`/XHR call (or an image, script, ...) with a redirect, the whole WebView
navigates to the redirect target and the single-page application is gone.

**Proposed fix:** Navigate manually only for `CoreWebView2WebResourceContext.Document`. For other contexts, check
whether WebView2 follows the redirect itself under the custom scheme; otherwise resolve the redirect inside the
adapter (send a follow-up request to the TestServer) and return the final response.

## 2. Multi-value response headers are merged with a comma (breaks `Set-Cookie`)

**Where:** `BuildResponseHeaderString`.

**Problem:** Several values of one header are combined into one line with `", "`.

**Effect:** For `Set-Cookie` this is invalid: several cookies are merged into one, and cookies with an `Expires`
attribute (which contains a comma) are corrupted. This breaks cookie-based authentication and sessions.

**Proposed fix:** Write one header line per value (`Set-Cookie: a=1` and `Set-Cookie: b=2` on separate lines). The
header string of `CreateWebResourceResponse` allows repeated header names.

## 3. Requests longer than 30 seconds fail; no streaming responses

**Where:** `_HttpClientFactory` in the constructor (`HttpClient.Timeout = 30 s`) and the response buffering in
`OnWebResourceRequested`.

**Problem:** Every request that takes longer than 30 seconds is aborted and answered with an error page (500).
Responses are buffered completely before they are passed to WebView2.

**Effect:**
- SignalR has no WebSockets in the WebView (see the skill) and falls back to long polling. Its default poll timeout
  (90 s) is longer than 30 s, so every idle poll fails.
- Server-Sent Events and other streaming responses never complete and therefore do not work.
- Long-running operations (exports, large uploads/downloads) fail after 30 s.

**Proposed fix:** Make the timeout configurable (or `Timeout.InfiniteTimeSpan` plus cancellation when the WebView or
the adapter is disposed). For streaming, an unbuffered response stream would be needed, which conflicts with the
synchronous reading of the stream by WebView2 on the UI thread - needs a separate design. Until then: prefer short
polling or `PostWebMessageAsJson` for server push.

## 4. `Dispose()` throws a NullReferenceException before the host has started

**Where:** `Dispose()`.

**Problem:** `_Host` is assigned on the host thread after `application.Start()`. If the adapter is disposed before
that (e.g. the application is closed during startup), `_Host.Dispose()` throws.

**Proposed fix:** Null check on `_Host`; additionally let `WaitForHttpClientFactoryAsync` stop waiting when the
adapter is disposed (`_IsDisposed` or `_HostCancellationTokenSource`).

## 5. Minor: memory visibility of `_HttpClientFactory`

**Where:** field `_HttpClientFactory`, `WaitForHttpClientFactoryAsync`.

**Problem:** The field is written on the host thread and polled on the UI thread without `volatile` or
`Volatile.Read`. In practice the `await Task.Delay(...)` acts as a memory barrier, but formally the access is not
correct.

**Proposed fix:** Declare the field `volatile` or replace the polling with a `TaskCompletionSource<Func<HttpClient>>`
that is completed by the host thread.

## Behavior Change to Watch (not a defect)

Since the stack overflow fix, requests are processed truly in parallel. Before, they were effectively serialized by
the nested UI-thread dispatch. Application code that relied on this implicitly (non-thread-safe singletons, shared
state without locks) can now run into race conditions.
