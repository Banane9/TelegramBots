using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Banane9.TelegramBots.ArtChannelBot.Data
{
    [JsonObject]
    public sealed class ArtChannel
    {
        [JsonProperty("artworks")]
        public List<Artwork> Artworks { get; private set; }

        [JsonProperty("id")]
        public long Id { get; private set; }

        public ArtChannel(long id)
            : this(id, new Artwork[0])
        { }

        public ArtChannel(long id, IEnumerable<Artwork> artworks)
        {
            Id = id;
            Artworks = new List<Artwork>(artworks);
        }

        private ArtChannel()
        { }
    }
}