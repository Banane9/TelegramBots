using System.Linq;
using Newtonsoft.Json;

using System.Linq;

namespace Banane9.TelegramBots.ArtChannelBot.Data
{
    [JsonObject]
    public sealed class Artwork
    {
        [JsonProperty("artists")]
        public string[] Artists { get; private set; }

        [JsonProperty("characters")]
        public string[] Characters { get; private set; }

        [JsonProperty("fileId")]
        public string FileId { get; private set; }

        [JsonProperty("messageId")]
        public int MessageId { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("tags")]
        public string[] Tags { get; private set; }

        private Artwork()
        { }

        public static Artwork ParseFromMessage(string message, int id, string fileId)
        {
            var artwork = new Artwork();
            artwork.MessageId = id;
            artwork.FileId = fileId;

            var parts = message.Split(';').Select(p => p.Trim());

            foreach (var part in parts)
            {
                var nameValue = part.Split(':').Select(p => p.Trim()).ToArray();

                switch (nameValue[0].ToLowerInvariant())
                {
                    case "artist":
                    case "artists":
                        artwork.Artists = nameValue[1].Split(',').Select(p => p.Trim()).ToArray();
                        break;

                    case "character":
                    case "characters":
                        artwork.Characters = nameValue[1].Split(',').Select(p => p.Trim()).ToArray();
                        break;

                    case "tag":
                    case "tags":
                        artwork.Tags = nameValue[1].Split(',').Select(p => p.Trim().ToLowerInvariant()).ToArray();
                        break;

                    case "name":
                        artwork.Name = nameValue[1];
                        break;
                }
            }

            return artwork;
        }
    }
}