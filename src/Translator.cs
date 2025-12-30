using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class Translator
    {
        private string _lastSpoken = "";

        private static string SanitizeForTts(string s)
        {
            s = Regex.Replace(s, @"\s+", " ").Trim();
            s = s.Replace("[", "").Replace("]", "");
            return s;
        }

        private bool IsDuplicate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return true;
            if (string.Equals(_lastSpoken, s, StringComparison.OrdinalIgnoreCase)) return true;
            _lastSpoken = s;
            return false;
        }

        private async Task DisplayLoop(CancellationToken ct)
        {
            // Existing loop logic...
            // Integrate sentence-end trigger using isChoke.
            // Pseudocode integration point:

            var setting = new Setting(); // replace with actual instance
            var tts = new TtsPlaybackService(new EdgeTtsClient());

            while (!ct.IsCancellationRequested)
            {
                string translatedText = ""; // replace with actual translated text
                bool isChoke = false; // replace with actual isChoke condition

                if (setting.EnableTts && isChoke)
                {
                    var toSpeak = SanitizeForTts(translatedText);
                    if (!IsDuplicate(toSpeak))
                    {
                        await tts.SpeakAsync(toSpeak, setting.TtsVoice, setting.TtsOutputDeviceId, ct);
                    }
                }

                await Task.Delay(10, ct);
            }
        }
    }
}
