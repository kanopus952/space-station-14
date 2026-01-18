using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server._Sunrise.Auth;

public sealed class AccountCreationManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly HttpClient _httpClient = new();
    private ISawmill _sawmill = default!;
    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("auth_created_time_api");
    }
    public async Task<DateTimeOffset?> TryGetAccountCreatedTimeAsync(NetUserId userId, CancellationToken cancel = default)
    {
        if (_httpClient.BaseAddress == null)
        {
            _sawmill.Warning("Auth server URL is not set.");
            return null;
        }

        try
        {
            var requestUri = $"api/query/userid?userid={WebUtility.UrlEncode(userId.UserId.ToString())}";
            using var resp = await _httpClient.GetAsync(requestUri, cancel);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                _sawmill.Error("Auth server token is invalid.");
                return null;
            }

            if (!resp.IsSuccessStatusCode)
            {
                _sawmill.Error("Auth server returned bad response {StatusCode}!", resp.StatusCode);
                return null;
            }

            var responseData = await resp.Content.ReadFromJsonAsync<UserDataResponse>(cancellationToken: cancel);
            return responseData?.CreatedTime;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Auth server request failed: {e}");
        }

        return null;
    }

    private sealed record UserDataResponse(
        string UserName,
        Guid UserId,
        int? PatronTier,
        DateTimeOffset CreatedTime);
}
