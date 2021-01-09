using AudioStream.Panel;

namespace AudioStream
{
    internal static class Program
    {
        private static NetworkChatPanel NetworkChatPanel;

        private static void Main()
        {
            NetworkChatPanel = new NetworkChatPanel();
        }
    }
}