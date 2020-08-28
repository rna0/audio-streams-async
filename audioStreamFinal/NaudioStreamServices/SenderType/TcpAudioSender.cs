using System.Net;
using System.Net.Sockets;

namespace audioStreamFinal.SenderType
{
	class TcpAudioSender : IAudioSender
	{
		private readonly TcpClient tcpSender;
		public TcpAudioSender(IPEndPoint endPoint)
		{
			try
			{
				tcpSender = new TcpClient();
				tcpSender.Connect(endPoint);
			}
			catch
			{
				System.Console.WriteLine("## The connection timed out ##");
			}
		}

		public void Send(byte[] payload)
		{
			try
			{
				tcpSender.Client.Send(payload);
			}
			catch
			{

			}
		}

		public void Dispose()
		{
			tcpSender?.Close();
		}
	}
}
