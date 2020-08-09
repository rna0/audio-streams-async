﻿using System;
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
		string ipAddr = GetLocalIPAddress();//"192.168.1.167";
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

			//_ = StartStreamingAsync();
		}
		private char inputCharOnly()
		{
			string try_input;
			do
			{
				Console.WriteLine("enter a single character");
				try_input = Console.ReadLine();
			} while (try_input.Length != 1);
			return char.Parse(try_input);

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
					"P - Choose Port to connect\n" +
					"S - Start\n" +
					"D - Disconnect\n" +
					"E - Exit");

				input = inputCharOnly();
				switch (input)
				{
					case 'R':
					case 'r':
						PopulateCodecsCombo(ReflectionHelperInstances.CreatAllInstancesOf<INetworkChatCodec>());
						printSources();
						break;
					case 'C':
					case 'c':
						PopulateCodecsCombo(ReflectionHelperInstances.CreatAllInstancesOf<INetworkChatCodec>());
						ChoosePrintSources();
						break;
					case 'I':
					case 'i':
						isIPOk();
						Console.WriteLine(ipAddr + "is your current IP.");
						break;
					case 'P':
					case 'p':
						Console.WriteLine(textPort + " is your current Port.");
						isPortOk();
						break;
					case 'S':
					case 's':
						StartStreamingAsync();
						Console.WriteLine("-Connected");
						break;
					case 'D':
					case 'd':
						DisconnectAsync();
						Console.WriteLine("-Disconnected");
						break;
					case 'E':
					case 'e':
						DisconnectAsync();
						Environment.Exit(0);
						break;
					default:
						Console.WriteLine("None of the above were selected");
						break;
				}
			} while (true);
		}
		private void isIPOk()
		{
		}
		private void isPortOk()
		{
			do
			{
				Console.WriteLine("please provide an alternative Port:");
				textPort = Console.ReadLine();
				if (textPort == "")
				{
					Console.WriteLine("**Please provide correct port**");
					textPort = "8192";
				}
				int port;
				int.TryParse(textPort, out port);
				if (port == 0 || port > 65535)
				{
					Console.WriteLine("**Bigger than 47823, made by default to 47823**");
					port = 47823;
					textPort = "47823";
				}

			} while(textPort == "" && Int32.Parse(textPort) > 65535);
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
				Console.WriteLine("Device " + comboBoxCodecsIndex + " selected successfully.");
			}
			else
			{
				comboBoxCodecsIndex = 0;
				Console.WriteLine("Couldn't select Device " + comboBoxCodecsIndex + ", is first one By Default");
			}
		}
		private void PopulateCodecsCombo(IEnumerable<INetworkChatCodec> codecs)
		{
			comboBoxCodecs.Clear();
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
			try
			{
				IPEndPoint endPoint = CreateIPEndPoint(ipAddr + ":" + textPort);
				int inputDeviceNumber = comboBoxCodecsIndex;
				selectedCodec = ((CodecComboItem)comboBoxCodecs.First()).Codec;
				await ConnectAsync(isUDP, endPoint, inputDeviceNumber, selectedCodec);
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