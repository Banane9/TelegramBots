using System;
using System.Linq;

using System.Linq;

using System.Collections.Generic;

using System.Threading;
using Telegram.Bot.Types;
using TelegramBotLib;

namespace Banane9.TelegramBots.StackoverflowCheck
{
    public sealed class StackoverflowCheckBot : TelegramBot
    {
        private readonly Settings settings;
        private readonly Timer timer;
        private TimeSpan updateTime;

        public TimeSpan UpdateTime
        {
            get { return updateTime; }
            set
            {
                updateTime = value;
                timer.Change(TimeSpan.FromSeconds(0), updateTime);
            }
        }

        public StackoverflowCheckBot(string token, string settingsPath = "Settings.json", TimeSpan updateTime = default(TimeSpan))
            : base(token)
        {
            this.updateTime = updateTime;
            if (updateTime == default(TimeSpan))
                this.updateTime = TimeSpan.FromMinutes(10);

            timer = new Timer(checkForUpdates, null, TimeSpan.FromSeconds(0), updateTime);

            settings = new Settings(settingsPath);
        }

        protected override void OnChannelPost(Message channelPost)
        {
            var parts = channelPost.Text?.ToLowerInvariant().Split(' ') ?? new string[0];

            if (parts.Length < 2 || !parts[0].StartsWith("/add") || !parts[0].StartsWith("/remove"))
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
                if (settings.LastUpdates.ContainsKey(search))
                {
                    settings.LastUpdates[search].Channels.Add(channel);
                    return;
                }

                var searchEntry = new SearchEntry(channel, search, DateTime.MinValue);
                settings.LastUpdates.Add(search, searchEntry);
                settings.Save();

                update(searchEntry);
            }
        }

        private void checkForUpdates(object _)
        {
            lock (settings)
            {
                foreach (var search in settings.LastUpdates)
                {
                    if (search.Value.LastCheck > (DateTime.Now - UpdateTime))
                        continue;

                    update(search.Value);
                }
            }
        }

        private void removeSearch(ChatId channel, string search)
        {
            lock (settings)
            {
                if (!settings.LastUpdates.ContainsKey(search))
                    return;

                settings.LastUpdates[search].Channels.Remove(channel);

                if (settings.LastUpdates[search].Channels.Count == 0)
                    settings.LastUpdates.Remove(search);
            }
        }

        private void update(SearchEntry searchEntry)
        {
            throw new NotImplementedException();
        }
    }
}