using System;

namespace audioStreamFinal
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			new NetworkChatPanel();
			Console.ReadLine();
		}
	}
}
