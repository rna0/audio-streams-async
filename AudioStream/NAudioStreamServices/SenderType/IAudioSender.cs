using System;

namespace AudioStream.NAudioStreamServices.SenderType
{
    internal interface IAudioSender : IDisposable
    {
        void Send(byte[] payload);
    }
}