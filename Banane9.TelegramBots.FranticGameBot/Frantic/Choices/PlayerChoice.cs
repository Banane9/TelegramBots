using System;
using System.Collections.Generic;
namespace Banane9.TelegramBots.FranticGameBot.Frantic.Choices
{
    public sealed class PlayerChoice : Choice
    {
        public Player Player { get; set; } = null;
    }
}