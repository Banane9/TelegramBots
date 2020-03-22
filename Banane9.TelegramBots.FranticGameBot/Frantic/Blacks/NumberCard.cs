using System;
using System.Collections.Generic;

namespace Banane9.TelegramBots.FranticGameBot.Frantic.Blacks
{
    public class NumberCard : BlackCard
    {
        public override Color Color { get; }
        public override string StickerId { get; }
        public override uint Value { get; }

        public NumberCard(Color color, uint value)
        {
            Color = color;
            Value = value;
        }

        public override bool CheckPlayability(FranticGame game)
        {
            return base.CheckPlayability(game) && (
                (game.LastCard is NumberCard && (game.LastCard.Value == Value || (Color != Color.None && game.LastCard.Color == Color)))
                || (game.WishedColor != Color.None && game.WishedColor == Color)
                || (game.WishedValue > 0 && game.WishedValue == Value));
        }

        public override string ToString()
        {
            return $"{(Color == Color.None ? "Black" : Color.ToString())} {Value}";
        }
    }
}