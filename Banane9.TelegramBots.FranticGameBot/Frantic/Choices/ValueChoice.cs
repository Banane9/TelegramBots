﻿using System;
using System.Collections.Generic;

namespace Banane9.TelegramBots.FranticGameBot.Frantic.Choices
{
    public sealed class ValueChoice : Choice
    {
        public uint Value { get; }

        public ValueChoice(uint value)
        {
            Value = value;
        }
    }
}