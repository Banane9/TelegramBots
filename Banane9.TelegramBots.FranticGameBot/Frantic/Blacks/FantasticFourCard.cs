using System;
using System.Collections.Generic;
using Banane9.TelegramBots.FranticGameBot.Frantic.Choices;

namespace Banane9.TelegramBots.FranticGameBot.Frantic.Blacks
{
    public sealed class FantasticFourCard : BlackCard
    {
        public override IEnumerable<IEnumerable<Type>> Choices { get; } = new[] { new[] { typeof(ColorChoice), typeof(ValueChoice) }, new[] { typeof(ExecuteEffectChoice) }, new[] { typeof(PlayerChoice) } };
        public override Color Color { get; } = Color.None;
        public override string StickerId { get; } = "CAACAgIAAxkBAAI2OF53jRa6VxFXmoUsbLBnDyqQ_KfFAAJFAQAC5dKnAnp4TYm4_iq1GAQ";
        public override uint Value { get; } = 7;

        public override string ToString()
        {
            return "Fantastic Four";
        }
    }
}