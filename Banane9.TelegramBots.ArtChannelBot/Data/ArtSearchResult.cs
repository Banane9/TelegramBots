using System.Data.Common;

namespace Banane9.TelegramBots.ArtChannelBot.Data
{
    //Channels.ChannelId AS ChannelId,
    //Channels.Name AS ChannelName,
    //Art.Id AS ArtId,
    //Art.MessageId AS MessageId,
    //Art.FileId AS FileId,
    //Art.Name AS ArtName
    public class ArtSearchResult
    {
        public long ArtId { get; private set; }
        public string ArtName { get; private set; }
        public long ChannelId { get; private set; }

        public string ChannelName { get; private set; }
        public string FileId { get; private set; }
        public int MessageId { get; private set; }

        public ArtSearchResult(DbDataReader reader)
        {
            ChannelId = reader.GetInt64(0);
            ChannelName = reader.GetString(1);
            ArtId = reader.GetInt64(2);
            MessageId = reader.GetInt32(3);
            FileId = reader.GetString(4);
            ArtName = reader.GetString(5);
        }
    }
}