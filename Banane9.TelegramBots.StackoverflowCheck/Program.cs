using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace Banane9.TelegramBots.StackoverflowCheck
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var bot = new StackoverflowCheckBot("585936263:AAFnHPCnsBX6j4o-lecdOW5o-5R5eIRx1Yk");

            Console.Title = bot.Self.FirstName + bot.Self.LastName;

            bot.Start();

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
    }
}