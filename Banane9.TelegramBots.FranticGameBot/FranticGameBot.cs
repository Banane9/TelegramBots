using System;
using System.Collections.Generic;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotLib;

namespace Banane9.TelegramBots.FranticGameBot
{
    public class FranticGameBot : TelegramBot
    {
        public FranticGameBot(string botToken)
            : base(botToken)
        {
        }

        protected override void OnMessage(Message message)
        {
            var switchInlineQuery = new InlineKeyboardMarkup(new InlineKeyboardButton() { Text = "Pick a card!", SwitchInlineQueryCurrentChat = message.Chat.Id.ToString() });
            client.SendTextMessageAsync(message.Chat, $"It's your turn, @{message.From.Username}", replyMarkup: switchInlineQuery).Wait();
        }
    }
}