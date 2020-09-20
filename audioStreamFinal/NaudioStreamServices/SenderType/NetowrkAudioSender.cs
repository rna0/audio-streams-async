using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace audioStreamFinal.SenderType
{

	class NetworkAudioSender
	{
		private readonly INetworkChatCodec codec;
		private readonly IAudioSender audioSender;
		private readonly WaveInEvent waveIn;
		private byte[] bufferEncoded;
		public int inputVol, temp;

		public NetworkAudioSender(INetworkChatCodec codec, int inputDeviceNumber, IAudioSender audioSender)
		{
			this.codec = codec;
			this.audioSender = audioSender;

			this.SendAudio(audioSender);

			waveIn = new WaveInEvent();

			waveIn.BufferMilliseconds = 50;
			waveIn.DeviceNumber = inputDeviceNumber;
			waveIn.WaveFormat = codec.RecordFormat;
			waveIn.DataAvailable += OnAudioCaptured;
			waveIn.StartRecording();
		}

		private async Task SendAudio(IAudioSender audioSender)
		{
			await Task.Run(() =>
			{
				while (true)
				{
					if (this.bufferEncoded != null)
					{
						audioSender.Send(this.bufferEncoded);
						this.bufferEncoded = null;
					}

					Task.Delay(50);
				}
			});
		}

		private void OnAudioCaptured(object sender, WaveInEventArgs e)
		{

			for (int i = 0; i < e.BytesRecorded; i += 2)
			{
				short sample = (short)((e.Buffer[i + 1] << 8) |
										e.Buffer[i + 0]);
				float sample32 = sample / 32768f;

				//Audio converted to db value.
				double sampleD = (double)sample32;
				sampleD = 20 * Math.Log10(Math.Abs(sampleD));
				temp = (int)sampleD + 100;

				//Filter to remove nonsensical db outputs
				if (temp > 0 && temp < 100)
				{
					inputVol = temp;
				}
				else
				{
					//ignore
				}
			}

			this.bufferEncoded = codec.Encode(e.Buffer, 0, e.BytesRecorded);
		}

		public void Dispose()
		{
			waveIn.DataAvailable -= OnAudioCaptured;
			waveIn.StopRecording();
			waveIn.Dispose();
			waveIn?.Dispose();
			audioSender?.Dispose();
		}
	}
}
