using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using AudioStream.NAudioStreamServices.CompressionType;
using AudioStream.NAudioStreamServices.ReceiverType;
using AudioStream.NAudioStreamServices.Reflection_helper;
using AudioStream.NAudioStreamServices.SenderType;
using NAudio;
using NAudio.Wave;

namespace AudioStream.Panel
{
    /// <summary>
    /// uses a nice console app interface with supporting functions for commands
    /// </summary>
    public class NetworkChatPanel
    {
        /// <summary>
        /// An object type which contains the NAudio WaveFormat and details about the wave
        /// </summary>
        private INetworkChatCodec SelectedCodec;

        /// <summary>
        /// A boolean type which sets to true when a connection is made to make sure That no duplicates will be created
        /// </summary>
        private volatile bool Connected;

        /// <summary>
        /// An object type which receives the stream with NAudio IAudioReceiver, saves on the BufferedWaveProvider and plays with the IWavePlayer.
        /// </summary>
        private NetworkAudioPlayer Player;

        /// <summary>
        /// An object type which receives the stream with NAudio IAudioSender and records audio from WaveInEvent (notice that waveIn type doesn't work on consoleApp)
        /// </summary>
        private NetworkAudioSender AudioSender;

        /// <summary>
        /// List of all devices marked as Microphones connected.
        /// </summary>
        private readonly List<string> InputDevices;

        private readonly List<CodecItem> Codecs;

        /// <summary>
        /// CodecsIndex saves the number in the list of available microphones to be used and is read in the next connection attempt
        /// </summary>
        private int InputDevicesIndex;

        private int CodecsIndex;

        /// <summary>
        /// A boolean type which sets to true when the connection is set to be used with the udp protocol and TCP otherwise
        /// </summary>
        private bool IsUdp;

        /// <summary>
        /// contains the IP in string format to be sent to the client side
        /// </summary>
        private string IpAddr;

        /// <summary>
        /// contains the Port in string format to be sent to the client side
        /// </summary>
        private string TextPort;

        /// <summary>
        /// An integer which represents volume value from 1 up to 10
        /// </summary>
        private int AudioValue;

