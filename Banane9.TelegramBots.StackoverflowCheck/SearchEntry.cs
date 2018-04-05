using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace Banane9.TelegramBots.StackoverflowCheck
{
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public sealed class SearchEntry
    {
        public List<ChatId> Channels { get; private set; }
        public DateTime LastCheck { get; set; }
        public string Search { get; private set; }

        public SearchEntry(ChatId channel, string search, DateTime lastCheck)
        {
            Channels = new List<ChatId> { channel };
            Search = search;
            LastCheck = lastCheck;
        }

        private SearchEntry()
        { }
    }
}