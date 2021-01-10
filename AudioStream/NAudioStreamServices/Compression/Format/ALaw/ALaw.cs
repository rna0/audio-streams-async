using System;
using AudioStream.NAudioStreamServices.Compression.Format.ACM;
using NAudio.Codecs;
using NAudio.Wave;

namespace AudioStream.NAudioStreamServices.Compression.Format.ALaw
{
    class ALaw : AcmChatCodec
    {
        public ALaw()
            : base(new WaveFormat(8000, 16, 1), WaveFormat.CreateALawFormat(8000, 1))
        {
        }

        public override string Name => "ACM G.711 a-law";
    }

    class ALawCodec : INetworkChatCodec
    {
        public string Name => "G.711 a-law";

        public int BitsPerSecond => RecordFormat.SampleRate * 8;

        public WaveFormat RecordFormat => new WaveFormat(8000, 16, 1);

        public byte[] Encode(byte[] data, int offset, int length)
        {
            var encoded = new byte[length / 2];
            var outIndex = 0;
            for (var n = 0; n < length; n += 2)
            {
                encoded[outIndex++] = ALawEncoder.LinearToALawSample(BitConverter.ToInt16(data, offset + n));
            }

            return encoded;
        }

        public byte[] Decode(byte[] data, int offset, int length)
        {
            var decoded = new byte[length * 2];
            var outIndex = 0;
            for (var n = 0; n < length; n++)
            {
                var decodedSample = ALawDecoder.ALawToLinearSample(data[n + offset]);
                decoded[outIndex++] = (byte) (decodedSample & 0xFF);
                decoded[outIndex++] = (byte) (decodedSample >> 8);
            }

            return decoded;
        }

        public void Dispose()
        {
            //Nothing but empty space
        }

        public bool IsAvailable => true;
    }
}