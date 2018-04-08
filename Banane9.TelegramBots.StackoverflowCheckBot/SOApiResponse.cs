using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Banane9.TelegramBots.StackoverflowCheck
{
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public sealed class SOApiResponse
    {
        [JsonProperty("items")]
        public Question[] Items { get; private set; }

        [JsonObject]
        public sealed class Question
        {
            [JsonProperty("link")]
            public string Link { get; private set; }

            [JsonProperty("title")]
            public string Title { get; private set; }
        }
    }
}