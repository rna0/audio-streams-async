using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace audioStreamFinal.ReciverType
{
	class NetworkAudioPlayer : IDisposable
	{
		private readonly INetworkChatCodec codec;
		private readonly IAudioReceiver receiver;
		private readonly IWavePlayer waveOut;
		private readonly BufferedWaveProvider waveProvider;
		private byte[] bufferDecoded;

		public NetworkAudioPlayer(INetworkChatCodec codec, IAudioReceiver receiver)
		{
			this.codec = codec;
			this.receiver = receiver;
			receiver.OnReceived(OnDataReceived);

			this.ReceiveAudio(receiver);

			waveOut = new WaveOut();
			waveProvider = new BufferedWaveProvider(codec.RecordFormat);
			waveOut.Init(waveProvider);
			waveOut.Play();
		}

		private async Task ReceiveAudio(IAudioReceiver audioSender)
		{
			await Task.Run(() =>
			{
				while (true)
				{
					if (this.bufferDecoded != null)
					{
						waveProvider.AddSamples(bufferDecoded, 0, bufferDecoded.Length);
						this.bufferDecoded = null;
					}

					Task.Delay(50);
				}
			});
		}
		void OnDataReceived(byte[] compressed)
		{
			bufferDecoded = codec.Decode(compressed, 0, compressed.Length);
		}

		public void Dispose()
		{
			receiver?.Dispose();
			waveOut?.Dispose();
		}
	}
}
