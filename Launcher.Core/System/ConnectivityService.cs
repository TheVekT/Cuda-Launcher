using System.Net.NetworkInformation;
using Launcher.Core.System.Abstractions;

namespace Launcher.Core.System;

public class ConnectivityService(HttpClient httpClient) : IConnectivityService
{
    private DateTime _lastCheckTime = DateTime.MinValue;
    private bool _lastCheckResult;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);

    public async Task<bool> CheckInternetAccessAsync(CancellationToken cancellationToken = default)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return false;

        if (DateTime.UtcNow - _lastCheckTime < _cacheDuration)
            return _lastCheckResult;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            using var request = new HttpRequestMessage(HttpMethod.Head, "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            _lastCheckResult = response.IsSuccessStatusCode;
        }
        catch
        {
            _lastCheckResult = false;
        }

        _lastCheckTime = DateTime.UtcNow;
        return _lastCheckResult;
    }
}
