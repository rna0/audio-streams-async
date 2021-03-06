﻿using AudioStream.NAudioStreamServices.Compression.Format.ACM;
using NAudio.Wave;

namespace AudioStream.NAudioStreamServices.Compression.Format.MicrosoftADPCM
{
    class MS_ADPCM : AcmChatCodec
    {
        public MS_ADPCM()
            : base(new WaveFormat(8000, 16, 1), new AdpcmWaveFormat(8000, 1))
        {
        }

        public override string Name => "Microsoft ADPCM";
    }
}