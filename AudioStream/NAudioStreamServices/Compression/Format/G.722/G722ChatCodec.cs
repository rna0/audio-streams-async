using System;
using System.Diagnostics;
using NAudio.Codecs;
using NAudio.Wave;

namespace AudioStream.NAudioStreamServices.Compression.Format.G._722
{
    class G722ChatCodec : INetworkChatCodec
    {
        private readonly int Bitrate;
        private readonly G722CodecState EncoderState;
        private readonly G722CodecState DecoderState;
        private readonly G722Codec Codec;

        public G722ChatCodec()
        {
            Bitrate = 64000;
            EncoderState = new G722CodecState(Bitrate, G722Flags.None);
            DecoderState = new G722CodecState(Bitrate, G722Flags.None);
            Codec = new G722Codec();
            RecordFormat = new WaveFormat(16000, 1);
        }

        public string Name => "G.722 16kHZ";

        public int BitsPerSecond => Bitrate;

        public WaveFormat RecordFormat { get; }

        public byte[] Encode(byte[] data, int offset, int length)
        {
            if (offset != 0)
            {
                throw new ArgumentException("G722 does not yet support non-zero offsets");
            }

            var wb = new WaveBuffer(data);
            var encodedLength = length / 4;
            var outputBuffer = new byte[encodedLength];
            var encoded = Codec.Encode(EncoderState, outputBuffer, wb.ShortBuffer, length / 2);
            Debug.Assert(encodedLength == encoded);
            return outputBuffer;
        }

        public byte[] Decode(byte[] data, int offset, int length)
        {
            if (offset != 0)
            {
                throw new ArgumentException("G722 does not yet support non-zero offsets");
            }

            var decodedLength = length * 4;
            var outputBuffer = new byte[decodedLength];
            var wb = new WaveBuffer(outputBuffer);
            var decoded = Codec.Decode(DecoderState, wb.ShortBuffer, data, length);
            Debug.Assert(decodedLength == decoded * 2);
            return outputBuffer;
        }

        public void Dispose()
        {
            //Nada
        }

        public bool IsAvailable => true;
    }
}