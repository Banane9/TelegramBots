using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Banane9.TelegramBots.StackoverflowCheck
{
    public sealed class Settings
    {
        private static readonly JsonSerializer serializer = new JsonSerializer();

        private readonly string path;
        public Dictionary<string, SearchEntry> Searches { get; }

        public Settings(string path)
        {
            this.path = path;

            if (File.Exists(path))
            {
                using (var reader = new StreamReader(File.OpenRead(path)))
                    Searches = serializer.Deserialize<Dictionary<string, SearchEntry>>(new JsonTextReader(reader));
            }

            if (Searches == null && File.Exists(path + ".bak"))
            {
                using (var reader = new StreamReader(File.OpenRead(path + ".bak")))
                    Searches = serializer.Deserialize<Dictionary<string, SearchEntry>>(new JsonTextReader(reader));
            }

            Searches = Searches ?? new Dictionary<string, SearchEntry>();

            Save(true);
        }

        public void Save(bool skipBak = false)
        {
            if (File.Exists(path))
            {
                if (!skipBak)
                    File.Copy(path, path + ".bak", true);

                File.Delete(path);
            }

            using (var writer = new StreamWriter(File.OpenWrite(path)))
                serializer.Serialize(writer, Searches);
        }
    }
}