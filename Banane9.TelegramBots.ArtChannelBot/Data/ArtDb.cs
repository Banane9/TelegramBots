using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        private readonly SQLiteCommand clearArtSearchResultsCommand;
        private readonly SQLiteConnection connection;
        private readonly SQLiteCommand getArtistCommand;
        private readonly SQLiteCommand getArtSearchResultsCommand;
        private readonly SQLiteCommand getChannelCommand;
        private readonly SQLiteCommand getCharacterCommand;
        private readonly SQLiteCommand getTagCommand;
        private readonly SQLiteCommand getUserCommand;
        private readonly SQLiteCommand insertChannelCommand;
        private readonly SQLiteCommand prepareArtSearchCommand;
        private readonly SQLiteCommand removeArtCommand;
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

            prepareArtSearchCommand = connection.CreateCommand();
            prepareArtSearchCommand.CommandText = "CREATE TEMPORARY TABLE IF NOT EXISTS ArtSearchResults (ChannelId INTEGER NOT NULL, ChannelName CHAR NOT NULL, ArtId INTEGER NOT NULL, MessageId INTEGER NOT NULL, FileId CHAR NOT NULL, ArtName CHAR NOT NULL);";

            searchCommand = connection.CreateCommand();
            using (var asmStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Banane9.TelegramBots.ArtChannelBot.Data.Search.sql"))
            using (var reader = new StreamReader(asmStream))
                searchCommand.CommandText = reader.ReadToEnd();

            getArtSearchResultsCommand = connection.CreateCommand();
            getArtSearchResultsCommand.CommandText = "SELECT * FROM ArtSearchResults GROUP BY ArtSearchResults.ArtId ORDER BY COUNT(ArtSearchResults.ArtId) DESC";

            clearArtSearchResultsCommand = connection.CreateCommand();
            clearArtSearchResultsCommand.CommandText = "DROP TABLE IF EXISTS ArtSearchResults";

            getChannelCommand = connection.CreateCommand();
            getChannelCommand.CommandText = "SELECT * FROM Channels WHERE (Channels.ChannelId = @channelId)";

            insertChannelCommand = connection.CreateCommand();
            insertChannelCommand.CommandText = "INSERT OR IGNORE INTO Channels (Name, ChannelId) VALUES (@name, @channelId)";

            getTagCommand = connection.CreateCommand();
            getTagCommand.CommandText = "SELECT * FROM Tags WHERE (Tags.Name = @name)";

            addTagCommand = connection.CreateCommand();
            addTagCommand.CommandText = "INSERT OR IGNORE INTO Tags (Name) VALUES (@name)";

            addArtTagCommand = connection.CreateCommand();
            addArtTagCommand.CommandText = "INSERT OR IGNORE INTO ArtTags (ArtId, TagId) VALUES (@artId, @otherId)";

            getArtistCommand = connection.CreateCommand();
            getArtistCommand.CommandText = "SELECT * FROM Artists WHERE (Artists.Name = @name)";

            addArtistCommand = connection.CreateCommand();
            addArtistCommand.CommandText = "INSERT OR IGNORE INTO Artists (Name) VALUES (@name)";

            addArtistPieceCommand = connection.CreateCommand();
            addArtistPieceCommand.CommandText = "INSERT OR IGNORE INTO ArtistPieces (ArtId, ArtistId) VALUES (@artId, @otherId)";

            getCharacterCommand = connection.CreateCommand();
            getCharacterCommand.CommandText = "SELECT * FROM Characters WHERE (Characters.Name = @name)";

            addCharacterCommand = connection.CreateCommand();
            addCharacterCommand.CommandText = "INSERT OR IGNORE INTO Characters (Name) VALUES (@name)";

            addCharacterArtCommand = connection.CreateCommand();
            addCharacterArtCommand.CommandText = "INSERT OR IGNORE INTO CharacterArt (ArtId, CharacterId) VALUES (@artId, @otherId)";

            addArtCommand = connection.CreateCommand();
            addArtCommand.CommandText = "INSERT OR IGNORE INTO Art (ChannelId, MessageId, FileId, Name, Rating) VALUES (@channelId, @messageId, @fileId, @name, @rating)";

            removeArtCommand = connection.CreateCommand();
            removeArtCommand.CommandText = "DELETE FROM Art WHERE (Art.ChannelId = @channelId) AND (Art.MessageId = @messageId)";
        }

        public void AddArt(Channel channel, Message channelPost, SQLiteTransaction transaction = null)
        {
            var details = new ArtworkDetails(channelPost.Caption);

            addArtCommand.Reset();
            addArtCommand.Parameters.AddWithValue("@channelId", channel.InternalId);
            addArtCommand.Parameters.AddWithValue("@messageId", channelPost.MessageId);
            addArtCommand.Parameters.AddWithValue("@fileId", channelPost.Photo[0].FileId);
            addArtCommand.Parameters.AddWithValue("@name", details.Name);
            addArtCommand.Parameters.AddWithValue("@rating", details.Rating);

            if (transaction == null)
                Monitor.Enter(connection);

            try
            {
                transaction = transaction ?? connection.BeginTransaction();
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
            finally
            {
                Monitor.Exit(connection);
            }
        }

        public Channel GetChannel(Chat chat)
        {
            lock (connection)
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
            }

            return new Channel(connection.LastInsertRowId, chat.Title, chat.Id);
        }

        public IEnumerable<ArtSearchResult> SearchArt(IEnumerable<string> terms)
        {
            SQLiteDataReader reader;

            lock (connection)
            {
                clearArtSearchResultsCommand.ExecuteNonQuery();
                prepareArtSearchCommand.ExecuteNonQuery();

                foreach (var term in terms)
                {
                    searchCommand.Reset();
                    searchCommand.Parameters.AddWithValue("@term", term + "%");
                    searchCommand.ExecuteNonQuery();
                }

                getArtSearchResultsCommand.Reset();
                reader = getArtSearchResultsCommand.ExecuteReader();
            }

            while (reader.Read())
            {
                yield return new ArtSearchResult(reader);
            }
        }

        public void UpdateArt(Channel channel, Message channelPost)
        {
            removeArtCommand.Reset();
            removeArtCommand.Parameters.AddWithValue("@channelId", channel.InternalId);
            removeArtCommand.Parameters.AddWithValue("@messageId", channelPost.MessageId);

            Monitor.Enter(connection);

            var transaction = connection.BeginTransaction();
            removeArtCommand.ExecuteNonQuery();

            AddArt(channel, channelPost, transaction);
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