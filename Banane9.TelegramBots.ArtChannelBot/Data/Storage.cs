using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Banane9.TelegramBots.ArtChannelBot.Data
{
    [JsonObject]
    public sealed class Storage
    {
        private static readonly JsonSerializer serializer = new JsonSerializer();

        [JsonProperty("artChannels")]
        public List<ArtChannel> ArtChannels { get; private set; }

        [JsonProperty("userChannels")]
        public Dictionary<int, List<long>> UserChannels { get; private set; }

        public Storage()
        {
            ArtChannels = new List<ArtChannel>();
            UserChannels = new Dictionary<int, List<long>>();
        }

        public static Storage LoadStorage(string name = "storage.json")
        {
            if (File.Exists(name))
                using (var reader = new StreamReader(name))
                    return serializer.Deserialize<Storage>(new JsonTextReader(reader));

            return new Storage();
        }

        public void WriteStorage(string name = "storage.json")
        {
            using (var writer = new StreamWriter(name))
                serializer.Serialize(new JsonTextWriter(writer), this);
        }
    }
}