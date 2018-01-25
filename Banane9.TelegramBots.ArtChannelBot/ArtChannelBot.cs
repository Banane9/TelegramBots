using System;
using System.Collections.Generic;
using System.Linq;
using Banane9.TelegramBots.ArtChannelBot.Data;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using TelegramBotLib;

namespace Banane9.TelegramBots.ArtChannelBot
{
    public sealed class ArtChannelBot : TelegramBot
    {
        private static readonly string formatText = "The Details for an Image must have the following format: `Name: My Image; Rating: SFW; Artist: my artist; Characters: A, B; Tags: cute, test`. The order doesn't matter. The Artist, Character and Tag fields support plural names and a comma separated list of values.";
        private static readonly string helpText = "This bot can track posts in (Art) Channels and allows them to be searched with inline queries (i.e. `@ArtChanBot my, search, terms`). It will only search in channels that you're subscribed to. When an image is selected, it will be posted with the name and a link to the channel it came from. To track posts, the bot needs to be an admin of the channel. If you can't edit the details into the posts anymore, you can forward them to the bot and then add the details in the message after it. Supported fields are `Name` and `Rating` (with only one value), as well as `Artist(s)`, `Character(s)` and `Tag(s)`. Use /format to see how to use them. Edits to the original post or the detail message will be tracked.";
        private static readonly string subscribeText = "Simply forward a message from the channel you want to subscribe to!";
        private static readonly string unsubscribeText = "Simply forward a message from the channel you want to unsubscribe from!";
        private readonly ArtDb database = new ArtDb("../../ArtDB.db3");
        private readonly Dictionary<Data.User, Message> lastUserMessage = new Dictionary<Data.User, Message>();
        private readonly HashSet<int> usersWaitingForChannelSub = new HashSet<int>();
        private readonly HashSet<int> usersWaitingForChannelUnsub = new HashSet<int>();

        public ArtChannelBot(string botToken)
            : base(botToken)
        { }

        public void Start()
        {
            client.StartReceiving();
        }

        public void Stop()
        {
            client.StopReceiving();
        }

        protected override void OnChannelPost(Message channelPost)
        {
            if (channelPost.Type != MessageType.Photo)
                return;

            var channel = database.GetChannel(channelPost.Chat, getJoinLink);
            database.AddArt(channel, channelPost, channelPost);
        }

        protected override void OnChannelPostEdited(Message editedChannelPost)
        {
            if (editedChannelPost.Type != MessageType.Photo)
                return;

            var uChannel = database.GetChannel(editedChannelPost.Chat, getJoinLink);
            database.UpdateArt(editedChannelPost);
        }

        protected async override void OnInlineQuery(InlineQuery inlineQuery)
        {
            Console.WriteLine("Query: " + inlineQuery.Query);

            var user = database.GetUser(inlineQuery.From.Id, inlineQuery.From.Id);
            var query = inlineQuery.Query.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim());
            var x = 0;

            var result = database.SearchArt(user, query).Select(artResult => new InlineQueryResultCachedPhoto((++x).ToString(), artResult.FileId)
            {
                Title = artResult.ArtName,
                Description = $"{artResult.ArtName} from {artResult.ChannelName}",
                Caption = $"{artResult.ArtName}\r\n{artResult.ChannelJoinLink}"
            }).ToArray();

            await client.AnswerInlineQueryAsync(inlineQuery.Id, result, isPersonal: true, cacheTime: 60);
        }

        protected async override void OnMessage(Message message)
        {
            Console.WriteLine(message.Text);
            var user = database.GetUser(message.From.Id, message.Chat.Id);
            var forwardFrom = message.ForwardFromChat;

            if (forwardFrom == null && message.Type == MessageType.Text && message.Text.StartsWith("/"))
            {
                handleCommand(message);
                return;
            }

            if (forwardFrom != null && forwardFrom.Type == ChatType.Channel && usersWaitingForChannelSub.Contains(user.UserId))
            {
                var channel = database.GetChannel(forwardFrom, getJoinLink);
                database.AddSubscription(user, channel);
                usersWaitingForChannelSub.Remove(user.UserId);
                await client.SendTextMessageAsync(user.UserId, $"You have subscribed to **{channel.Name}**. It may take a moment till new results appear for recent queries.", ParseMode.Markdown);
                return;
            }

            if (forwardFrom != null && forwardFrom.Type == ChatType.Channel && usersWaitingForChannelUnsub.Contains(user.UserId))
            {
                var channel = database.GetChannel(forwardFrom, getJoinLink);
                database.RemoveSubscription(user, database.GetChannel(forwardFrom, getJoinLink));
                usersWaitingForChannelUnsub.Remove(user.UserId);
                await client.SendTextMessageAsync(user.UserId, $"You have been unsubscribed from **{channel.Name}**. It may take a moment till the results disappear for recent queries.", ParseMode.Markdown);
                return;
            }

            if (forwardFrom != null && forwardFrom.Type == ChatType.Channel && message.Type == MessageType.Photo)
            {
                var admins = await client.GetChatAdministratorsAsync(forwardFrom.Id);
                if (!admins.Any(admin => admin.User.Id == user.UserId))
                {
                    await client.SendTextMessageAsync(user.UserId, $"You have to be an Admin of **{forwardFrom.Title}** to add art from it!", ParseMode.Markdown);
                    return;
                }

                lastUserMessage[user] = message;

                if (!string.IsNullOrWhiteSpace(message.Caption))
                {
                    database.AddArt(database.GetChannel(forwardFrom, getJoinLink), message);

                    await client.SendTextMessageAsync(user.UserId, "Added the Artwork using the caption for details. The next text message can be used to update it.");
                    return;
                }

                await client.SendTextMessageAsync(user.UserId, "Waiting for details in the next text message.");
                return;
            }

            if (message.Type != MessageType.Text || !lastUserMessage.ContainsKey(user) || lastUserMessage[user] == null || string.IsNullOrWhiteSpace(message.Text))
                return;

            var lChannel = database.GetChannel(lastUserMessage[user].ForwardFromChat, getJoinLink);

            if (!string.IsNullOrWhiteSpace(lastUserMessage[user].Caption))
                database.UpdateArt(message, lastUserMessage[user]);
            else
                database.AddArt(lChannel, lastUserMessage[user], message);

            await client.SendTextMessageAsync(user.UserId, "The details have been added!");

            lastUserMessage[user] = null;
        }

        protected override void OnMessageEdited(Message editedMessage)
        {
            if (editedMessage.Type != MessageType.Text)
                return;

            database.UpdateArt(editedMessage);
        }

        private string getJoinLink(Chat channel)
        {
            return client.ExportChatInviteLinkAsync(channel.Id).Result;
        }

        private void handleCommand(Message message)
        {
            switch (message.Text.ToLowerInvariant())
            {
                case "/help":
                    client.SendTextMessageAsync(message.From.Id, helpText, ParseMode.Markdown);
                    break;

                case "/format":
                    client.SendTextMessageAsync(message.From.Id, formatText, ParseMode.Markdown);
                    break;

                case "/subscribe":
                    client.SendTextMessageAsync(message.From.Id, subscribeText);
                    usersWaitingForChannelSub.Add(message.From.Id);
                    break;

                case "/unsubscribe":
                    client.SendTextMessageAsync(message.From.Id, unsubscribeText);
                    usersWaitingForChannelUnsub.Add(message.From.Id);
                    break;
            }
        }
    }
}