using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private static readonly string unknownCommandText = "Unknown Command! Use /help to get a list of supported commands!";
        private static readonly string unsubscribeText = "Simply forward a message from the channel you want to unsubscribe from!";
        private readonly ArtDb database = new ArtDb("../../ArtDB.db3");
        private readonly Dictionary<Data.User, Message> lastUserMessage = new Dictionary<Data.User, Message>();
        private readonly StateMachine protoStateMachine = new StateMachine(BasicState.WaitingForCommands);
        private readonly Dictionary<int, StateMachine> userStates = new Dictionary<int, StateMachine>();
        private readonly HashSet<int> usersWaitingForChannelSub = new HashSet<int>();
        private readonly HashSet<int> usersWaitingForChannelUnsub = new HashSet<int>();

        public ArtChannelBot(string botToken)
            : base(botToken)
        {
            protoStateMachine.AddTransition<BasicState, Message, BasicState>(canDoStandardCommand, doStandardCommand);
            protoStateMachine.AddTransition<BasicState, Message, BasicState>(canDoSubCommand, doSubCommand);
            protoStateMachine.AddTransition<BasicState, Message, BasicState>(canDoUnsubCommand, doUnsubCommand);
            protoStateMachine.AddTransition<BasicState, Message, BasicState>(canDoUnknownCommand, doUnknownCommand);
            protoStateMachine.AddTransition<BasicState, Message, BasicState>(canDoSubUnsub, doSub);
            protoStateMachine.AddTransition<BasicState, Message, BasicState>(canDoSubUnsub, doUnsub);
            protoStateMachine.AddTransition<BasicState, Message, WaitingForArtDetails>(canAddArt, doAddArt);
            protoStateMachine.AddTransition<WaitingForArtDetails, Message, WaitingForArtDetails>(canDoInvalidDetail, doInvalidDetail);
            protoStateMachine.AddTransition<WaitingForArtDetails, Message, BasicState>(canSkip, doSkip);
            protoStateMachine.AddTransition<WaitingForArtDetails, Message, BasicState>(canCancel, doCancel);
            protoStateMachine.AddTransition<WaitingForArtDetails, Message, BasicState>(canAddDetails, doAddDetails);
            protoStateMachine.AddTransition<BasicState, Message, BasicState>(canDoUnsupported, doUnsupported);
        }

        public void Start()
        {
            client.StartReceiving();
        }

        public void Stop()
        {
            client.StopReceiving();
        }

        protected async override void InlineQueryTask(InlineQuery inlineQuery)
        {
            Console.WriteLine("Query: " + inlineQuery.Query);

            var user = database.GetUser(inlineQuery.From.Id, inlineQuery.From.Id);
            var query = inlineQuery.Query.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim());
            var x = 0;

            var result = database.SearchArt(user, query).ToArray()
                .Select(artResult => new InlineQueryResultCachedPhoto((++x).ToString(), artResult.FileId)
                {
                    Title = artResult.ArtName,
                    Description = $"{artResult.ArtName} from {artResult.ChannelName}",
                    Caption = $"{artResult.ArtName}\r\n{artResult.ChannelJoinLink}"
                });

            foreach (var resultChunk in result.Chunk(3))
                await client.AnswerInlineQueryAsync(inlineQuery.Id, resultChunk, cacheTime: 60, isPersonal: true);
        }

        protected override void OnChannelPost(Message channelPost)
        {
            if (channelPost.Type != MessageType.Photo)
                return;

            var channel = database.GetChannel(channelPost.Chat, getJoinLink);

            if (string.IsNullOrWhiteSpace(channelPost.Caption))
            {
                if (channelPost.From != null)
                    client.SendTextMessageAsync(channelPost.From.Id, $"**{channelPost.Chat.Title}** Images that are supposed to be tracked need a caption. Use /format to see how it needs to look.\r\nYou can edit it or forward it here to add it as well.", ParseMode.Markdown);

                return;
            }

            database.AddArt(channel, channelPost, channelPost);

            if (channelPost.From != null)
                client.SendTextMessageAsync(channelPost.From.Id, $"**{channelPost.Chat.Title}** The Image has been added using the caption for details.\r\nYou can edit it or forward it here to change it.", ParseMode.Markdown);
        }

        protected override void OnChannelPostEdited(Message editedChannelPost)
        {
            if (editedChannelPost.Type != MessageType.Photo)
                return;

            if (string.IsNullOrWhiteSpace(editedChannelPost.Caption))
            {
                if (editedChannelPost.From != null)
                    client.SendTextMessageAsync(editedChannelPost.From.Id, $"**{editedChannelPost.Chat.Title}** Images that are supposed to be tracked need a caption. Use /format to see how it needs to look.\r\nYou can edit it or forward it here to add it as well.\r\n\r\nIf it was added already, the change will be ignored.", ParseMode.Markdown);

                return;
            }

            database.UpdateArt(editedChannelPost, getJoinLink);

            if (editedChannelPost.From != null)
                client.SendTextMessageAsync(editedChannelPost.From.Id, $"**{editedChannelPost.Chat.Title}** Changed the details to reflect the new caption.", ParseMode.Markdown);
        }

        protected override void OnMessage(Message message)
        {
            Console.WriteLine(message.Text);

            if (!userStates.ContainsKey(message.From.Id))
                userStates.Add(message.From.Id, protoStateMachine.Copy());

            if (!userStates[message.From.Id].TryTransitioning(message))
            {
                client.SendTextMessageAsync(message.From.Id, "Something went wrong.");
                userStates[message.From.Id].ForceState(BasicState.WaitingForCommands);
            }
        }

        protected override void OnMessageEdited(Message editedMessage)
        {
            if (editedMessage.Type != MessageType.Text)
                return;

            if (string.IsNullOrWhiteSpace(editedMessage.Text))
            {
                client.SendTextMessageAsync(editedMessage.From.Id, "If this was a detail message, the change has been ignored because it's empty.", replyToMessageId: editedMessage.MessageId);
                return;
            }

            database.UpdateArt(editedMessage, getJoinLink);
            client.SendTextMessageAsync(editedMessage.From.Id, "If this was a detail message, the Image has been updated to reflect the new text.", replyToMessageId: editedMessage.MessageId);
        }

        private bool canAddArt(BasicState state, Message message)
        {
            if (message.ForwardFromChat?.Type != ChatType.Channel || message.Type != MessageType.Photo)
                return false;

            if (!isChatAdmin(message.ForwardFromChat.Id, message.From.Id).Result)
            {
                client.SendTextMessageAsync(message.From.Id, $"You have to be an Admin of **{message.ForwardFromChat.Title}** to add Images from it!", ParseMode.Markdown);
                return false;
            }

            return true;
        }

        private bool canAddDetails(WaitingForArtDetails _, Message message)
        {
            if (message.Type != MessageType.Text || string.IsNullOrWhiteSpace(message.Text))
            {//add invalid detail message transition
                var text = message.Text.ToLowerInvariant();
                return text == "/skip" || text == "/cancel";
            }

            client.SendTextMessageAsync(message.From.Id, "Please send a text message to add details, or write /skip or /cancel\r\n/skip will add the art as-is.\r\n/cancel will stop the process of adding it.");

            return false;
        }

        private bool canCancel(WaitingForArtDetails state, Message message)
        {
            return message.Text.ToLowerInvariant() == "/cancel";
        }

        private bool canDoInvalidDetail(WaitingForArtDetails state, Message message)
        {
            return message.Type != MessageType.Text || string.IsNullOrWhiteSpace(message.Text)
                 || (message.Text.ToLowerInvariant() == "/skip" && string.IsNullOrWhiteSpace(state.FileMessage.Caption))
                 || message.Text.ToLowerInvariant() != "/cancel";
        }

        private bool canDoStandardCommand(BasicState state, Message message)
        {
            if (message.Type != MessageType.Text)
                return false;

            var cmd = message.Text.ToLowerInvariant();

            return state == BasicState.WaitingForCommands && (cmd == "/help" || cmd == "/start" || cmd == "/format");
        }

        private bool canDoSubCommand(BasicState state, Message message)
        {
            return message.Type == MessageType.Text && state == BasicState.WaitingForCommands && message.Text.ToLowerInvariant() == "/subscribe";
        }

        private bool canDoSubUnsub(BasicState state, Message message)
        {
            return (state == BasicState.WaitingForSub || state == BasicState.WaitingForUnsub) && message.ForwardFromChat?.Type == ChatType.Channel;
        }

        private bool canDoUnknownCommand(BasicState state, Message message)
        {
            return message.Type == MessageType.Text && state == BasicState.WaitingForCommands && message.Text.StartsWith("/");
        }

        private bool canDoUnsubCommand(BasicState state, Message message)
        {
            return message.Type == MessageType.Text && state == BasicState.WaitingForCommands && message.Text.ToLowerInvariant() == "/unsubscribe";
        }

        private bool canDoUnsupported(BasicState _, Message message)
        {
            return message.Type != MessageType.Text || message.ForwardFromChat == null;
        }

        private bool canSkip(WaitingForArtDetails state, Message message)
        {
            return message.Text.ToLowerInvariant() == "/skip";
        }

        private WaitingForArtDetails doAddArt(BasicState state, Message message)
        {
            client.SendTextMessageAsync(message.From.Id, "Waiting for details in the next text message.\r\nWriting /skip will add the art as-is.\r\nWriting /cancel will stop the process of adding it.");

            return new WaitingForArtDetails(message);
        }

        private BasicState doAddDetails(WaitingForArtDetails state, Message message)
        {
            var channel = database.GetChannel(state.FileMessage.ForwardFromChat, getJoinLink);

            database.AddArt(channel, state.FileMessage, message);
            client.SendTextMessageAsync(message.From.Id, "Added the Image with the details from the text message!");

            return BasicState.WaitingForCommands;
        }

        private BasicState doCancel(WaitingForArtDetails _, Message message)
        {
            client.SendTextMessageAsync(message.From.Id, "Canceled.");

            return BasicState.WaitingForCommands;
        }

        private WaitingForArtDetails doInvalidDetail(WaitingForArtDetails state, Message message)
        {
            if (message.Text?.ToLowerInvariant() == "/skip")
                client.SendTextMessageAsync(message.From.Id, "The Image must have a caption to skip the detail message, use /cancel instead!");
            else
                client.SendTextMessageAsync(message.From.Id, "Please send a text message to add details, or write /skip or /cancel\r\n/skip will add the art as-is.\r\n/cancel will stop the process of adding it.");

            return state;
        }

        private BasicState doSkip(WaitingForArtDetails state, Message message)
        {
            var channel = database.GetChannel(state.FileMessage.ForwardFromChat, getJoinLink);

            database.AddArt(channel, state.FileMessage);
            client.SendTextMessageAsync(message.From.Id, "Added the Image with the details from its caption!");

            return BasicState.WaitingForCommands;
        }

        private BasicState doStandardCommand(BasicState _, Message message)
        {
            switch (message.Text.ToLowerInvariant())
            {
                case "/help":
                case "/start":
                    client.SendTextMessageAsync(message.From.Id, helpText, ParseMode.Markdown);
                    break;

                case "/format":
                    client.SendTextMessageAsync(message.From.Id, formatText, ParseMode.Markdown);
                    break;
            }

            return BasicState.WaitingForCommands;
        }

        private BasicState doSub(BasicState _, Message message)
        {
            var user = database.GetUser(message.From.Id, message.Chat.Id);
            var channel = database.GetChannel(message.ForwardFromChat, getJoinLink);

            database.AddSubscription(user, channel);
            client.SendTextMessageAsync(user.UserId, $"You have subscribed to **{channel.Name}**. It may take a moment till new results appear for recent queries.", ParseMode.Markdown);

            return BasicState.WaitingForCommands;
        }

        private BasicState doSubCommand(BasicState _, Message message)
        {
            client.SendTextMessageAsync(message.From.Id, subscribeText);

            return BasicState.WaitingForSub;
        }

        private BasicState doUnknownCommand(BasicState _, Message message)
        {
            client.SendTextMessageAsync(message.From.Id, unknownCommandText);

            return BasicState.WaitingForCommands;
        }

        private BasicState doUnsub(BasicState _, Message message)
        {
            var user = database.GetUser(message.From.Id, message.Chat.Id);
            var channel = database.GetChannel(message.ForwardFromChat, getJoinLink);

            database.RemoveSubscription(user, channel);
            client.SendTextMessageAsync(user.UserId, $"You have been unsubscribed from **{channel.Name}**. It may take a moment till the results disappear for recent queries.", ParseMode.Markdown);

            return BasicState.WaitingForCommands;
        }

        private BasicState doUnsubCommand(BasicState _, Message message)
        {
            client.SendTextMessageAsync(message.From.Id, unsubscribeText);

            return BasicState.WaitingForUnsub;
        }

        private BasicState doUnsupported(BasicState _, Message message)
        {
            client.SendTextMessageAsync(message.From.Id, "Not supported! Canceling.");

            return BasicState.WaitingForCommands;
        }

        private string getJoinLink(Chat channel)
        {
            return client.ExportChatInviteLinkAsync(channel.Id).Result;
        }

        private async Task<bool> isChatAdmin(long chat, long user)
        {
            var admins = await client.GetChatAdministratorsAsync(chat);

            return admins.Any(admin => admin.User.Id == user);
        }
    }
}