using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;

namespace LiveCaptionsTranslator.utils
{
    public class AudioDeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public static class AudioDeviceService
    {
        public static List<AudioDeviceInfo> GetRenderDevices()
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            return devices
                .Select(d => new AudioDeviceInfo { Id = d.ID, Name = d.FriendlyName })
                .OrderBy(d => d.Name)
                .ToList();
        }

        public static string? GetDefaultRenderDeviceId()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var dev = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                return dev?.ID;
            }
            catch
            {
                return null;
            }
        }
    }
}
