using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace LiveCaptionsTranslator.utils
{
    public class TtsPlaybackService : IDisposable
    {
        private readonly EdgeTtsClient _client;
        private readonly SemaphoreSlim _playLock = new(1, 1);

        public TtsPlaybackService(EdgeTtsClient client)
        {
            _client = client;
        }

        public async Task SpeakAsync(string text, string voice, string? outputDeviceId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            await _playLock.WaitAsync(ct);
            try
            {
                var wavBytes = await _client.SynthesizeWavAsync(text, voice, ct);
                using var ms = new MemoryStream(wavBytes);
                using var reader = new WaveFileReader(ms);
                using var wasapi = CreateWasapiOut(outputDeviceId);
                wasapi.Init(reader);
                wasapi.Play();

                while (wasapi.PlaybackState == PlaybackState.Playing)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(50, ct);
                }
            }
            finally
            {
                _playLock.Release();
            }
        }

        private static IWavePlayer CreateWasapiOut(string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return new WasapiOut(AudioClientShareMode.Shared, 50);
            }

            using var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDevice(deviceId);
            return new WasapiOut(device, AudioClientShareMode.Shared, false, 50);
        }

        public void Dispose()
        {
            _playLock.Dispose();
        }
    }
}
