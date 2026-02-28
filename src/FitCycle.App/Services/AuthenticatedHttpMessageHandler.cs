using System.Net.Http.Headers;

namespace FitCycle.App.Services;

public class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private const string AccessTokenKey = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";
    private static bool _isRefreshing;

    public AuthenticatedHttpMessageHandler() : base(new HttpClientHandler())
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var needsAuth = !path.StartsWith("/auth/") || path == "/auth/me";

        if (needsAuth)
        {
            var token = await SecureStorage.GetAsync(AccessTokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // On 401, try refresh token, then retry once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            && needsAuth && !_isRefreshing
            && path != "/auth/refresh")
        {
            _isRefreshing = true;
            try
            {
                var refreshed = await TryRefreshTokenAsync(request.RequestUri!, cancellationToken);
                if (refreshed)
                {
                    // Retry with new token
                    var retry = await CloneAndRetryAsync(request, cancellationToken);
                    if (retry.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                        return retry;
                }

                // Refresh failed — redirect to login
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SecureStorage.Remove(AccessTokenKey);
                    SecureStorage.Remove(RefreshTokenKey);
                    SecureStorage.Remove("auth_username");
                    SecureStorage.Remove("auth_role");
                    if (Application.Current is not null)
                        Application.Current.MainPage = new NavigationPage(new Pages.LoginPage());
                });
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        return response;
    }

    private async Task<bool> TryRefreshTokenAsync(Uri baseUri, CancellationToken ct)
    {
        try
        {
            var refreshToken = await SecureStorage.GetAsync(RefreshTokenKey);
            if (string.IsNullOrEmpty(refreshToken)) return false;

            var refreshReq = new HttpRequestMessage(HttpMethod.Post,
                new Uri(baseUri.GetLeftPart(UriPartial.Authority) + "/auth/refresh"));
            refreshReq.Content = System.Net.Http.Json.JsonContent.Create(new { RefreshToken = refreshToken });

            var resp = await base.SendAsync(refreshReq, ct);
            if (!resp.IsSuccessStatusCode) return false;

            var json = await resp.Content.ReadAsStringAsync(ct);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("accessToken", out var at))
                await SecureStorage.SetAsync(AccessTokenKey, at.GetString()!);
            if (root.TryGetProperty("refreshToken", out var rt))
                await SecureStorage.SetAsync(RefreshTokenKey, rt.GetString()!);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<HttpResponseMessage> CloneAndRetryAsync(
        HttpRequestMessage original, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        if (original.Content is not null)
        {
            var body = await original.Content.ReadAsByteArrayAsync(ct);
            clone.Content = new ByteArrayContent(body);
            if (original.Content.Headers.ContentType is not null)
                clone.Content.Headers.ContentType = original.Content.Headers.ContentType;
        }

        var token = await SecureStorage.GetAsync(AccessTokenKey);
        if (!string.IsNullOrEmpty(token))
            clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(clone, ct);
    }
}
