using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace Banane9.TelegramBots.ArtChannelBot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var bot = new ArtChannelBot("471628940:AAES_fQeiL9UTmzlkJkAWbdgMKuzLyxiEYo");

            Console.Title = bot.Self.FirstName + bot.Self.LastName;

            bot.Start();

            if (args.Length < 1 && args[0] != "-d")
            {
                Console.WriteLine("Write quit to stop");
                while (Console.ReadLine() != "quit") ;
                bot.Stop();
                return;
            }

            while (true)
                Thread.Sleep(int.MaxValue);
        }
    }
}