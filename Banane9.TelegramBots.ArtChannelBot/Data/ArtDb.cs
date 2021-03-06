﻿using System;
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
        private readonly SQLiteCommand addSubscriptionCommand;
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
        private readonly SQLiteCommand removeSubscriptionCommand;
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
            prepareArtSearchCommand.CommandText = "CREATE TEMPORARY TABLE IF NOT EXISTS ArtSearchResults (ChannelId INTEGER NOT NULL, ChannelName CHAR NOT NULL, ChannelJoinLink CHAR NOT NULL, ArtId INTEGER NOT NULL, FileId CHAR NOT NULL, ArtName CHAR NOT NULL);";

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
            addChannelCommand.CommandText = "INSERT OR IGNORE INTO Channels (Name, ChannelId, JoinLink) VALUES (@name, @channelId, @joinLink)";

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
            addUserCommand.CommandText = "INSERT OR IGNORE INTO Users (UserId) VALUES (@userId)";

            addSubscriptionCommand = connection.CreateCommand();
            addSubscriptionCommand.CommandText = "INSERT OR IGNORE INTO UserSubscribedChannels (UserId, ChannelId) VALUES (@userId, @channelId)";

            removeSubscriptionCommand = connection.CreateCommand();
            removeSubscriptionCommand.CommandText = "DELETE FROM UserSubscribedChannels WHERE (UserSubscribedChannels.UserId = @userId) AND (UserSubscribedChannels.ChannelId = @channelId)";
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

        public bool AddSubscription(User user, Channel channel)
        {
            lock (connection)
            {
                addSubscriptionCommand.Reset();
                addSubscriptionCommand.Parameters.AddWithValue("@userId", user.InternalId);
                addSubscriptionCommand.Parameters.AddWithValue("@channelId", channel.InternalId);

                return addSubscriptionCommand.ExecuteNonQuery() > 0;
            }
        }

        public Channel GetChannel(Chat chat, Func<Chat, string> getJoinLink)
        {
            lock (connection)
            {
                getChannelCommand.Reset();
                getChannelCommand.Parameters.AddWithValue("@channelId", chat.Id);

                var reader = getChannelCommand.ExecuteReader();
                if (reader.Read())
                {
                    var channel = new Channel(reader);
                    reader.Close();
                    return channel;
                }

                var joinLink = getJoinLink(chat);

                addChannelCommand.Reset();
                addChannelCommand.Parameters.AddWithValue("@name", chat.Title);
                addChannelCommand.Parameters.AddWithValue("@channelId", chat.Id);
                addChannelCommand.Parameters.AddWithValue("@joinLink", joinLink);
                addChannelCommand.ExecuteNonQuery();

                return new Channel(connection.LastInsertRowId, chat.Title, chat.Id, joinLink);
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
                {
                    var user = new User(reader);
                    reader.Close();
                    return user;
                }

                addUserCommand.Reset();
                addUserCommand.Parameters.AddWithValue("@userId", userId);
                addUserCommand.ExecuteNonQuery();

                return new User(connection.LastInsertRowId, userId);
            }
        }

        public void RemoveSubscription(User user, Channel channel)
        {
            lock (connection)
            {
                removeSubscriptionCommand.Reset();
                removeSubscriptionCommand.Parameters.AddWithValue("@userId", user.InternalId);
                removeSubscriptionCommand.Parameters.AddWithValue("@channelId", channel.InternalId);

                removeSubscriptionCommand.ExecuteNonQuery();
            }
        }

        public ArtSearchResult[] SearchArt(User user, IEnumerable<string> terms)
        {
            lock (clearArtSearchResultsCommand)
            {
                SQLiteDataReader reader;

                lock (connection)
                {
                    clearArtSearchResultsCommand.ExecuteNonQuery();
                    prepareArtSearchCommand.ExecuteNonQuery();

                    foreach (var term in terms)
                    {
                        searchCommand.Reset();
                        searchCommand.Parameters.AddWithValue("@userId", user.InternalId);
                        searchCommand.Parameters.AddWithValue("@term", term + "%");
                        searchCommand.ExecuteNonQuery();
                    }

                    getArtSearchResultsCommand.Reset();
                    reader = getArtSearchResultsCommand.ExecuteReader();
                }

                var results = new List<ArtSearchResult>();
                while (reader.Read())
                {
                    results.Add(new ArtSearchResult(reader));
                }

                reader.Close();

                return results.ToArray();
            }
        }

        public void UpdateArt(Message detailMessage, Func<Chat, string> getJoinLink, Message fileMessage = null)
        {
            var details = new ArtworkDetails(detailMessage.Caption ?? detailMessage.Text);

            lock (connection)
            {
                getArtIdByMessageCommand.Reset();
                getArtIdByMessageCommand.Parameters.AddWithValue("@detailMessageChatId", fileMessage?.ForwardFromChat?.Id ?? fileMessage?.Chat?.Id ?? detailMessage.Chat.Id);
                getArtIdByMessageCommand.Parameters.AddWithValue("@detailMessageId", fileMessage?.ForwardFromMessageId ?? fileMessage?.MessageId ?? detailMessage.MessageId);

                var artIdObj = getArtIdByMessageCommand.ExecuteScalar();
                if (artIdObj == null)
                {
                    AddArt(GetChannel(fileMessage?.Chat ?? detailMessage.Chat, getJoinLink), fileMessage ?? detailMessage, detailMessage);

                    return;
                }

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

            var id = getCommand.ExecuteScalar();

            if (id != null)
                return (long)id;

            addCommand.Reset();
            addCommand.Parameters.AddWithValue("@name", name);
            addCommand.ExecuteNonQuery();

            return connection.LastInsertRowId;
        }
    }
}