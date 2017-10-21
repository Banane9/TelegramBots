using System.Data.SQLite;

namespace Banane9.TelegramBots.ArtChannelBot.Data
{
    public class Channel
    {
        public long Id { get; private set; }
        public long InternalId { get; private set; }
        public string Name { get; private set; }

        public Channel(SQLiteDataReader reader)
        {
            InternalId = reader.GetInt64(0);
            Name = reader.GetString(1);
            Id = reader.GetInt64(2);
        }

        public Channel(long internalId, string name, long id)
        {
            InternalId = internalId;
            Name = name;
            Id = id;
        }
    }
}