using System;
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
    public sealed class ArtChannelBot
    {
        private static readonly string formatText = "The Details for an Image must have the following format: `Name: My Image; Rating: SFW; Artist: my artist; Characters: A, B; Tags: cute, test`. The order doesn't matter. The Artist, Character and Tag fields support plural names and a comma separated list of values.";
        private static readonly string helpText = "This bot can track posts in (Art) Channels and allows them to be searched with inline queries (i.e. `@ArtChanBot my, search, terms`). It will only search in channels that you're subscribed to. When an image is selected, it will be posted with the name and a link to the channel it came from. To track posts, the bot needs to be an admin of the channel. If you can't edit the details into the posts anymore, you can forward them to the bot and then add the details in the message after it. Supported fields are `Name` and `Rating` (with only one value), as well as `Artist(s)`, `Character(s)` and `Tag(s)`. Use /format to see how to use them. Edits to the original post or the detail message will be tracked.";
        private static readonly string subscribeText = "Simply forward a message from the channel you want to subscribe to!";
        private static readonly string unsubscribeText = "Simply forward a message from the channel you want to unsubscribe from!";
        private readonly TelegramBotClient client;
        private readonly ArtDb database = new ArtDb("../../ArtDB.db3");
        private readonly Dictionary<Data.User, Message> lastUserMessage = new Dictionary<Data.User, Message>();
        private readonly HashSet<int> usersWaitingForChannelSub = new HashSet<int>();
        private readonly HashSet<int> usersWaitingForChannelUnsub = new HashSet<int>();
        public Telegram.Bot.Types.User Self { get; private set; }

        public ArtChannelBot(string botToken)
        {
            client = new TelegramBotClient(botToken);
            client.OnInlineQuery += client_OnInlineQuery;
            client.OnUpdate += client_OnUpdate;
            client.OnMessage += client_OnMessage;
            client.OnMessageEdited += client_OnMessageEdited;

            Self = client.GetMeAsync().Result;
        }

        public void Start()
        {
            client.StartReceiving();
        }

        public void Stop()
        {
            client.StopReceiving();
        }

        private async void client_OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
            Console.WriteLine("Query: " + e.InlineQuery.Query);

            var user = database.GetUser(e.InlineQuery.From.Id, e.InlineQuery.From.Id);
            var query = e.InlineQuery.Query.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim());
            var x = 0;

            var result = database.SearchArt(user, query).Select(async artResult => new InlineQueryResultCachedPhoto
            {
                Id = (++x).ToString(),
                FileId = artResult.FileId,
                Title = artResult.ArtName,
                Description = $"{artResult.ArtName} from {artResult.ChannelName}",
                Caption = $"{artResult.ArtName}\r\n{artResult.ChannelJoinLink}"
            }).Select(t => t.Result).ToArray();

            await client.AnswerInlineQueryAsync(e.InlineQuery.Id, result, isPersonal: true, cacheTime: 60);
        }

        private async void client_OnMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine(e.Message.Text);
            var user = database.GetUser(e.Message.From.Id, e.Message.Chat.Id);
            var forwardFrom = e.Message.ForwardFromChat;

            if (forwardFrom == null && e.Message.Type == MessageType.TextMessage && e.Message.Text.StartsWith("/"))
            {
                handleCommand(e.Message);
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

            if (forwardFrom != null && forwardFrom.Type == ChatType.Channel && e.Message.Type == MessageType.PhotoMessage)
            {
                var admins = await client.GetChatAdministratorsAsync(forwardFrom.Id);
                if (!admins.Any(admin => admin.User.Id == user.UserId))
                {
                    await client.SendTextMessageAsync(user.UserId, $"You have to be an Admin of **{forwardFrom.Title}** to add art from it!", ParseMode.Markdown);
                    return;
                }

                lastUserMessage[user] = e.Message;

                if (!string.IsNullOrWhiteSpace(e.Message.Caption))
                {
                    database.AddArt(database.GetChannel(forwardFrom, getJoinLink), e.Message);

                    await client.SendTextMessageAsync(user.UserId, "Added the Artwork using the caption for details. The next text message can be used to update it.");
                    return;
                }

                await client.SendTextMessageAsync(user.UserId, "Waiting for details in the next text message.");
                return;
            }

            if (e.Message.Type != MessageType.TextMessage || !lastUserMessage.ContainsKey(user) || lastUserMessage[user] == null || string.IsNullOrWhiteSpace(e.Message.Text))
                return;

            var lChannel = database.GetChannel(lastUserMessage[user].ForwardFromChat, getJoinLink);

            if (!string.IsNullOrWhiteSpace(lastUserMessage[user].Caption))
                database.UpdateArt(e.Message, lastUserMessage[user]);
            else
                database.AddArt(lChannel, lastUserMessage[user], e.Message);

            await client.SendTextMessageAsync(user.UserId, "The details have been added!");

            lastUserMessage[user] = null;
        }

        private void client_OnMessageEdited(object sender, MessageEventArgs e)
        {
            if (e.Message.Type != MessageType.TextMessage)
                return;

            database.UpdateArt(e.Message);
        }

        private void client_OnUpdate(object sender, UpdateEventArgs e)
        {
            Console.WriteLine("Update Type: " + e.Update.Type);

            var client = (TelegramBotClient)sender;
            switch (e.Update.Type)
            {
                case UpdateType.ChannelPost:
                    var post = e.Update.ChannelPost;

                    if (post.Type != MessageType.PhotoMessage)
                        break;

                    var channel = database.GetChannel(post.Chat, getJoinLink);
                    database.AddArt(channel, post, post);
                    break;

                case UpdateType.EditedChannelPost:
                    var ePost = e.Update.EditedChannelPost;

                    if (ePost.Type != MessageType.PhotoMessage)
                        break;

                    var uChannel = database.GetChannel(ePost.Chat, getJoinLink);
                    database.UpdateArt(ePost);
                    break;
            }
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