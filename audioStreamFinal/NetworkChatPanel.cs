using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

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
		string ipAddr = GetLocalIPAddress();
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

			consoleUserInterface();
		}
		private char inputCharOnly()
		{
			string try_input;
			do
			{
				Console.WriteLine("enter a single character");
				try_input = Console.ReadLine();
			} while (try_input.Length != 1);
			return Char.ToLower(char.Parse(try_input));

		}
		public void consoleUserInterface()
		{

			PopulateCodecsCombo(ReflectionHelperInstances.CreatAllInstancesOf<INetworkChatCodec>());
			char input;
			do
			{

				Console.WriteLine("\nR - Refresh sources\n" +
					"C - Choose source microphone\n" +
					"I - Choose IP to connect\n" +
					"O - Connect to original IP\n" +
					"P - Choose Port to connect\n" +
					"V - Change audio volume\n" +
					"U - Choose transmission protocol\n" +
					"S - Start\n" +
					"D - Disconnect\n" +
					"E - Exit");

				input = inputCharOnly();
				switch (input)
				{
					case 'r':
						PopulateCodecsCombo(ReflectionHelperInstances.CreatAllInstancesOf<INetworkChatCodec>());
						printSources();
						break;
					case 'c':
						PopulateCodecsCombo(ReflectionHelperInstances.CreatAllInstancesOf<INetworkChatCodec>());
						ChoosePrintSources();
						break;
					case 'i':
						isIPOk();
						Console.WriteLine("\n# " + ipAddr + " is your current IP.");
						break;
					case 'o':
						ipAddr = GetLocalIPAddress();
						Console.WriteLine("\n# " + ipAddr + " is your current IP.");
						break;
					case 'p':
						Console.WriteLine("\n# " + textPort + " is your current Port.");
						isPortOk();
						break;
					case 'v':
						outputVolumeControl();
						Console.WriteLine("\n# " + audioValue + " is your current Audio volume.");
						break;
					case 'u':
						ChooseProtocol();
						Console.WriteLine("\n# {0} is your current Audio volume.", isUDP ? "UDP" : "TCP");
						break;
					case 's':
						StartStreaming();
						Console.WriteLine("-Connected");
						break;
					case 'd':
						Disconnect();
						Console.WriteLine("-Disconnected");
						break;
					case 'e':
						Disconnect();
						Environment.Exit(0);
						break;
					default:
						Console.WriteLine("None of the above were selected");
						break;
				}
			} while (true);
		}

		private void ChooseProtocol()
		{
			string value;
			do
			{
				Console.WriteLine("Enter the name of the protocol you want to use\n" +
					"'UDP' or 'TCP'? ");
				value = Console.ReadLine();
			} while (value.ToLower() != "udp" && value.ToLower() != "tcp");
			isUDP = value.ToLower() == "udp";
		}
		private void isIPOk()
		{
			bool IpOk;
			byte tempForParsing;
			string[] splitValues;
			do
			{
				Console.WriteLine("Please enter a valid IP: ");
				ipAddr = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(ipAddr))
				{
					IpOk = false;
				}
				else
				{
					splitValues = ipAddr.Split('.');
					if (splitValues.Length != 4)
					{
						IpOk = false;
					}
					else
						IpOk = splitValues.All(r => byte.TryParse(r, out tempForParsing));
				}

			} while (!IpOk);
			Console.WriteLine(ipAddr + " Changed successfully");
		}
		private void isPortOk()
		{
			do
			{
				Console.WriteLine("Please provide an alternative Port:");
				textPort = Console.ReadLine();
				if (textPort == "")
				{
					Console.WriteLine("**Please provide correct port**");
					textPort = "8192";
				}
				else
				{
					int port;
					int.TryParse(textPort, out port);
					if (port == 0 || port > 47823)
					{
						Console.WriteLine("** " + port + "is Bigger than 47823 or zero, made by default to 47823**");
						port = 47823;
						textPort = "47823";
					}
				}

			} while (textPort == "" && Int32.Parse(textPort) > 47823);
			Console.WriteLine(textPort + " Changed successfully");
		}
		private void printSources()
		{
			//**see the full speakers list**
			if (comboBoxCodecs.Count == 0)
			{
				Console.WriteLine("No MIC source found");
				return;
			}
			foreach (CodecComboItem item in comboBoxCodecs)
			{
				Console.WriteLine(item.Text);
			}
		}
		private void ChoosePrintSources()
		{
			//**see the full speakers list**
			//**and then choose which one to use**
			if (comboBoxCodecs.Count == 0)
			{
				Console.WriteLine("No MIC source found");
				return;
			}
			Console.WriteLine("These are the possible sources:");
			for (int i = 0; i < comboBoxCodecs.Count; i++)
			{
				Console.WriteLine((i + 1) + ". " + comboBoxCodecs[i].Text.ToString());
			}
			Console.WriteLine("\nType in the number in the according line: ");


			Int32.TryParse(Console.ReadLine(), out comboBoxCodecsIndex);
			--comboBoxCodecsIndex;
			if (comboBoxCodecsIndex > 0 && comboBoxCodecsIndex <= comboBoxCodecs.Count)
			{
				Console.WriteLine("Device " + comboBoxCodecsIndex + 1 + " selected successfully.");
			}
			else
			{
				comboBoxCodecsIndex = 0;
				Console.WriteLine("Couldn't select Device " + comboBoxCodecsIndex + 1 + ", is first one By Default");
			}
		}
		private void PopulateCodecsCombo(IEnumerable<INetworkChatCodec> codecs)
		{
			comboBoxCodecs.Clear();
			var sorted = from codec in codecs
						 where codec.IsAvailable
						 orderby codec.BitsPerSecond ascending
						 select codec;

			foreach (var codec in sorted)
			{
				var bitRate = codec.BitsPerSecond == -1 ? "VBR" : $"{codec.BitsPerSecond / 1000.0:0.#}kbps";
				var text = $"{codec.Name} ({bitRate})";
				comboBoxCodecs.Add(new CodecComboItem { Text = text, Codec = codec });
			}
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
			for (int n = 0; n < WaveIn.DeviceCount; n++)
			{
				var capabilities = WaveIn.GetCapabilities(n);
				comboBoxInputDevices.Add(capabilities.ProductName);
			}
			if (comboBoxInputDevices.Count > 0)
			{
				comboBoxCodecsIndex = 0;
			}
		}

		private void StartStreaming()
		{
			try
			{
				IPEndPoint endPoint = CreateIPEndPoint(ipAddr + ":" + textPort);
				int inputDeviceNumber = comboBoxCodecsIndex;
				selectedCodec = ((CodecComboItem)comboBoxCodecs.First()).Codec;
				Connect(isUDP, endPoint, inputDeviceNumber, selectedCodec);
			}
			catch (Exception e)
			{
				if (e is NAudio.MmException)
					Console.WriteLine("No microphones are connected");
				else
					Console.WriteLine(e);
				Console.WriteLine("\n**remember, Please provide correct IP address**");
			}
		}
		private void Connect(bool isUDP, IPEndPoint endPoint, int inputDeviceNumber, INetworkChatCodec codec)
		{
			var receiver = (isUDP)
				? (IAudioReceiver)new UdpAudioReceiver(endPoint.Port)
				: new TcpAudioReceiver(endPoint.Port);
			var sender = (isUDP)
				? (IAudioSender)new UdpAudioSender(endPoint)
				: new TcpAudioSender(endPoint);

			player = new NetworkAudioPlayer(codec, receiver);
			audioSender = new NetworkAudioSender(codec, inputDeviceNumber, sender);
			connected = true;
		}

		private void Disconnect()
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
			string value;
			do
			{
				Console.WriteLine("please enter a Natural number value from 0 to 10");
				value = Console.ReadLine();
			} while (!(int.TryParse(value, out audioValue) && audioValue >= 0 && audioValue <= 10));

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
					Console.WriteLine("your IP: " + ip.ToString() + "\nalso this is the default delected IP adress");
					return ip.ToString();
				}
			}
			throw new Exception("No network adapters with an IPv4 address in the system!");
		}
	}
}
