namespace LiveCaptionsTranslator.models
{
    public class Setting
    {
        // existing settings...

        public bool EnableTts { get; set; } = false;
        public string TtsVoice { get; set; } = "en-US-JennyNeural";
        public string? TtsOutputDeviceId { get; set; } = null;
    }
}
