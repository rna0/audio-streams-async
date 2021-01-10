using System;
using AudioStream.NAudioStreamServices.Compression;
using NAudio.Wave;

namespace AudioStream.NAudioStreamServices.ReceiverType
{
    internal class NetworkAudioPlayer : IDisposable
    {
        private readonly INetworkChatCodec Codec;
        private readonly IAudioReceiver Receiver;
        private readonly IWavePlayer WaveOut;
        private readonly BufferedWaveProvider WaveProvider;

        public NetworkAudioPlayer(INetworkChatCodec codec, IAudioReceiver receiver)
        {
            Codec = codec;
            Receiver = receiver;
            receiver.OnReceived(OnDataReceived);

            WaveOut = new WaveOutEvent();
            WaveProvider = new BufferedWaveProvider(codec.RecordFormat);
            WaveOut.Init(WaveProvider);
            WaveOut.Play();
        }

        private void OnDataReceived(byte[] compressed)
        {
            var decoded = Codec.Decode(compressed, 0, compressed.Length);
            WaveProvider.AddSamples(decoded, 0, decoded.Length);
        }

        public void Dispose()
        {
            Receiver?.Dispose();
            WaveOut?.Dispose();
        }
    }
}