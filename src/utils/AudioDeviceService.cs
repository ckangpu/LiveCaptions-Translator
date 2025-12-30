using NAudio.CoreAudioApi;

namespace LiveCaptionsTranslator.utils
{
    public class AudioDeviceService
    {
        private static MMDeviceEnumerator? sharedEnumerator = null;
        
        public static List<AudioDeviceInfo> EnumerateOutputDevices()
        {
            var devices = new List<AudioDeviceInfo>();
            
            using (var enumerator = new MMDeviceEnumerator())
            {
                foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    devices.Add(new AudioDeviceInfo
                    {
                        Id = device.ID,
                        Name = device.FriendlyName
                    });
                }
            }
            
            return devices;
        }
        
        public static MMDevice? GetDeviceById(string? deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
                return null;
                
            try
            {
                // Keep a shared enumerator instance alive for device references
                if (sharedEnumerator == null)
                    sharedEnumerator = new MMDeviceEnumerator();
                    
                return sharedEnumerator.GetDevice(deviceId);
            }
            catch
            {
                return null;
            }
        }
    }
    
    public class AudioDeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
