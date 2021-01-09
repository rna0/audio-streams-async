namespace AudioStream.Panel
{
    /// <summary>
    /// making definitions of all my constants
    /// </summary>
    public static class NetworkChatPanelConstants
    {
        //constants from isIPOk()
        public const int IpV4Len = 4;

        //constants fr om isPortOk()
        public const int DefaultPort = 8192;

        public const int MaxPort = 47823;

        //constants from ChooseInputDevicesSource()
        public const int DefaultInputDevicesIndex = 0;

        //constants from ChooseCodecSource()
        public const int DefaultCodecsIndex = 0;

        //constants from PopulateCodecs()
        public const double Kilo = 1000.0;

        //constants from outputVolumeControl()
        public const int MaxVolume = 10;
        public const int MinVolume = 0;
        public const int VolumeOffset = 0x0000ffff;
        public const int PushVolumeOffset = 16;
    }
}