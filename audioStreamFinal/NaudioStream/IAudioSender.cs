using System;

namespace audioStreamFinal
{
	interface IAudioSender : IDisposable
	{
		void Send(byte[] payload);
	}
}
