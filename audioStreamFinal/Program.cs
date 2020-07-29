using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace audioStreamFinal
{
	static class Program
	{
		[STAThread]
		static async Task Main()
		{
			new NetworkChatPanel();
			Console.ReadLine();
		}
	}
}
