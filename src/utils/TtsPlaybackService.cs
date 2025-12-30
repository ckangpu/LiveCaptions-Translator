using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Collections.Concurrent;

namespace LiveCaptionsTranslator.utils
{
    public class TtsPlaybackService : IDisposable
    {
        private readonly ConcurrentQueue<TtsQueueItem> playbackQueue = new();
        private readonly SemaphoreSlim queueSemaphore = new(0);
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Task processingTask;
        private string? lastSpokenText = null;
        private bool isEnabled = false;
        private string? selectedDeviceId = null;
        private string voiceName = "zh-CN-XiaoxiaoNeural";
        
        public TtsPlaybackService()
        {
            processingTask = Task.Run(ProcessQueueAsync);
        }
        
        public void Configure(bool enabled, string? deviceId, string voice)
        {
            isEnabled = enabled;
            selectedDeviceId = deviceId;
            voiceName = voice;
        }
        
        public void Enqueue(string text)
        {
            if (!isEnabled || string.IsNullOrWhiteSpace(text))
                return;
                
            // De-duplicate consecutive identical sentences
            if (text == lastSpokenText)
                return;
                
            playbackQueue.Enqueue(new TtsQueueItem { Text = text });
            queueSemaphore.Release();
        }
        
        private async Task ProcessQueueAsync()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await queueSemaphore.WaitAsync(cancellationTokenSource.Token);
                    
                    if (playbackQueue.TryDequeue(out var item))
                    {
                        await SynthesizeAndPlayAsync(item.Text);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] TTS playback error: {ex.Message}");
                }
            }
        }
        
        private async Task SynthesizeAndPlayAsync(string text)
        {
            if (!isEnabled)
                return;
                
            try
            {
                // Synthesize speech
                var audioData = await EdgeTtsClient.SynthesizeTextToSpeechAsync(text, voiceName, cancellationTokenSource.Token);
                
                if (audioData == null || audioData.Length == 0)
                {
                    Console.WriteLine("[WARNING] TTS synthesis returned no audio data");
                    return;
                }
                
                // Play audio on selected device
                using (var ms = new MemoryStream(audioData))
                using (var reader = new Mp3FileReader(ms))
                {
                    MMDevice? device = null;
                    if (!string.IsNullOrEmpty(selectedDeviceId))
                    {
                        device = AudioDeviceService.GetDeviceById(selectedDeviceId);
                    }
                    
                    using (var outputDevice = device != null 
                        ? new WasapiOut(device, AudioClientShareMode.Shared, false, 200)
                        : new WasapiOut(AudioClientShareMode.Shared, 200))
                    {
                        outputDevice.Init(reader);
                        outputDevice.Play();
                        
                        // Wait for playback to complete
                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            await Task.Delay(100, cancellationTokenSource.Token);
                        }
                    }
                }
                
                // Update last spoken text after successful playback
                lastSpokenText = text;
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] TTS playback failed: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            try
            {
                processingTask.Wait(TimeSpan.FromSeconds(5)); // Wait up to 5 seconds for graceful shutdown
            }
            catch (AggregateException)
            {
                // Ignore cancellation exceptions during shutdown
            }
            queueSemaphore.Dispose();
            cancellationTokenSource.Dispose();
        }
        
        private class TtsQueueItem
        {
            public string Text { get; set; } = string.Empty;
        }
    }
}
