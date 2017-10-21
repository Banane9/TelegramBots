using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using Telegram.Bot.Types;

namespace Banane9.TelegramBots.ArtChannelBot.Data
{
    public sealed class ArtDb
    {
        private readonly SQLiteCommand addArtCommand;
        private readonly SQLiteCommand addArtistCommand;
        private readonly SQLiteCommand addArtistPieceCommand;
        private readonly SQLiteCommand addArtTagCommand;
        private readonly SQLiteCommand addCharacterArtCommand;
        private readonly SQLiteCommand addCharacterCommand;
        private readonly SQLiteCommand addTagCommand;
        private readonly SQLiteConnection connection;

        private readonly SQLiteCommand getArtistCommand;
        private readonly SQLiteCommand getChannelCommand;
        private readonly SQLiteCommand getCharacterCommand;
        private readonly SQLiteCommand getTagCommand;
        private readonly SQLiteCommand getUserCommand;
        private readonly SQLiteCommand insertChannelCommand;
        private readonly SQLiteCommand searchCommand;

        public ArtDb(string name = "ArtDB.db3")
        {
            connection = new SQLiteConnection($"Data Source={name};Version=3;");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            using (var asmStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Banane9.TelegramBots.ArtChannelBot.Data.ArtChanDB.sql"))
            using (var reader = new StreamReader(asmStream))
            {
                cmd.CommandText = reader.ReadToEnd();
                cmd.ExecuteNonQuery();
            }

            searchCommand = connection.CreateCommand();

            using (var asmStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Banane9.TelegramBots.ArtChannelBot.Data.Search.sql"))
            using (var reader = new StreamReader(asmStream))
                searchCommand.CommandText = reader.ReadToEnd();

            getChannelCommand = connection.CreateCommand();
            getChannelCommand.CommandText = "SELECT * FROM Channels WHERE (Channels.ChannelId = @channelId)";

            insertChannelCommand = connection.CreateCommand();
            insertChannelCommand.CommandText = "INSERT OR IGNORE INTO Channels (Name, ChannelId) VALUES (@name, @channelId)";

            getTagCommand = connection.CreateCommand();
            getTagCommand.CommandText = "SELECT * FROM Tags WHERE (Tags.Name = @name)";

            addTagCommand = connection.CreateCommand();
            addTagCommand.CommandText = "INSERT OR IGNORE INTO Tags (Name) VALUES (@name) --WHERE NOT EXISTS (SELECT 1 FROM Tags WHERE (Tags.Name = @name))";

            addArtTagCommand = connection.CreateCommand();
            addArtTagCommand.CommandText = "INSERT OR IGNORE INTO ArtTags (ArtId, TagId) VALUES (@artId, @otherId) --WHERE NOT EXISTS (SELECT 1 FROM ArtTags WHERE (ArtTags.ArtId = @artId) AND (ArtTags.TagId = @otherId))";

            getArtistCommand = connection.CreateCommand();
            getArtistCommand.CommandText = "SELECT * FROM Artists WHERE (Artists.Name = @name)";

            addArtistCommand = connection.CreateCommand();
            addArtistCommand.CommandText = "INSERT OR IGNORE INTO Artists (Name) VALUES (@name) --WHERE NOT EXISTS (SELECT 1 FROM Artists WHERE (Artists.Name = @name))";

            addArtistPieceCommand = connection.CreateCommand();
            addArtistPieceCommand.CommandText = "INSERT OR IGNORE INTO ArtistPieces (ArtId, ArtistId) VALUES (@artId, @otherId) --WHERE NOT EXISTS (SELECT 1 FROM ArtistPieces WHERE (ArtistPieces.ArtId = @artId) AND (ArtistPieces.ArtistId = @otherId))";

            getCharacterCommand = connection.CreateCommand();
            getCharacterCommand.CommandText = "SELECT * FROM Characters WHERE (Characters.Name = @name)";

            addCharacterCommand = connection.CreateCommand();
            addCharacterCommand.CommandText = "INSERT OR IGNORE INTO Characters (Name) VALUES (@name) --WHERE NOT EXISTS (SELECT 1 FROM Characters WHERE (Characters.Name = @name))";

            addCharacterArtCommand = connection.CreateCommand();
            addCharacterArtCommand.CommandText = "INSERT OR IGNORE INTO CharacterArt (ArtId, CharacterId) VALUES (@artId, @otherId) --WHERE NOT EXISTS (SELECT 1 FROM CharacterArt WHERE (CharacterArt.ArtId = @artId) AND (CharacterArt.CharacterId = @otherId))";

            addArtCommand = connection.CreateCommand();
            addArtCommand.CommandText = "INSERT OR IGNORE INTO Art (ChannelId, MessageId, FileId, Name, Rating) VALUES (@channelId, @messageId, @fileId, @name, @rating) --WHERE NOT EXISTS (SELECT 1 FROM Art WHERE (Art.ChannelId = @channelId) AND (Art.MessageId = @messageId))";
        }

        public void AddArt(Channel channel, Message channelPost)
        {
            var details = new ArtworkDetails(channelPost.Caption);

            addArtCommand.Reset();
            addArtCommand.Parameters.AddWithValue("@channelId", channel.InternalId);
            addArtCommand.Parameters.AddWithValue("@messageId", channelPost.MessageId);
            addArtCommand.Parameters.AddWithValue("@fileId", channelPost.Photo[0].FileId);
            addArtCommand.Parameters.AddWithValue("@name", details.Name);
            addArtCommand.Parameters.AddWithValue("@rating", details.Rating);

            var transaction = connection.BeginTransaction();
            if (addArtCommand.ExecuteNonQuery() == 0)
            {
                transaction.Commit();
                return;
            }

            var artId = connection.LastInsertRowId;

            foreach (var artist in details.Artists)
            {
                var artistId = getDetail(getArtistCommand, addArtistCommand, artist);
                addDetailRelation(addArtistPieceCommand, artId, artistId);
            }

            foreach (var character in details.Characters)
            {
                var charId = getDetail(getCharacterCommand, addCharacterCommand, character);
                addDetailRelation(addCharacterArtCommand, artId, charId);
            }

            foreach (var tag in details.Tags)
            {
                var tagId = getDetail(getTagCommand, addTagCommand, tag);
                addDetailRelation(addArtTagCommand, artId, tagId);
            }

            transaction.Commit();
        }

        public Channel GetChannel(Chat chat)
        {
            getChannelCommand.Reset();
            getChannelCommand.Parameters.AddWithValue("@channelId", chat.Id);
            var reader = getChannelCommand.ExecuteReader();

            if (reader.Read())
                return new Channel(reader);

            insertChannelCommand.Reset();
            insertChannelCommand.Parameters.AddWithValue("@name", chat.Title);
            insertChannelCommand.Parameters.AddWithValue("@channelId", chat.Id);
            insertChannelCommand.ExecuteNonQuery();

            return new Channel(connection.LastInsertRowId, chat.Title, chat.Id);
        }

        public IEnumerable<ArtSearchResult> SearchArt(IEnumerable<string> terms)
        {
            // Add sorted dictionary with more appearances = higher?
            foreach (var term in terms)
            {
                searchCommand.Reset();
                searchCommand.Parameters.AddWithValue("@term", term + "%");
                var reader = searchCommand.ExecuteReader();

                while (reader.Read())
                {
                    yield return new ArtSearchResult(reader);
                }
            }
        }

        internal void UpdateArt(Channel channel, Message channelPost)
        {
            throw new NotImplementedException();
        }

        private void addDetailRelation(SQLiteCommand command, long artId, long otherId)
        {
            command.Reset();
            command.Parameters.AddWithValue("@artId", artId);
            command.Parameters.AddWithValue("@otherId", otherId);
            command.ExecuteNonQuery();
        }

        private long getDetail(SQLiteCommand getCommand, SQLiteCommand addCommand, string name)
        {
            getCommand.Reset();
            getCommand.Parameters.AddWithValue("@name", name);
            var reader = getCommand.ExecuteReader();

            if (reader.Read())
                return reader.GetInt64(0);

            addCommand.Reset();
            addCommand.Parameters.AddWithValue("@name", name);
            addCommand.ExecuteNonQuery();

            return connection.LastInsertRowId;
        }
    }
}