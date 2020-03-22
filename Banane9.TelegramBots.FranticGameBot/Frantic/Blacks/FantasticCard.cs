﻿using System;
using System.Collections.Generic;
using Banane9.TelegramBots.FranticGameBot.Frantic.Choices;

namespace Banane9.TelegramBots.FranticGameBot.Frantic.Blacks
{
    public class FantasticCard : BlackCard
    {
        public override IEnumerable<IEnumerable<Type>> Choices { get; } = new[] { new[] { typeof(ColorChoice), typeof(ValueChoice) } };
        public override Color Color { get; } = Color.None;
        public override string StickerId { get; } = "CAACAgIAAxkBAAI2Nl53fwrY-zP993R5JG2_XR7pPh_VAAJGAQAC5dKnAuvBWS8B7zCmGAQ";
        public override uint Value { get; } = 7;

        public override string ToString()
        {
            return "Fantastic";
        }
    }
}