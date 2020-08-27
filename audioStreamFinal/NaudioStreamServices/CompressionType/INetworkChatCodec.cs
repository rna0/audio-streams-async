using NAudio.Wave;
using System;

namespace audioStreamFinal
{
	/// <summary>
	/// for more explanations about codecs watch here
	/// https://www.twilio.com/docs/video/managing-codecs
	/// </summary>
	public interface INetworkChatCodec : IDisposable
	{
		string Name { get; }
		bool IsAvailable { get; }
		int BitsPerSecond { get; }
		WaveFormat RecordFormat { get; }
		byte[] Encode(byte[] data, int offset, int length);
		byte[] Decode(byte[] data, int offset, int length);
	}
}
