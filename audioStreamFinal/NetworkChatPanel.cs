using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NAudio.Wave;
using audioStreamFinal.ReciverType;
using audioStreamFinal.SenderType;

namespace audioStreamFinal
{
	/// <summary>
	/// uses a nice console app interface with supporting functions for commands
	/// </summary>
	public class NetworkChatPanel
	{
		/// <summary>
		/// making definitions of all my constants
		/// </summary>
		class MyConst
		{
			//constants from isIPOk()
			public const int ipV4Len = 4;
			//constants fr om isPortOk()
			public const int defaultPort = 8192;
			public const int maxPort = 47823;
			//constants from ChooseInputDevicesSource()
			public const int defaultInputDevicesIndex = 0;
			//constants from ChooseCodecSource()
			public const int defaultCodecsIndex = 0;
			//constants from PopulateCodecs()
			public const double kilo = 1000.0;
			//constants from outputVolumeControl()
			public const int maxVolume = 10;
			public const int minVolume = 0;
			public const int volumeOffset = 0x0000ffff;
			public const int pushVolumeOffset = 16;

		}
		/// <summary>
		/// An object type which contains the Naudio WaveFormat and detailes about the wave
		/// </summary>
		private INetworkChatCodec selectedCodec;
		/// <summary>
		/// A boolean type which sets to true when a connection is made to make sure That no duplicates will be created
		/// </summary>
		private volatile bool connected;
		/// <summary>
		/// An object type which recieves the stream with Naudio IAudioReceiver, saves on the BufferedWaveProvider and plays with the IWavePlayer.
		/// </summary>
		private NetworkAudioPlayer player;
		/// <summary>
		/// An object type which recieves the stream with Naudio IAudioSender and records audio from WaveInEvent (notice that waveIn type doen't work on consoleApp)
		/// </summary>
		private NetworkAudioSender audioSender;
		/// <summary>
		/// List of all devices marked as Microphones connected.
		/// </summary>
		private List<string> InputDevices;
		private List<CodecItem> Codecs;
		/// <summary>
		/// CodecsIndex saves the number in the list of available microphones to be used and is read in the next connection attempt
		/// </summary>
		private int InputDevicesIndex;
		private int CodecsIndex;
		/// <summary>
		/// A boolean type which sets to true when the connection is set to be used with the udp protocol and TCP otherwise
		/// </summary>
		private bool isUDP;
		/// <summary>
		/// contains the IP in string format to be sent to the client side
		/// </summary>
		string ipAddr;
		/// <summary>
		/// contains the Port in string format to be sent to the client side
		/// </summary>
		string textPort;
		/// <summary>
		/// An integer which represents volume value from 1 up to 10
		/// </summary>
		int audioValue;
		/// <summary>
		/// Set the current volume on computer
		/// </summary>
		/// <param name="hwo"></param>
		/// <param name="dwVolume"></param>
		/// <returns></returns>
		[DllImport("winmm.dll")]
		public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);
		/// <summary>
		/// Get a list of codecs of microphones and call user input 
		/// </summary>
		public NetworkChatPanel()
		{
			//defining variables
			connected = false;
			InputDevices= new List<string>();
			Codecs = new List<CodecItem>();
			InputDevicesIndex = MyConst.defaultInputDevicesIndex;
			CodecsIndex = MyConst.defaultCodecsIndex;
			isUDP = true;
			ipAddr = GetLocalIPAddress();
			textPort = MyConst.defaultPort.ToString();
			audioValue = MyConst.maxVolume;

			Console.WriteLine("Next connection detailes: {0}:{1}\n" +
				"also these are the default IP adress and Port", ipAddr, textPort);
			// Use reflection to find all the codecs and populate the codec list with them
			PopulateInputDevices();
			PopulateCodecs();

			consoleUserInterface();
		}
		/// <summary>
		/// Get a single char from the client and manage the conversasion between audio streaming functions and the User
		/// </summary>
		public void consoleUserInterface()
		{
			char input;
			do
			{

				Console.WriteLine("\nR - Refresh sources\n" +
					"C - Choose source microphone\n" +
					"K - Choose Compression Type\n" +
					"I - Choose IP to connect\n" +
					"O - revert to original IP and port\n" +
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
						printInputDevices();
						break;
					case 'c':
						ChooseInputDevicesSource();
						break;
					case 'k':
						ChooseCodecSource();
						break;
					case 'i':
						isIPOk();
						Console.WriteLine("\n# {0}:{1} is your current Connection parameters.", ipAddr, textPort);
						break;
					case 'o':
						ipAddr = GetLocalIPAddress();
						textPort = "8192";
						Console.WriteLine("\n# {0}:{1} is your current Connection parameters.", ipAddr, textPort);
						break;
					case 'p':
						Console.WriteLine("\n# " + textPort + " is your current Port.");
						isPortOk();
						Console.WriteLine("\n# {0}:{1} is your current Connection parameters.", ipAddr, textPort);
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
						break;
					case 'd':
						Disconnect();
						break;
					case 'e':
						Disconnect();
						break;
					default:
						Console.WriteLine("None of the above were selected");
						break;
				}
			} while (input!='e');
		}
		/// <summary>
		/// Get a single character from user and ask again if needed
		/// </summary>
		/// <returns>A lowercase user input</returns>
		private char inputCharOnly()
		{
			string try_input;
			do
			{
				Console.WriteLine("enter a single character: ");
				try_input = Console.ReadLine();
			} while (try_input.Length != 1);
			return Char.ToLower(char.Parse(try_input));

		}
		/// <summary>
		/// user inputs 'UDP' or 'TCP' and the boolean isUDP changes accordingly 
		/// </summary>
		private void ChooseProtocol()
		{
			string value;
			do
			{
				Console.WriteLine("Enter the name of the protocol you want to use\n" +
					"'UDP' or 'TCP'? ");
				value = Console.ReadLine().ToLower();
			} while (value != "udp" && value != "tcp");
			isUDP = value == "udp";
		}
		/// <summary>
		/// makes sure the String ipAddr is Valid: only numbers and 3 dots between up to 3 digits eatch.
		/// </summary>
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
					if (splitValues.Length != MyConst.ipV4Len)
					{
						IpOk = false;
					}
					else
						IpOk = splitValues.All(r => byte.TryParse(r, out tempForParsing));
				}

			} while (!IpOk);
			Console.WriteLine(ipAddr + " Changed successfully");
		}
		/// <summary>
		/// makes sure the String textPort gets user input in the valid range of ports available.
		/// </summary>
		private void isPortOk()
		{
			Console.WriteLine("Please provide an alternative Port:");
			textPort = Console.ReadLine();
			if (textPort == "")
			{
				Console.WriteLine("**Please provide correct port next time, changed to default**");
				textPort = MyConst.defaultPort.ToString() ;
			}
			else
			{
				int port;
				int.TryParse(textPort, out port);
				if (port == 0 || port >= MyConst.maxPort)
				{
					Console.WriteLine("** Your input was submitted as: {0}\t**\n" +
						"** Which is Bigger than {1} or Zero\t**\n" +
						"** or containes a Non-Number character,\t**\n" +
						"** Input made by default to {1}\t**", port, MyConst.maxPort);
					port = MyConst.maxPort;
					textPort = port.ToString();
				}
			}
			Console.WriteLine(textPort + " Changed successfully");
		}
		/// <summary>
		///  see the full speakers list
		/// </summary>
		private void printInputDevices()
		{
			if (InputDevices.Count == 0)
			{
				Console.WriteLine("No MIC source found");
				return;
			}
			for (int i = 0; i < InputDevices.Count; i++)
			{
				Console.WriteLine("{0}. {1}", (i + 1) , InputDevices[i]);
			}
		}
		/// <summary>
		/// call printInputDevices() and then choose which Mic to use
		/// </summary>
		private void ChooseInputDevicesSource()
		{

			Console.WriteLine("These are the possible sources:");
			printInputDevices();
			Console.WriteLine("\nType in the number in the according line: ");
			Int32.TryParse(Console.ReadLine(), out InputDevicesIndex);
			//check in range
			if (InputDevicesIndex > 0 && InputDevicesIndex <= InputDevices.Count)
			{
				Console.WriteLine("Device " + InputDevicesIndex + " selected successfully.");
			}
			else
			{
				InputDevicesIndex = MyConst.defaultInputDevicesIndex;
				Console.WriteLine("Couldn't select Device " + InputDevicesIndex + ", is chosen one By Default");
			}
			--InputDevicesIndex;
		}
		/// <summary>
		/// see the Compression option list
		/// </summary>
		private void printCodecs()
		{
			if (Codecs.Count == 0)
			{
				Console.WriteLine("No Codecs found");
				return;
			}
			for (int i = 0; i < Codecs.Count; i++)
			{
				Console.WriteLine("{0}. {1}", (i + 1), Codecs[i].Text);
			}
		}
		/// <summary>
		/// call printCodecs() and then choose which one to use
		/// </summary>
		private void ChooseCodecSource()
		{

			Console.WriteLine("These are the possible sources:");
			printCodecs();
			Console.WriteLine("\nType in the number in the according line: ");
			Int32.TryParse(Console.ReadLine(), out CodecsIndex);
			//check in range
			if (CodecsIndex > 0 && CodecsIndex <= Codecs.Count)
			{
				Console.WriteLine("Device " + CodecsIndex + " selected successfully.");
			}
			else
			{
				CodecsIndex = MyConst.defaultCodecsIndex;
				Console.WriteLine("Couldn't select Device " + CodecsIndex + ", is chosen one By Default");
			}
			--CodecsIndex;
		}
		/// <summary>
		/// Add Connected Microphones Names to NameList 
		/// </summary>
		private void PopulateInputDevices()
		{
			for (int n = 0; n < WaveIn.DeviceCount; n++)
			{
				var capabilities = WaveIn.GetCapabilities(n);
				InputDevices.Add(capabilities.ProductName);
			}
			if (InputDevices.Count == 0)
			{
				Console.WriteLine("** No Input Devices Connected **");
			}
		}
		/// <summary>
		/// Add INetworkChatCodec detailes about Codecs 
		/// </summary>
		private void PopulateCodecs()
		{
			IEnumerable<INetworkChatCodec> codecs = ReflectionHelperInstances.CreatAllInstancesOf<INetworkChatCodec>();
			var sorted = from codec in codecs
						 where codec.IsAvailable
						 orderby codec.BitsPerSecond ascending
						 select codec;

			foreach (var codec in sorted)
			{
				var bitRate = codec.BitsPerSecond == -1 ? "VBR" : $"{codec.BitsPerSecond / MyConst.kilo:0.#}kbps";
				var text = $"{codec.Name} ({bitRate})";
				Codecs.Add(new CodecItem { Text = text, Codec = codec });
			}
		}
		/// <summary>
		/// Basic Data saved on every Compression option
		/// </summary>
		class CodecItem
		{
			public string Text { get; set; }
			public INetworkChatCodec Codec { get; set; }
		}
		/// <summary>
		/// endsure everything for connection and catch if connection failes
		/// </summary>
		private void StartStreaming()
		{
			if (!connected)
			{
				try
				{
					IPEndPoint endPoint = CreateIPEndPoint(ipAddr + ":" + textPort);
					selectedCodec = ((CodecItem)Codecs[CodecsIndex]).Codec;
					Connect(endPoint);
					Console.WriteLine("-Connected");
				}
				catch (Exception e)
				{
					if (e is NAudio.MmException) Console.WriteLine("No microphones are connected, STILL CONNECTED FOR LIISTENING!!!");
					else
						Console.WriteLine(e);
						Console.WriteLine("\n**remember to Listen on the correct IP address**");
				}
			}
		}
		/// <summary>
		/// create sender and reciever and connect to IP:Port with selected protocol
		/// </summary>
		/// <param name="isUDP"></param>
		/// <param name="endPoint"></param>
		/// <param name="inputDeviceNumber"></param>
		private void Connect(IPEndPoint endPoint)
		{
			var receiver = (isUDP)
				? (IAudioReceiver)new UdpAudioReceiver(endPoint.Port)
				: new TcpAudioReceiver(endPoint.Port);
			var sender = (isUDP)
				? (IAudioSender)new UdpAudioSender(endPoint)
				: new TcpAudioSender(endPoint);

			player = new NetworkAudioPlayer(selectedCodec, receiver);
			audioSender = new NetworkAudioSender(selectedCodec, InputDevicesIndex, sender);
			connected = true;
		}
		/// <summary>
		/// Dispose everything connection related
		/// </summary>
		private void Disconnect()
		{
			if (connected)
			{
				connected = false;

				player.Dispose();
				audioSender.Dispose();
				selectedCodec.Dispose();
				saveIpPort();
				Console.WriteLine("-Disconnected");
			}
		}
		/// <summary>
		/// save ip and port for further connections, useful after closer of last thread *********not done.
		/// </summary>
		private void saveIpPort()
		{
			Properties.Settings.Default.IP = ipAddr;
			Properties.Settings.Default.Port = textPort;
			Properties.Settings.Default.Save();
		}
		/// <summary>
		/// Get value for volume output and adjust volume accordingly
		/// </summary>
		private void outputVolumeControl()
		{
			//Main audio output volume control
			string value;
			do
			{
				Console.WriteLine("please enter a Natural number value from 0 to 10");
				value = Console.ReadLine();
			} while (!(int.TryParse(value, out audioValue) && audioValue >= MyConst.minVolume && audioValue <= MyConst.maxVolume));

			int newVolume = ((ushort.MaxValue / MyConst.maxVolume) * audioValue);
			uint NewVolumeAllChannels = (((uint)newVolume & MyConst.volumeOffset) | ((uint)newVolume << MyConst.pushVolumeOffset));
			waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);
		}
		/// <summary>
		/// try creating IPEndPoint object from IP:port string
		/// </summary>
		/// <param name="endPoint"></param>
		/// <returns>IPEndPoint connection detailes for streaming</returns>
		private IPEndPoint CreateIPEndPoint(string endPoint)
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
		/// <summary>
		/// Get IP of console pc for loopback connection by default
		/// </summary>
		/// <returns>local ip address in string format</returns>
		private string GetLocalIPAddress()
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (var ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					return ip.ToString();
				}
			}
			throw new Exception("No network adapters with an IPv4 address in the system!");
		}
	}
}
