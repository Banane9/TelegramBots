using System;
using System.Collections.Generic;
using System.Linq;

namespace Banane9.TelegramBots.ArtChannelBot.Data
{
    public sealed class ArtworkDetails
    {
        public string[] Artists { get; private set; } = new string[0];

        public string[] Characters { get; private set; } = new string[0];

        public string Name { get; private set; } = "Unknown";

        public string Rating { get; private set; } = "Unknown";
        public string[] Tags { get; private set; } = new string[0];

        public ArtworkDetails(string message)
        {
            var parts = message.Split(';').Select(p => p.Trim());

            foreach (var part in parts)
            {
                var nameValue = part.Split(':').Select(p => p.Trim()).ToArray();

                if (nameValue.Length < 2)
                    continue;

                switch (nameValue[0].ToLowerInvariant())
                {
                    case "artist":
                    case "artists":
                        Artists = nameValue[1].Split(',').Select(p => p.Trim()).ToArray();
                        break;

                    case "character":
                    case "characters":
                        Characters = nameValue[1].Split(',').Select(p => p.Trim()).ToArray();
                        break;

                    case "tag":
                    case "tags":
                        Tags = nameValue[1].Split(',').Select(p => p.Trim().ToLowerInvariant()).ToArray();
                        break;

                    case "name":
                        Name = nameValue[1];
                        break;

                    case "rating":
                        Rating = nameValue[1];
                        break;
                }
            }
        }
    }
}