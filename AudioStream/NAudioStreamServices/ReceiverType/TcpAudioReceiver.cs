using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AudioStream.NAudioStreamServices.ReceiverType
{
    internal class TcpAudioReceiver : IAudioReceiver
    {
        private readonly TcpListener Listener;
        private Action<byte[]> Handler;
        private bool Listening;

        public TcpAudioReceiver(int portNumber)
        {
            var endPoint = new IPEndPoint(IPAddress.Any, portNumber);
            Listener = new TcpListener(endPoint);
            Listener.Start();
            Listening = true;
            ThreadPool.QueueUserWorkItem(ListenerThread, null);
        }

        public void OnReceived(Action<byte[]> onAudioReceivedAction)
        {
            Handler = onAudioReceivedAction;
        }

        private void ListenerThread(object state)
        {
            var incomingBuffer = new byte[1024 * 16];
            try
            {
                while (Listening)
                {
                    using var client = Listener.AcceptTcpClient();
                    while (Listening)
                    {
                        var received = client.Client.Receive(incomingBuffer);
                        var b = new byte[received];
                        Buffer.BlockCopy(incomingBuffer, 0, b, 0, received);
                        Handler?.Invoke(b);
                    }
                }
            }
            catch (SocketException)
            {
                //Disconnected
            }
        }

        public void Dispose()
        {
            Listening = false;
            Listener?.Stop();
        }
    }
}