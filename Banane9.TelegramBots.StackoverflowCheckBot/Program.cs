using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Banane9.TelegramBots.StackoverflowCheck
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = validateCertificate;

            try
            {
                var bot = new StackoverflowCheckBot("585936263:AAFnHPCnsBX6j4o-lecdOW5o-5R5eIRx1Yk", TimeSpan.FromMinutes(10));

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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private static bool validateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}