using System;
using System.Linq;
using System.IO;

using System.Linq;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using TelegramBotLib;

namespace Banane9.TelegramBots.StackoverflowCheck
{
    public sealed class StackoverflowCheckBot : TelegramBot
    {
        private static readonly JsonSerializer serializer = new JsonSerializer();
        private static DateTime linuxEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private readonly HttpClient httpClient;
        private readonly Settings settings;
        private readonly Timer timer;
        private TimeSpan updateTime;

        public TimeSpan UpdateTime
        {
            get { return updateTime; }
            set
            {
                updateTime = value;
                timer.Change(TimeSpan.FromSeconds(10), updateTime);
            }
        }

        public StackoverflowCheckBot(string token, TimeSpan updateTime, string settingsPath = "Settings.json")
            : base(token)
        {
            this.updateTime = updateTime;

            settings = new Settings(settingsPath);

            timer = new Timer(checkForUpdates, null, TimeSpan.FromSeconds(10), updateTime);

            var clientHandler = new HttpClientHandler();
            if (clientHandler.SupportsAutomaticDecompression)
                clientHandler.AutomaticDecompression = DecompressionMethods.GZip;

            httpClient = new HttpClient(clientHandler);

            httpClient.DefaultRequestHeaders.Add("Host", "api.stackexchange.com");
            httpClient.DefaultRequestHeaders.Add("UserAgent", "Stackoverflow Update Telegram Bot");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json; charset=UTF-8");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");

            if (clientHandler.SupportsAutomaticDecompression)
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");

            Console.WriteLine((clientHandler.SupportsAutomaticDecompression ? "Supports" : "Doesn't support") + " Compression");
        }

        protected override void OnChannelPost(Message channelPost)
        {
            var parts = channelPost.Text?.ToLowerInvariant().Split(' ') ?? new string[0];

            if (parts.Length < 2 || (!parts[0].StartsWith("/add") && !parts[0].StartsWith("/remove")))
                return;

            var search = string.Join(";", parts.Skip(1));

            switch (parts[0])
            {
                case "/add":
                    addSearch(channelPost.Chat, search);
                    break;

                case "/remove":
                    removeSearch(channelPost.Chat, search);
                    break;
            }
        }

        private void addSearch(ChatId channel, string search)
        {
            lock (settings)
            {
                if (settings.Searches.ContainsKey(search))
                {
                    if (!settings.Searches[search].Channels.Contains(channel))
                        settings.Searches[search].Channels.Add(channel);

                    return;
                }

                var searchEntry = new SearchEntry(channel, search, (DateTime.Now - UpdateTime));
                settings.Searches.Add(search, searchEntry);

                update(searchEntry);

                settings.Save();
            }
        }

        private void checkForUpdates(object _)
        {
            lock (settings)
            {
                foreach (var search in settings.Searches)
                    update(search.Value);

                settings.Save();
            }
        }

        private void removeSearch(ChatId channel, string search)
        {
            lock (settings)
            {
                if (!settings.Searches.ContainsKey(search))
                    return;

                settings.Searches[search].Channels.Remove(channel);

                if (settings.Searches[search].Channels.Count == 0)
                    settings.Searches.Remove(search);

                settings.Save();
            }
        }

        private void update(SearchEntry searchEntry)
        {
            try
            {
                var now = DateTime.Now;
                var fromDate = (long)(searchEntry.LastCheck.ToUniversalTime() - linuxEpoch).TotalSeconds;
                var url = $"https://api.stackexchange.com/2.2/questions?fromdate={fromDate}&order=asc&sort=creation&tagged={HttpUtility.UrlEncode(searchEntry.Search)}&site=stackoverflow&filter=!C(o*VZFDya.Q_ppGK";
                Console.WriteLine(url);
                var response = httpClient.GetStreamAsync(url).Result;
                var result = serializer.Deserialize<SOApiResponse>(new JsonTextReader(new StreamReader(response)));

                foreach (var question in result.Items)
                    foreach (var channel in searchEntry.Channels)
                        client.SendTextMessageAsync(channel, $"{HttpUtility.HtmlDecode(question.Title)}\r\n{question.Link}");

                searchEntry.LastCheck = now;
                Console.WriteLine("Done with " + result.Items.Length);
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e.InnerExceptions[0].Message);
                Console.WriteLine(e.InnerExceptions[0].StackTrace);
            }
        }
    }
}