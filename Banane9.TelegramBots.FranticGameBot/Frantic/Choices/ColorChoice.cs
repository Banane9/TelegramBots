using System;
using System.Collections.Generic;

namespace Banane9.TelegramBots.FranticGameBot.Frantic.Choices
{
    public sealed class ColorChoice : Choice
    {
        public Color Color { get; }

        public ColorChoice(Color color)
        {
            Color = color;
        }
    }
}