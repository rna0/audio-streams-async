using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Data;
using System.Linq;
using System.Net;
using NAudio.Wave;
using System.Globalization;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace audioStreamFinal
{
	
	public class NetworkChatPanel
	{
		private INetworkChatCodec selectedCodec;
		private volatile bool connected;
		private NetworkAudioPlayer player;
		private NetworkAudioSender audioSender;
		private List<CodecComboItem> comboBoxCodecs = new List<CodecComboItem>();
		private List<string> comboBoxInputDevices = new List<string>();
		private int comboBoxCodecsIndex = 0;
		private bool isUDP = true;
		string ipAddr = "192.168.1.167";//GetLocalIPAddress();//
		string textPort = "8192";
		int audioValue = 10;

		public static int inputSens;

		[DllImport("winmm.dll")]
		public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

		[DllImport("winmm.dll")]
		public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

		public NetworkChatPanel()
		{
			// Use reflection to find all the codecs
			var codecs = ReflectionHelperInstances.CreatAllInstancesOf<INetworkChatCodec>();

			PopulateInputDevicesCombo();
			PopulateCodecsCombo(codecs);

			uint CurrVol = 0;
			waveOutGetVolume(IntPtr.Zero, out CurrVol);
			ushort CalcVol = (ushort)(CurrVol & 0x0000FFFF);


			_ = StartStreamingAsync();
		}
		private void PopulateCodecsCombo(IEnumerable<INetworkChatCodec> codecs)
		{
			var sorted = from codec in codecs
						 where codec.IsAvailable
						 orderby codec.BitsPerSecond ascending
						 select codec;

			foreach(var codec in sorted)
			{
				var bitRate = codec.BitsPerSecond == -1 ? "VBR" : $"{codec.BitsPerSecond / 1000.0:0.#}kbps";
				var text = $"{codec.Name} ({bitRate})";
				comboBoxCodecs.Add(new CodecComboItem { Text = text, Codec = codec });
			}

			//**see the full speakers list**
			//foreach (CodecComboItem item in comboBoxCodecs)
			//{
			//	Console.WriteLine(item.Text);
			//}
		}

		class CodecComboItem
		{
			public string Text { get; set; }
			public INetworkChatCodec Codec { get; set; }
			public override string ToString()
			{
				return Text;
			}
		}

		private void PopulateInputDevicesCombo()
		{
			for(int n = 0; n < WaveIn.DeviceCount; n++)
			{
				var capabilities = WaveIn.GetCapabilities(n);
				comboBoxInputDevices.Add(capabilities.ProductName);
			}
			if (comboBoxInputDevices.Count > 0)
			{
				comboBoxCodecsIndex = 0;
			}
		}

		private async Task StartStreamingAsync()
		{
			if (!connected)
			{
				if (textPort == "")
				{
					Console.WriteLine("**Please provide correct port**");
					textPort = "0000";
				}

				int port = Int32.Parse(textPort);

				if (port > 65535)
				{
					Console.WriteLine("**Bigger than 65535, made by default to 65535**");
					port = 65535;
				}

				PopulateCodecsCombo(ReflectionHelperInstances.CreatAllInstancesOf<INetworkChatCodec>());
				try
				{
					IPEndPoint endPoint = CreateIPEndPoint(ipAddr + ":" + textPort);
					int inputDeviceNumber = comboBoxCodecsIndex;
					selectedCodec = ((CodecComboItem)comboBoxCodecs.First()).Codec;
					await ConnectAsync(isUDP, endPoint, inputDeviceNumber, selectedCodec);
					Console.WriteLine("-Connected");
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					Console.WriteLine("**Please provide correct IP address**");
				}

			}
			else
			{
				await DisconnectAsync();
				Console.WriteLine("-Disconnected");
			}
		}
		private async Task ConnectAsync(bool isUDP, IPEndPoint endPoint, int inputDeviceNumber, INetworkChatCodec codec)
		{
			var receiver = (isUDP)
				? (IAudioReceiver)new UdpAudioReceiver(endPoint.Port)
				: new TcpAudioReceiver(endPoint.Port);
			var sender = (isUDP)
				? (IAudioSender)new UdpAudioSender(endPoint)
				: new TcpAudioSender(endPoint);

			player = new NetworkAudioPlayer(codec, receiver);
			audioSender =  new NetworkAudioSender(codec, inputDeviceNumber, sender);
			connected = true;
		}

		private async Task DisconnectAsync()
		{
			if (connected)
			{
				connected = false;

				player.Dispose();
				audioSender.Dispose();
				selectedCodec.Dispose();
				saveIpPort();
			}
		}

		private void saveIpPort()
		{
			Properties.Settings.Default.IP = ipAddr;
			Properties.Settings.Default.Port = textPort;
			Properties.Settings.Default.Save();
		}
		
		private void outputVolumeControl()
		{
			//Main audio output volume control
			int newVolume = ((ushort.MaxValue / 10) * audioValue);
			uint NewVolumeAllChannels = (((uint)newVolume & 0x0000ffff) | ((uint)newVolume << 16));
			waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);
		}
		
		public static IPEndPoint CreateIPEndPoint(string endPoint)
		{
			string[] ep = endPoint.Split(':');
			if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
			IPAddress ip;
			if (ep.Length > 2)
			{
				if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
				{
					throw new FormatException("Invalid ip-adress");
				}
			}
			else
			{
				if (!IPAddress.TryParse(ep[0], out ip))
				{
					throw new FormatException("Invalid ip-adress");
				}
			}
			int port;
			if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
			{
				throw new FormatException("Invalid port");
			}
			return new IPEndPoint(ip, port);
		}
		public static string GetLocalIPAddress()
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (var ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					Console.WriteLine("ip selected: "+ ip.ToString());
					return ip.ToString();
				}
			}
			throw new Exception("No network adapters with an IPv4 address in the system!");
		}
	}
}
