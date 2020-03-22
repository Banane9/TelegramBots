using System;
using System.Collections.Generic;

namespace Banane9.TelegramBots.FranticGameBot.Frantic.Choices
{
    public sealed class ExecuteEffectChoice : Choice
    {
        public bool ExecuteEffect { get; }

        public ExecuteEffectChoice(bool executeEffect)
        {
            ExecuteEffect = executeEffect;
        }
    }
}