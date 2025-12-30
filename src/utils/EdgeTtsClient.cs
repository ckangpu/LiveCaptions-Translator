using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace LiveCaptionsTranslator.utils
{
    public class EdgeTtsClient
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string EDGE_TTS_URL = "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken=6A5AA1D4EAFF4E9FB37E23D68491D6F4";
        
        public static async Task<byte[]?> SynthesizeTextToSpeechAsync(string text, string voice = "zh-CN-XiaoxiaoNeural", CancellationToken cancellationToken = default)
        {
            try
            {
                using var ws = new ClientWebSocket();
                ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
                ws.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
                ws.Options.SetRequestHeader("Accept-Language", "en-US,en;q=0.9");
                ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.77 Safari/537.36 Edg/91.0.864.41");
                
                await ws.ConnectAsync(new Uri(EDGE_TTS_URL), cancellationToken);
                
                // Send configuration message
                var requestId = Guid.NewGuid().ToString("N");
                var configMessage = $"X-Timestamp:{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffK}\r\nContent-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n" +
                    "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"false\"},\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}";
                await ws.SendAsync(Encoding.UTF8.GetBytes(configMessage), WebSocketMessageType.Text, true, cancellationToken);
                
                // Send SSML message
                var ssml = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'><voice name='{voice}'>{EscapeXml(text)}</voice></speak>";
                var ssmlMessage = $"X-RequestId:{requestId}\r\nContent-Type:application/ssml+xml\r\nPath:ssml\r\n\r\n{ssml}";
                await ws.SendAsync(Encoding.UTF8.GetBytes(ssmlMessage), WebSocketMessageType.Text, true, cancellationToken);
                
                // Receive audio data
                var audioData = new List<byte>();
                var buffer = new byte[65536];
                
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;
                        
                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        // Binary messages contain audio data after header
                        var headerEnd = FindHeaderEnd(buffer, result.Count);
                        if (headerEnd > 0 && headerEnd < result.Count)
                        {
                            audioData.AddRange(buffer.Skip(headerEnd).Take(result.Count - headerEnd));
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // Check for turn.end message
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (message.Contains("Path:turn.end"))
                            break;
                    }
                }
                
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                
                return audioData.Count > 0 ? audioData.ToArray() : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Edge TTS synthesis failed: {ex.Message}");
                return null;
            }
        }
        
        private static int FindHeaderEnd(byte[] buffer, int count)
        {
            // Look for \r\n\r\n which marks the end of headers
            for (int i = 0; i < count - 3; i++)
            {
                if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
                    return i + 4;
            }
            return -1;
        }
        
        private static string EscapeXml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
