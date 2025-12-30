using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LiveCaptionsTranslator.utils
{
    // Minimal Edge TTS REST wrapper.
    // NOTE: If this repo already had a different Edge TTS approach, adapt accordingly.
    public class EdgeTtsClient
    {
        private readonly HttpClient _http;

        public EdgeTtsClient(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
        }

        public async Task<byte[]> SynthesizeWavAsync(string text, string voice, CancellationToken ct)
        {
            // This endpoint is a placeholder; actual Edge TTS uses websocket.
            // In this repository we provide a local service hook pattern.
            // Replace with your actual synthesis backend if needed.
            var payload = new { text, voice, format = "riff-24khz-16bit-mono-pcm" };
            var json = JsonSerializer.Serialize(payload);
            using var req = new HttpRequestMessage(HttpMethod.Post, "http://localhost:8765/edge-tts/speak");
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsByteArrayAsync(ct);
        }
    }
}
