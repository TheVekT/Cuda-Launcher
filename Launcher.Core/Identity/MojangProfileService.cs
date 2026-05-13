using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Launcher.Core.Identity.Abstractions;
using Launcher.Core.Identity.Models;

namespace Launcher.Core.Identity;

public class MojangProfileService : IMojangProfileService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.minecraftservices.com/minecraft/profile";

    public MojangProfileService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MojangProfile?> GetProfileAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MojangProfile>(json);
    }

    public async Task<bool> UploadSkinAsync(string accessToken, string filePath, string variant = "classic")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/skins");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var content = new MultipartFormDataContent();
        
        content.Add(new StringContent(variant), "variant");
        
        using var fileStream = File.OpenRead(filePath);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ApplyCapeAsync(string accessToken, string capeId)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/capes/active");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var jsonContent = JsonSerializer.Serialize(new { capeId = capeId });
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> HideCapeAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/capes/active");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}