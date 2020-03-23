using System;
using System.Collections.Generic;
using System.Linq;
using Banane9.TelegramBots.FranticGameBot.Frantic;
using Banane9.TelegramBots.FranticGameBot.Frantic.Blacks;
using Banane9.TelegramBots.FranticGameBot.Frantic.Choices;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotLib;

namespace Banane9.TelegramBots.FranticGameBot
{
    public class FranticGameBot : TelegramBot
    {
        private static readonly Dictionary<Color, string> colorHearts = new Dictionary<Color, string>(4)
        {
            { Color.Blue, "💙" },
            {Color.Green, "💚" },
            {Color.Red, "❤️" },
            {Color.Yellow, "💛" }
        };

        private Dictionary<string, FranticGame> games = new Dictionary<string, FranticGame>();

        public FranticGameBot(string botToken)
            : base(botToken)
        {
            var game = new FranticGame();
            game.Players.Add(new Player("Banane9"));
            game.Players.Add(new Player("Torui"));
            games.Add("-1001403862746", game);
            game.Start();
        }

        protected override void OnCallbackQuery(CallbackQuery callbackQuery)
        {
            Console.WriteLine(callbackQuery.Data);
            var split = callbackQuery.Data.Split(' ');

            if (split.Length != 3 || !games.ContainsKey(split[0]))
                return;

            var game = games[split[0]];
            if (game.CurrentPlayer.Name != callbackQuery.From.Username
             || !game.WaitingChoices.Peek().Any(c => c.Name == split[1]))
                return;

            game.WaitingChoices.Dequeue();
            Choice choice;
            switch (split[1])
            {
                case "ColorChoice":
                    choice = new ColorChoice((Color)int.Parse(split[2]));
                    break;

                case "ValueChoice":
                    choice = new ValueChoice(uint.Parse(split[2]));
                    break;

                default:
                    choice = null;
                    break;
            }

            game.Choices.Add(choice);
        }

        protected override void OnInlineQuery(InlineQuery inlineQuery)
        {
            if (!games.ContainsKey(inlineQuery.Query))
                return;

            var fantasticCard = new FantasticCard();

            client.AnswerInlineQueryAsync(inlineQuery.Id, new[] { new InlineQueryResultCachedSticker("end", new TheEndCard().StickerId)
            { ReplyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton() { CallbackData = "test", Text = "Test!" }) }, new InlineQueryResultCachedSticker("fantastic", fantasticCard.StickerId)
            {
                ReplyMarkup = makeInlineKeyboardMarkup(inlineQuery.Query, fantasticCard.Choices).First()
            }
            }, 0, true).Wait();
        }

        protected override void OnMessage(Message message)
        {
            var gameId = message.Chat.Id.ToString();

            if (!games.ContainsKey(gameId))
                client.SendTextMessageAsync(message.Chat, "No Game yet!", replyToMessageId: message.MessageId).Wait();

            if (message.Sticker == null)
                return;

            if (games[gameId].CurrentPlayer.Name != message.From.Username)
                client.SendTextMessageAsync(message.Chat, "Not your turn!", replyToMessageId: message.MessageId).Wait();

            var playedCard = games[gameId].Cards[message.Sticker.FileUniqueId];

            if (!games[gameId].TryPlay(playedCard))
            {
                client.SendTextMessageAsync(message.Chat, "Sorry, you can't play this card now!", replyToMessageId: message.MessageId).Wait();
                client.DeleteMessageAsync(message.Chat, message.MessageId).Wait();
            }

            var switchInlineQuery = new InlineKeyboardMarkup(new InlineKeyboardButton() { Text = "Pick a card!", SwitchInlineQueryCurrentChat = message.Chat.Id.ToString() });
            client.SendTextMessageAsync(message.Chat, $"It's your turn, @{games[gameId].CurrentPlayer.Name}", replyMarkup: switchInlineQuery).Wait();
        }

        private IEnumerable<IEnumerable<InlineKeyboardButton>> makeInlineKeyboardButtons(string id, IEnumerable<Type> choices)
        {
            foreach (var choice in choices)
            {
                if (choice == typeof(ColorChoice))
                {
                    yield return FranticGame.RealColors.Select(color => new InlineKeyboardButton()
                    {
                        Text = $"{color} {colorHearts[color]}",
                        CallbackData = $"{id} {choice.Name} {(int)color}"
                    });
                }
                else if (choice == typeof(ValueChoice))
                {
                    yield return Enumerable.Range(1, 5).Select(num => new InlineKeyboardButton()
                    {
                        Text = $"{num}",
                        CallbackData = $"{id} {choice.Name} {num}"
                    });
                    yield return Enumerable.Range(6, 5).Select(num => new InlineKeyboardButton()
                    {
                        Text = $"{num}",
                        CallbackData = $"{id} {choice.Name} {num}"
                    });
                }
            }
        }

        private IEnumerable<InlineKeyboardMarkup> makeInlineKeyboardMarkup(string id, IEnumerable<IEnumerable<Type>> choices)
        {
            foreach (var choice in choices)
            {
                yield return new InlineKeyboardMarkup(makeInlineKeyboardButtons(id, choice));
            }
        }
    }
}