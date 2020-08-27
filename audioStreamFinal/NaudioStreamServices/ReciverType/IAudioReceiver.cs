using System;

namespace audioStreamFinal
{
	interface IAudioReceiver : IDisposable
	{
		void OnReceived(Action<byte[]> handler);
	}
}