        /// <summary>
        /// Set the current volume on computer
        /// </summary>
        /// <param name="hwo"></param>
        /// <param name="dwVolume"></param>
        /// <returns></returns>
        [DllImport("winmm.dll")]
        private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        /// <summary>
        /// Get a list of codecs of microphones and call user input 
        /// </summary>
        public NetworkChatPanel()
        {
            //defining variables
            Connected = false;
            InputDevices = new List<string>();
            Codecs = new List<CodecItem>();
            InputDevicesIndex = NetworkChatPanelConstants.DefaultInputDevicesIndex;
            CodecsIndex = NetworkChatPanelConstants.DefaultCodecsIndex;
            IsUdp = true;
            IpAddr = GetLocalIpAddress();
            TextPort = NetworkChatPanelConstants.DefaultPort.ToString();
            AudioValue = NetworkChatPanelConstants.MaxVolume;

            Console.WriteLine($@"Next connection details: {IpAddr}:{TextPort}
also these are the default IP address and Port");
            // Use reflection to find all the codecs and populate the codec list with them
            PopulateInputDevices();
            PopulateCodecs();

            ConsoleUserInterface();
        }

        /// <summary>
        /// Get a single char from the client and manage the conversation between audio streaming functions and the User
        /// </summary>
        private void ConsoleUserInterface()
        {
            char input;
            do
            {
                Console.WriteLine(@"
R - Refresh sources
C - Choose source microphone
K - Choose Compression Type
I - Choose IP to connect
O - revert to original IP and port
P - Choose Port to connect
V - Change audio volume
U - Choose transmission protocol
S - Start
D - Disconnect
E - Exit");

                input = inputCharOnly();
                switch (input)
                {
                    case 'r':
                        PrintInputDevices();
                        break;
                    case 'c':
                        ChooseInputDevicesSource();
                        break;
                    case 'k':
                        ChooseCodecSource();
                        break;
                    case 'i':
                        IsIpOk();
                        break;
                    case 'o':
                        IpAddr = GetLocalIpAddress();
                        TextPort = "8192";
                        NetworkChatPanelController.ShowIpAndPort(IpAddr, TextPort);
                        break;
                    case 'p':
                        IsPortOk();
                        break;
                    case 'v':
                        OutputVolumeControl();
                        Console.WriteLine($@"# {AudioValue} is your current Audio volume.");
                        break;
                    case 'u':
                        ChooseProtocol();
                        var stringIsUdp = IsUdp ? "UDP" : "TCP";
                        Console.WriteLine($@"# {stringIsUdp} is your current Audio volume.");
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
                        Console.WriteLine(@"None of the above were selected");
                        break;
                }
            } while (input != 'e');
        }

        /// <summary>
        /// Get a single character from user and ask again if needed
        /// </summary>
        /// <returns>A lowercase user input</returns>
        private static char inputCharOnly()
        {
            string tryInput;
            do
            {
                Console.WriteLine(@"enter a single character: ");
                tryInput = Console.ReadLine();
            } while (tryInput != null && tryInput.Length != 1);

            return char.ToLower(char.Parse(tryInput ?? string.Empty));
        }

        /// <summary>
        /// user inputs 'UDP' or 'TCP' and the boolean isUDP changes accordingly 
        /// </summary>
        private void ChooseProtocol()
        {
            string value;
            do
            {
                Console.WriteLine(@"Enter the name of the protocol you want to use
'UDP' or 'TCP'? ");
                value = Console.ReadLine()?.ToLower();
            } while (value != "udp" && value != "tcp");

            IsUdp = value == "udp";
        }

        /// <summary>
        /// makes sure the String ipAddr is Valid: only numbers and 3 dots between up to 3 digits each.
        /// </summary>
        private void IsIpOk()
        {
            bool ipOk;
            do
            {
                Console.WriteLine(@"Please enter a valid IP: ");
                IpAddr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(IpAddr))
                {
                    ipOk = false;
                }
                else
                {
                    var splitValues = IpAddr.Split('.');
                    ipOk = splitValues.Length == NetworkChatPanelConstants.IpV4Len &&
                           splitValues.All(r => byte.TryParse(r, out _));
                }
            } while (!ipOk);

            Console.WriteLine($@"{IpAddr} Changed successfully");
            NetworkChatPanelController.ShowIpAndPort(IpAddr, TextPort);
        }

        /// <summary>
        /// makes sure the String textPort gets user input in the valid range of ports available.
        /// </summary>
        private void IsPortOk()
        {
            NetworkChatPanelController.ShowIpAndPort(IpAddr, TextPort);
            Console.WriteLine(@"Please provide an alternative Port:");
            TextPort = Console.ReadLine();
            if (TextPort == "")
            {
                Console.WriteLine(@"**Please provide correct port next time, changed to default**");
                TextPort = NetworkChatPanelConstants.DefaultPort.ToString();
            }
            else
            {
                var port = NetworkChatPanelController.GetIntFromUser();

                if (port == 0 || port >= NetworkChatPanelConstants.MaxPort)
                {
                    Console.WriteLine(
                        $@"** Your input was submitted as: {port}  **
** Which is Bigger than {NetworkChatPanelConstants.MaxPort} or Zero     **
** or contains a Non-Number character,  **
** Input made by default to {NetworkChatPanelConstants.MaxPort}         **");
                    port = NetworkChatPanelConstants.MaxPort;
                    TextPort = port.ToString();
                }
            }

            Console.WriteLine($@"{TextPort} Changed successfully");
            NetworkChatPanelController.ShowIpAndPort(IpAddr, TextPort);
        }

        /// <summary>
        ///  see the full speakers list
        /// </summary>
        private void PrintInputDevices()
        {
            if (InputDevices.Count == 0)
            {
                Console.WriteLine(@"No MIC source found");
                return;
            }

            for (var i = 0; i < InputDevices.Count; i++)
            {
                Console.WriteLine($@"{i + 1}. {InputDevices[i]}");
            }
        }

        /// <summary>
        /// call printInputDevices() and then choose which Mic to use
        /// </summary>
        private void ChooseInputDevicesSource()
        {
            Console.WriteLine(@"These are the possible sources:");
            PrintInputDevices();
            Console.WriteLine(@"Type in the number in the according line: ");

            InputDevicesIndex = NetworkChatPanelController.GetIntFromUser();

            if (InputDevicesIndex > 0 && InputDevicesIndex <= InputDevices.Count)
            {
                Console.WriteLine($@"Device {InputDevicesIndex} selected successfully.");
            }
            else
            {
                InputDevicesIndex = NetworkChatPanelConstants.DefaultInputDevicesIndex;
                Console.WriteLine($@"Couldn't select Device {InputDevicesIndex}, one is chosen By Default");
            }

            --InputDevicesIndex;
        }

        /// <summary>
        /// see the Compression option list
        /// </summary>
        private void PrintCodecs()
        {
            if (Codecs.Count == 0)
            {
                Console.WriteLine(@"No Codecs found");
                return;
            }

            for (var i = 0; i < Codecs.Count; i++)
            {
                Console.WriteLine($@"{i + 1}. {Codecs[i].Text}");
            }
        }

        /// <summary>
        /// call printCodecs() and then choose which one to use
        /// </summary>
        private void ChooseCodecSource()
        {
            Console.WriteLine(@"These are the possible sources:");
            PrintCodecs();
            Console.WriteLine(@"
Type in the number in the according line: ");

            CodecsIndex = NetworkChatPanelController.GetIntFromUser();

            //check in range
            if (CodecsIndex > 0 && CodecsIndex <= Codecs.Count)
            {
                Console.WriteLine($@"Device {CodecsIndex} selected successfully.");
            }
            else
            {
                CodecsIndex = NetworkChatPanelConstants.DefaultCodecsIndex;
                Console.WriteLine($@"Couldn't select Device {CodecsIndex}, is chosen one By Default");
            }

            --CodecsIndex;
        }

        /// <summary>
        /// Add Connected Microphones Names to NameList 
        /// </summary>
        private void PopulateInputDevices()
        {
            for (var n = 0; n < WaveIn.DeviceCount; n++)
            {
                var capabilities = WaveIn.GetCapabilities(n);
                InputDevices.Add(capabilities.ProductName);
            }

            if (InputDevices.Count == 0)
            {
                Console.WriteLine(@"** No Input Devices Connected **");
            }
        }

        /// <summary>
        /// Add INetworkChatCodec details about Codecs 
        /// </summary>
        private void PopulateCodecs()
        {
            var codecs = ReflectionHelperInstances.CreatAllInstancesOf<INetworkChatCodec>();
            var sorted = from codec in codecs
                where codec.IsAvailable
                orderby codec.BitsPerSecond
                select codec;

            foreach (var codec in sorted)
            {
                var bitRate = codec.BitsPerSecond == -1
                    ? "VBR"
                    : $"{codec.BitsPerSecond / NetworkChatPanelConstants.Kilo:0.#}kbps";
                var text = $"{codec.Name} ({bitRate})";
                Codecs.Add(new CodecItem {Text = text, Codec = codec});
            }
        }

        /// <summary>
        /// Basic Data saved on every Compression option
        /// </summary>
        private class CodecItem
        {
            public string Text { get; init; }
            public INetworkChatCodec Codec { get; init; }
        }

        /// <summary>
        /// ensure everything for connection and catch if connection fails
        /// </summary>
        private void StartStreaming()
        {
            if (Connected) return;
            try
            {
                var endPoint = NetworkChatPanelController.CreateIpEndPoint($@"{IpAddr}:{TextPort}");
                SelectedCodec = Codecs[CodecsIndex].Codec;
                Connect(endPoint);
                Console.WriteLine(@"-Connected");
            }
            catch (Exception e)
            {
                if (e is MmException)
                    Console.WriteLine(@"No microphones are connected, STILL CONNECTED FOR LISTENING!!!");
                else
                    Console.WriteLine(e);
                Console.WriteLine(@"
**remember to Listen on the correct IP address**");
            }
        }

        /// <summary>
        /// create sender and receiver and connect to IP:Port with selected protocol
        /// </summary>
        /// <param name="endPoint"></param>
        private void Connect(IPEndPoint endPoint)
        {
            var receiver = IsUdp
                ? (IAudioReceiver) new UdpAudioReceiver(endPoint.Port)
                : new TcpAudioReceiver(endPoint.Port);
            var sender = IsUdp
                ? (IAudioSender) new UdpAudioSender(endPoint)
                : new TcpAudioSender(endPoint);

            Player = new NetworkAudioPlayer(SelectedCodec, receiver);
            AudioSender = new NetworkAudioSender(SelectedCodec, InputDevicesIndex, sender);
            Connected = true;
        }

        /// <summary>
        /// Dispose everything connection related
        /// </summary>
        private void Disconnect()
        {
            if (!Connected) return;
            Connected = false;

            Player.Dispose();
            AudioSender.Dispose();
            SelectedCodec.Dispose();
            Console.WriteLine(@"-Disconnected");
        }

        /// <summary>
        /// Get value for volume output and adjust volume accordingly
        /// </summary>
        private void OutputVolumeControl()
        {
            //Main audio output volume control
            string value;
            do
            {
                Console.WriteLine(@"please enter a Natural number value from 0 to 10");
                value = Console.ReadLine();
            } while (!(int.TryParse(value, out AudioValue) && AudioValue >= NetworkChatPanelConstants.MinVolume &&
                       AudioValue <= NetworkChatPanelConstants.MaxVolume));

            var newVolume = ushort.MaxValue / NetworkChatPanelConstants.MaxVolume * AudioValue;
            var newVolumeAllChannels = ((uint) newVolume & NetworkChatPanelConstants.VolumeOffset) |
                                       ((uint) newVolume << NetworkChatPanelConstants.PushVolumeOffset);
            waveOutSetVolume(IntPtr.Zero, newVolumeAllChannels);
        }

        /// <summary>
        /// Get IP of console pc for loopback connection by default
        /// </summary>
        /// <returns>local ip address in string format</returns>
        private static string GetLocalIpAddress()
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