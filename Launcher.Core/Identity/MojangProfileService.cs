using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Launcher.Core.Common.Enums;
using Launcher.Core.Identity.Abstractions;
using Launcher.Core.Identity.Models;

namespace Launcher.Core.Identity;

public class MojangProfileService(HttpClient httpClient) : IMojangProfileService
{
    private const string BaseUrl = "https://api.minecraftservices.com/minecraft/profile";

    private static NetworkRequestStatus MapStatusCode(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.OK or HttpStatusCode.NoContent or HttpStatusCode.Created or HttpStatusCode.Accepted => NetworkRequestStatus.Success,
        HttpStatusCode.TooManyRequests => NetworkRequestStatus.RateLimited,
        HttpStatusCode.Unauthorized => NetworkRequestStatus.Unauthorized,
        HttpStatusCode.Forbidden => NetworkRequestStatus.Forbidden,
        HttpStatusCode.NotFound => NetworkRequestStatus.NotFound,
        HttpStatusCode.BadRequest => NetworkRequestStatus.BadRequest,
        >= HttpStatusCode.InternalServerError => NetworkRequestStatus.ServerError,
        _ => NetworkRequestStatus.Error
    };

    public async Task<MojangProfile?> GetProfileAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MojangProfile>(json);
    }

    public async Task<NetworkRequestStatus> UploadSkinAsync(string accessToken, string filePath, string variant = "classic")
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/skins");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(variant), "variant");

            await using var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "file", Path.GetFileName(filePath));

            request.Content = content;

            var response = await httpClient.SendAsync(request);
            return MapStatusCode(response.StatusCode);
        }
        catch
        {
            return NetworkRequestStatus.Error;
        }
    }

    public async Task<NetworkRequestStatus> ApplyCapeAsync(string accessToken, string capeId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/capes/active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var jsonContent = JsonSerializer.Serialize(new { capeId });
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);
            return MapStatusCode(response.StatusCode);
        }
        catch
        {
            return NetworkRequestStatus.Error;
        }
    }

    public async Task<NetworkRequestStatus> HideCapeAsync(string accessToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/capes/active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);
            return MapStatusCode(response.StatusCode);
        }
        catch
        {
            return NetworkRequestStatus.Error;
        }
    }
}