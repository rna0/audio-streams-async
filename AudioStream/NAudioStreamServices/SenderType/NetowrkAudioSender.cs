using System;
using AudioStream.NAudioStreamServices.CompressionType;
using NAudio.Wave;

namespace AudioStream.NAudioStreamServices.SenderType
{
    internal class NetworkAudioSender
    {
        private readonly INetworkChatCodec Codec;
        private readonly IAudioSender AudioSender;
        private readonly WaveInEvent WaveIn;
        private int InputVol;
        private int Temp;

        public NetworkAudioSender(INetworkChatCodec codec, int inputDeviceNumber, IAudioSender audioSender)
        {
            Codec = codec;
            AudioSender = audioSender;

            WaveIn = new WaveInEvent
            {
                BufferMilliseconds = 50, DeviceNumber = inputDeviceNumber, WaveFormat = codec.RecordFormat
            };

            WaveIn.DataAvailable += OnAudioCaptured;
            WaveIn.StartRecording();
        }

        private void OnAudioCaptured(object sender, WaveInEventArgs e)
        {
            for (var i = 0; i < e.BytesRecorded; i += 2)
            {
                var sample = (short) ((e.Buffer[i + 1] << 8) |
                                      e.Buffer[i + 0]);
                var sample32 = sample / 32768f;

                //Audio converted to db value.
                var sampleD = (double) sample32;
                sampleD = 20 * Math.Log10(Math.Abs(sampleD));
                Temp = (int) sampleD + 100;

                //Filter to remove nonsensical db outputs
                if (Temp > 0 && Temp < 100)
                {
                    InputVol = Temp;
                }
            }

            var encoded = Codec.Encode(e.Buffer, 0, e.BytesRecorded);
            AudioSender.Send(encoded);
        }

        public void Dispose()
        {
            WaveIn.DataAvailable -= OnAudioCaptured;
            WaveIn.StopRecording();
            WaveIn.Dispose();
            WaveIn?.Dispose();
            AudioSender?.Dispose();
        }
    }
}