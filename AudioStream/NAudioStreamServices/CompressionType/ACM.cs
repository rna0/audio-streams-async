using System;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.Compression;

namespace AudioStream.NAudioStreamServices.CompressionType
{
    internal abstract class AcmChatCodec : INetworkChatCodec
    {
        private readonly WaveFormat EncodeFormat;
        private AcmStream EncodeStream;
        private AcmStream DecodeStream;
        private int DecodeSourceBytesLeftovers;
        private int EncodeSourceBytesLeftovers;

        protected AcmChatCodec(WaveFormat recordFormat, WaveFormat encodeFormat)
        {
            RecordFormat = recordFormat;
            EncodeFormat = encodeFormat;
        }

        public WaveFormat RecordFormat { get; }

        public byte[] Encode(byte[] data, int offset, int length)
        {
            EncodeStream ??= new AcmStream(RecordFormat, EncodeFormat);

            return Convert(EncodeStream, data, offset, length, ref EncodeSourceBytesLeftovers);
        }


        public byte[] Decode(byte[] data, int offset, int length)
        {
            DecodeStream ??= new AcmStream(EncodeFormat, RecordFormat);

            return Convert(DecodeStream, data, offset, length, ref DecodeSourceBytesLeftovers);
        }

        private static byte[] Convert(AcmStream conversionStream, byte[] data, int offset, int length,
            ref int sourceBytesLeftovers)
        {
            var bytesInSourceBuffer = length + sourceBytesLeftovers;
            Array.Copy(data, offset, conversionStream.SourceBuffer, sourceBytesLeftovers, length);
            var bytesConverted = conversionStream.Convert(bytesInSourceBuffer, out var sourceBytesConverted);
            sourceBytesLeftovers = bytesInSourceBuffer - sourceBytesConverted;
            if (sourceBytesLeftovers > 0)
            {
                Array.Copy(conversionStream.SourceBuffer, sourceBytesConverted, conversionStream.SourceBuffer, 0,
                    sourceBytesLeftovers);
            }

            var encoded = new byte[bytesConverted];
            Array.Copy(conversionStream.DestBuffer, 0, encoded, 0, bytesConverted);
            return encoded;
        }

        public abstract string Name { get; }

        public int BitsPerSecond => EncodeFormat.AverageBytesPerSecond * 8;

        public void Dispose()
        {
            if (EncodeStream != null)
            {
                EncodeStream.Dispose();
                EncodeStream = null;
            }

            if (DecodeStream == null) return;
            DecodeStream.Dispose();
            DecodeStream = null;
        }

        public bool IsAvailable
        {
            get
            {
                //Is this codec installed
                var available = true;
                try
                {
                    using (new AcmStream(RecordFormat, EncodeFormat))
                    {
                    }

                    using (new AcmStream(EncodeFormat, RecordFormat))
                    {
                    }
                }
                catch (MmException)
                {
                    available = false;
                }

                return available;
            }
        }
    }
}