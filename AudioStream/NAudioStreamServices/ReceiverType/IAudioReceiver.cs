using System;

namespace AudioStream.NAudioStreamServices.ReceiverType
{
    internal interface IAudioReceiver : IDisposable
    {
        void OnReceived(Action<byte[]> handler);
    }
}