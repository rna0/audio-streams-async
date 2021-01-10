using System;
using AudioStream.NAudioStreamServices.Compression.Format.ACM;
using NAudio.Codecs;
using NAudio.Wave;

namespace AudioStream.NAudioStreamServices.Compression.Format
{
    class AcmMuLawChatCodec : AcmChatCodec
    {
        public AcmMuLawChatCodec()
            : base(new WaveFormat(8000, 16, 1), WaveFormat.CreateMuLawFormat(8000, 1))
        {
        }

        public override string Name => "ACM G.711 mu-law";
    }

    class MuLawChatCodec : INetworkChatCodec
    {
        public string Name => "G.711 mu-law";

        public int BitsPerSecond => RecordFormat.SampleRate * 8;

        public WaveFormat RecordFormat => new WaveFormat(8000, 16, 1);

        public byte[] Encode(byte[] data, int offset, int length)
        {
            var encoded = new byte[length / 2];
            var outIndex = 0;
            for (var n = 0; n < length; n += 2)
            {
                encoded[outIndex++] = MuLawEncoder.LinearToMuLawSample(BitConverter.ToInt16(data, offset + n));
            }

            return encoded;
        }

        public byte[] Decode(byte[] data, int offset, int length)
        {
            var decoded = new byte[length * 2];
            var outIndex = 0;
            for (var n = 0; n < length; n++)
            {
                var decodedSample = MuLawDecoder.MuLawToLinearSample(data[n + offset]);
                decoded[outIndex++] = (byte) (decodedSample & 0xFF);
                decoded[outIndex++] = (byte) (decodedSample >> 8);
            }

            return decoded;
        }

        public void Dispose()
        {
            //Nada
        }

        public bool IsAvailable => true;
    }
}