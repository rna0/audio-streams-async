using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AudioStream.NAudioStreamServices.ReceiverType
{
    internal class UdpAudioReceiver : IAudioReceiver
    {
        private Action<byte[]> Handler;
        private readonly UdpClient UdpListener;
        private bool Listeneing;

        public UdpAudioReceiver(int portNumber)
        {
            var endPoint = new IPEndPoint(IPAddress.Any, portNumber);

            UdpListener = new UdpClient();

            //Below for testing purpose
            UdpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            UdpListener.Client.Bind(endPoint);

            ThreadPool.QueueUserWorkItem(ListenerThread, endPoint);
            Listeneing = true;
        }

        private void ListenerThread(object state)
        {
            var endPoint = (IPEndPoint) state;
            try
            {
                while (Listeneing)
                {
                    var b = UdpListener.Receive(ref endPoint);
                    Handler?.Invoke(b);
                }
            }
            catch (SocketException)
            {
                //Disconnected 
            }
        }

        public void Dispose()
        {
            Listeneing = false;
            UdpListener?.Close();
        }

        public void OnReceived(Action<byte[]> onAudioReceivedAction)
        {
            Handler = onAudioReceivedAction;
        }
    }
}