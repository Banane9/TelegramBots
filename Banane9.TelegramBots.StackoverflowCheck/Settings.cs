using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Banane9.TelegramBots.StackoverflowCheck
{
    public sealed class Settings
    {
        private static JsonSerializer serializer = new JsonSerializer();

        private readonly string path;
        public Dictionary<string, SearchEntry> LastUpdates { get; }

        public Settings(string path)
        {
            this.path = path;

            if (File.Exists(path))
                LastUpdates = serializer.Deserialize<Dictionary<string, SearchEntry>>(new JsonTextReader(new StreamReader(path)));
            else
                LastUpdates = new Dictionary<string, SearchEntry>();
        }

        public void Save()
        {
            var file = File.OpenWrite(path);
            var writer = new StreamWriter(file);

            serializer.Serialize(writer, LastUpdates);

            file.Close();
        }
    }
}