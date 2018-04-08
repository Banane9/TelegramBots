using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Banane9.TelegramBots.StackoverflowCheck
{
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public sealed class SearchEntry
    {
        public List<string> Channels { get; private set; } = new List<string>();
        public DateTime LastCheck { get; set; }
        public string Search { get; private set; }

        public SearchEntry(string channel, string search, DateTime lastCheck)
        {
            if (channel != null)
                Channels.Add(channel);

            Search = search;
            LastCheck = lastCheck;
        }

        private SearchEntry()
        { }
    }
}