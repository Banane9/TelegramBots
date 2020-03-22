using System;
using System.Collections.Generic;

namespace Banane9.TelegramBots.FranticGameBot.Frantic.Blacks
{
    public sealed class TheEndCard : BlackCard
    {
        public override Color Color => Color.None;
        public override string StickerId { get; } = "CAACAgIAAxkBAAI14F52zfAVZIbSU9EdzANKeEmenIuwAAJNAQAC5dKnAhombfl0610JGAQ";
        public override uint Value { get; } = 17;

        public override bool CheckPlayability(FranticGame game)
        {
            return base.CheckPlayability(game) && game.CurrentPlayer.Hand.Count == 1;
        }

        public override string ToString()
        {
            return "The End";
        }
    }
}