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
        private readonly SQLiteCommand addChannelCommand;
        private readonly SQLiteCommand addCharacterArtCommand;
        private readonly SQLiteCommand addCharacterCommand;
        private readonly SQLiteCommand addTagCommand;
        private readonly SQLiteCommand addUserCommand;
        private readonly SQLiteCommand clearArtSearchResultsCommand;
        private readonly SQLiteConnection connection;
        private readonly SQLiteCommand getArtIdByMessageCommand;
        private readonly SQLiteCommand getArtistCommand;
        private readonly SQLiteCommand getArtSearchResultsCommand;
        private readonly SQLiteCommand getChannelCommand;
        private readonly SQLiteCommand getCharacterCommand;
        private readonly SQLiteCommand getTagCommand;
        private readonly SQLiteCommand getUserCommand;
        private readonly SQLiteCommand prepareArtSearchCommand;
        private readonly SQLiteCommand removeArtCommand;
        private readonly SQLiteCommand searchCommand;
        private readonly SQLiteCommand updateArtCommand;

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

            addChannelCommand = connection.CreateCommand();
            addChannelCommand.CommandText = "INSERT OR IGNORE INTO Channels (Name, ChannelId) VALUES (@name, @channelId)";

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
            addArtCommand.CommandText = "INSERT OR IGNORE INTO Art (ChannelId, FileId, DetailMessageChatId, DetailMessageId, Name, Rating) VALUES (@channelId, @fileId, @detailMessageChatId, @detailMessageId, @name, @rating)";

            getArtIdByMessageCommand = connection.CreateCommand();
            getArtIdByMessageCommand.CommandText = "SELECT Id FROM Art WHERE (Art.DetailMessageChatId = @detailMessageChatId) AND (Art.DetailMessageId = @detailMessageId)";

            updateArtCommand = connection.CreateCommand();
            updateArtCommand.CommandText = "UPDATE Art SET DetailMessageChatId = @newDetailMessageChatId, DetailMessageId = @newDetailMessageId, Name = @newName, Rating = @newRating WHERE (Art.Id = @artId)";

            removeArtCommand = connection.CreateCommand();
            removeArtCommand.CommandText = "DELETE FROM Art WHERE (Art.ChannelId = @channelId) AND (Art.MessageId = @messageId)";

            getUserCommand = connection.CreateCommand();
            getUserCommand.CommandText = "SELECT * FROM Users WHERE (Users.UserId = @userId)";

            addUserCommand = connection.CreateCommand();
            addUserCommand.CommandText = "INSERT OR IGNORE INTO Users (UserId, ChatId) VALUES (@userId, @chatId)";
        }

        public bool AddArt(Channel channel, Message fileMessage, Message detailMessage = null)
        {
            var details = new ArtworkDetails(detailMessage?.Text ?? fileMessage.Caption);

            lock (connection)
            {
                addArtCommand.Reset();
                addArtCommand.Parameters.AddWithValue("@channelId", channel.InternalId);
                addArtCommand.Parameters.AddWithValue("@fileId", fileMessage.Photo[0].FileId);
                addArtCommand.Parameters.AddWithValue("@detailMessageChatId", detailMessage?.Chat?.Id ?? fileMessage.ForwardFromChat?.Id ?? fileMessage.Chat.Id);
                addArtCommand.Parameters.AddWithValue("@detailMessageId", detailMessage?.MessageId ?? fileMessage?.ForwardFromMessageId ?? fileMessage.MessageId);
                addArtCommand.Parameters.AddWithValue("@name", details.Name);
                addArtCommand.Parameters.AddWithValue("@rating", details.Rating);

                var transaction = connection.BeginTransaction();
                if (addArtCommand.ExecuteNonQuery() == 0)
                {
                    transaction.Commit();
                    return false;
                }

                var artId = connection.LastInsertRowId;

                addDetailRelations(artId, details);

                transaction.Commit();
            }

            return true;
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

                addChannelCommand.Reset();
                addChannelCommand.Parameters.AddWithValue("@name", chat.Title);
                addChannelCommand.Parameters.AddWithValue("@channelId", chat.Id);
                addChannelCommand.ExecuteNonQuery();

                return new Channel(connection.LastInsertRowId, chat.Title, chat.Id);
            }
        }

        public User GetUser(int userId, long chatId)
        {
            lock (connection)
            {
                getUserCommand.Reset();
                getUserCommand.Parameters.AddWithValue("@userId", userId);

                var reader = getUserCommand.ExecuteReader();
                if (reader.Read())
                    return new User(reader);

                addUserCommand.Reset();
                addUserCommand.Parameters.AddWithValue("@userId", userId);
                addUserCommand.ExecuteNonQuery();

                return new User(connection.LastInsertRowId, userId);
            }
        }

        public IEnumerable<ArtSearchResult> SearchArt(IEnumerable<string> terms)
        {
            SQLiteDataReader reader;

            lock (clearArtSearchResultsCommand)
            {
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
        }

        public void UpdateArt(Message detailMessage, Message fileMessage = null)
        {
            var details = new ArtworkDetails(detailMessage.Caption ?? detailMessage.Text);

            lock (connection)
            {
                getArtIdByMessageCommand.Reset();
                getArtIdByMessageCommand.Parameters.AddWithValue("@detailMessageChatId", fileMessage?.ForwardFromChat?.Id ?? fileMessage?.Chat?.Id ?? detailMessage.Chat.Id);
                getArtIdByMessageCommand.Parameters.AddWithValue("@detailMessageId", fileMessage?.ForwardFromMessageId ?? fileMessage?.MessageId ?? detailMessage.MessageId);

                var artIdObj = getArtIdByMessageCommand.ExecuteScalar();
                if (artIdObj == null)
                    return;

                var artId = (long)artIdObj;

                updateArtCommand.Reset();
                updateArtCommand.Parameters.AddWithValue("@newDetailMessageChatId", detailMessage.Chat.Id);
                updateArtCommand.Parameters.AddWithValue("@newDetailMessageId", detailMessage.MessageId);
                updateArtCommand.Parameters.AddWithValue("@newName", details.Name);
                updateArtCommand.Parameters.AddWithValue("@newRating", details.Rating);
                updateArtCommand.Parameters.AddWithValue("@artId", artId);

                var transaction = connection.BeginTransaction();

                updateArtCommand.ExecuteNonQuery();

                var removeTags = buildDeleteCommandForArtJunctionEntries("ArtTags", "Tags", "TagId", details.Tags.Length);
                removeTags.Parameters.AddWithValue("@artId", artId);
                removeTags.Parameters.AddRange(details.Tags.Select(t => new SQLiteParameter(null, t)).ToArray());
                removeTags.ExecuteNonQuery();

                var removeChars = buildDeleteCommandForArtJunctionEntries("CharacterArt", "Characters", "CharacterId", details.Characters.Length);
                removeChars.Parameters.AddWithValue("@artId", artId);
                removeChars.Parameters.AddRange(details.Characters.Select(c => new SQLiteParameter(null, c)).ToArray());
                removeChars.ExecuteNonQuery();

                var removeArtists = buildDeleteCommandForArtJunctionEntries("ArtistPieces", "Artists", "ArtistId", details.Artists.Length);
                removeArtists.Parameters.AddWithValue("@artId", artId);
                removeArtists.Parameters.AddRange(details.Artists.Select(a => new SQLiteParameter(null, a)).ToArray());
                removeArtists.ExecuteNonQuery();

                addDetailRelations(artId, details);

                transaction.Commit();
            }
        }

        private void addDetailRelation(SQLiteCommand command, long artId, long otherId)
        {
            command.Reset();
            command.Parameters.AddWithValue("@artId", artId);
            command.Parameters.AddWithValue("@otherId", otherId);
            command.ExecuteNonQuery();
        }

        private void addDetailRelations(long artId, ArtworkDetails details)
        {
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
        }

        private SQLiteCommand buildDeleteCommandForArtJunctionEntries(string junction, string other, string otherId, int toKeep)
        {
            SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"DELETE FROM {junction} WHERE ({junction}.ArtId = @artId) AND ((SELECT Name FROM {other} WHERE ({other}.Id = {junction}.{otherId})) NOT IN ({string.Join(", ", Enumerable.Repeat('?', toKeep))}))";

            return cmd;
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