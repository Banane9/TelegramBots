using System;
using System.Linq;
using System.Collections.Generic;

namespace Banane9.TelegramBots.ArtChannelBot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var bot = new ArtChannelBot("471628940:AAES_fQeiL9UTmzlkJkAWbdgMKuzLyxiEYo");

            Console.Title = bot.Self.FirstName + bot.Self.LastName;

            bot.Start();
            while (Console.ReadLine() != "quit") ;
            bot.Stop();
        }
    }
}