using System;
using System.Collections.Generic;
using Banane9.TelegramBots.FranticGameBot.Frantic;
using Banane9.TelegramBots.FranticGameBot.Frantic.Blacks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotLib;

namespace Banane9.TelegramBots.FranticGameBot
{
    public class FranticGameBot : TelegramBot
    {
        private Dictionary<string, FranticGame> games = new Dictionary<string, FranticGame>() { { "-482130963", new FranticGame() } };

        public FranticGameBot(string botToken)
            : base(botToken)
        {
        }

        protected override void OnInlineQuery(InlineQuery inlineQuery)
        {
            if (!games.ContainsKey(inlineQuery.Query))
                return;

            client.AnswerInlineQueryAsync(inlineQuery.Id, new[] { new InlineQueryResultCachedSticker("end", new TheEndCard().StickerId) }, 0, true).Wait();
        }

        protected override void OnMessage(Message message)
        {
            var switchInlineQuery = new InlineKeyboardMarkup(new InlineKeyboardButton() { Text = "Pick a card!", SwitchInlineQueryCurrentChat = message.Chat.Id.ToString() });
            client.SendTextMessageAsync(message.Chat, $"It's your turn, @{message.From.Username}", replyMarkup: switchInlineQuery).Wait();
        }
    }
}