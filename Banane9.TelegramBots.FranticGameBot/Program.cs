using System;
using System.Threading;

namespace Banane9.TelegramBots.FranticGameBot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var bot = new FranticGameBot("1135372298:AAF3qHz-DW2c1SCY4fjVAzqVOCHarrq5CWg");

                bot.Start();
                Console.Title = bot.Self.FirstName + bot.Self.LastName;

                if (args.Length < 1 || args[0] != "-d")
                {
                    Console.WriteLine("Write quit to stop");
                    while (Console.ReadLine() != "quit") ;
                    bot.Stop();
                    return;
                }

                while (true)
                    Thread.Sleep(int.MaxValue);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            Console.ReadLine();
        }
    }
}