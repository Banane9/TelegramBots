using System;
using System.Linq;
using System.Collections.Generic;

using System.Linq;

using Banane9.TelegramBots.ArtChannelBot.Data;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace Banane9.TelegramBots.ArtChannelBot
{
    internal class Program
    {
        private static readonly Dictionary<Data.User, Message> lastUserMessage = new Dictionary<Data.User, Message>();
        private static ArtDb database = new ArtDb();

        private static async void BotClient_OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
            Console.WriteLine("Query: " + e.InlineQuery.Query);
            var client = (TelegramBotClient)sender;

            var query = e.InlineQuery.Query.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim());
            var x = 0;

            var result = database.SearchArt(query).Select(async artResult => new InlineQueryResultCachedPhoto
            {
                Id = (++x).ToString(),
                FileId = artResult.FileId,
                Title = artResult.ArtName,
                Description = $"{artResult.ArtName} from {artResult.ChannelName}",
                Caption = $"{artResult.ArtName}\r\n{await client.ExportChatInviteLinkAsync(artResult.ChannelId)}"
            }).Select(t => t.Result).ToArray();

            await client.AnswerInlineQueryAsync(e.InlineQuery.Id, result, isPersonal: true, cacheTime: 60);
        }

        private static async void BotClient_OnUpdate(object sender, UpdateEventArgs e)
        {
            Console.WriteLine("Update Type: " + e.Update.Type);

            var client = (TelegramBotClient)sender;
            switch (e.Update.Type)
            {
                case UpdateType.ChannelPost:
                    var post = e.Update.ChannelPost;

                    if (post.Type != MessageType.PhotoMessage)
                        break;

                    var channel = database.GetChannel(post.Chat);
                    database.AddArt(channel, post, post);
                    break;

                case UpdateType.EditedChannelPost:
                    var ePost = e.Update.EditedChannelPost;

                    if (ePost.Type != MessageType.PhotoMessage)
                        break;

                    var uChannel = database.GetChannel(ePost.Chat);
                    database.UpdateArt(ePost);
                    break;

                case UpdateType.MessageUpdate:
                    var message = e.Update.Message;
                    var user = database.GetUser(message.From.Id, message.Chat.Id);
                    var forwardFrom = message.ForwardFromChat;

                    if (forwardFrom != null && forwardFrom.Type == ChatType.Channel && message.Type == MessageType.PhotoMessage)
                    {
                        var admins = await client.GetChatAdministratorsAsync(forwardFrom.Id);
                        if (!admins.Any(admin => admin.User.Id == user.UserId))
                        {
                            await client.SendTextMessageAsync(user.UserId, $"You have to be an Admin of **{forwardFrom.Title}** to add art from it!", ParseMode.Markdown);
                            break;
                        }

                        lastUserMessage[user] = message;

                        if (!string.IsNullOrWhiteSpace(message.Caption))
                        {
                            database.AddArt(database.GetChannel(forwardFrom), message);

                            await client.SendTextMessageAsync(user.UserId, "Added the Artwork using the caption for details. The next text message can be used to update it.");
                            break;
                        }

                        await client.SendTextMessageAsync(user.UserId, "Waiting for details in the next text message.");
                        break;
                    }

                    if (message.Type != MessageType.TextMessage || !lastUserMessage.ContainsKey(user) || lastUserMessage[user] == null || string.IsNullOrWhiteSpace(message.Text))
                        break;

                    var lChannel = database.GetChannel(lastUserMessage[user].ForwardFromChat);

                    if (!string.IsNullOrWhiteSpace(lastUserMessage[user].Caption))
                        database.UpdateArt(message, lastUserMessage[user]);
                    else
                        database.AddArt(lChannel, lastUserMessage[user], message);

                    lastUserMessage[user] = null;
                    break;

                case UpdateType.EditedMessage:
                    if (e.Update.EditedMessage.Type != MessageType.TextMessage)
                        break;

                    database.UpdateArt(e.Update.EditedMessage);
                    break;
            }
        }

        private static void Main(string[] args)
        {
            var botClient = new TelegramBotClient("471628940:AAES_fQeiL9UTmzlkJkAWbdgMKuzLyxiEYo");
            botClient.OnInlineQuery += BotClient_OnInlineQuery;
            botClient.OnUpdate += BotClient_OnUpdate;

            Console.Title = botClient.GetMeAsync().Result.FirstName;

            botClient.StartReceiving();
            Console.ReadLine();
            botClient.StopReceiving();
        }
    }
}