using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HarManager.Services.Sync
{
    public class CustomApiSyncProvider : ISyncProvider
    {
        public string Name => "Custom Server";
        private string _baseUrl = "";
        private string _token = "";
        private HttpClient? _client;

        public Task InitializeAsync(Dictionary<string, string> config)
        {
            if (config.TryGetValue("BaseUrl", out var url)) _baseUrl = url.TrimEnd('/');
            if (config.TryGetValue("Token", out var token)) _token = token;

            _client = new HttpClient();
            _client.BaseAddress = new Uri(_baseUrl);
            if (!string.IsNullOrEmpty(_token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
            
            return Task.CompletedTask;
        }

        public async Task<List<RemoteItemInfo>> ListItemsAsync(string? parentId = null)
        {
            if (_client == null) throw new InvalidOperationException("Provider not initialized");

            var url = "/api/items";
            if (!string.IsNullOrEmpty(parentId))
            {
                url += $"?parentId={Uri.EscapeDataString(parentId)}";
            }

            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<RemoteItemInfo>>(json) ?? new List<RemoteItemInfo>();
        }

        public async Task<string?> DownloadItemAsync(string remoteId)
        {
            if (_client == null) throw new InvalidOperationException("Provider not initialized");

            var response = await _client.GetAsync($"/api/items/{Uri.EscapeDataString(remoteId)}/content");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> UploadItemAsync(string name, string content, string? remoteId = null, string? parentId = null)
        {
            if (_client == null) throw new InvalidOperationException("Provider not initialized");

            var payload = new
            {
                name,
                content,
                parentId
            };

            var json = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (!string.IsNullOrEmpty(remoteId))
            {
                 // Update
                 response = await _client.PutAsync($"/api/items/{Uri.EscapeDataString(remoteId)}", httpContent);
            }
            else
            {
                // Create
                response = await _client.PostAsync("/api/items", httpContent);
            }

            response.EnsureSuccessStatusCode();
            
            // Assume server returns the ID in the body
            var responseJson = await response.Content.ReadAsStringAsync();
            dynamic? result = JsonConvert.DeserializeObject(responseJson);
            return result?.id?.ToString() ?? remoteId ?? ""; 
        }

        public async Task DeleteItemAsync(string remoteId)
        {
            if (_client == null) throw new InvalidOperationException("Provider not initialized");

            var response = await _client.DeleteAsync($"/api/items/{Uri.EscapeDataString(remoteId)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return;
            
            response.EnsureSuccessStatusCode();
        }
    }
}

