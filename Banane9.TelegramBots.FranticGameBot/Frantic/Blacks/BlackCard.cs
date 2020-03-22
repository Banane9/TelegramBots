using System;
using System.Collections.Generic;
using System.Linq;
using Banane9.TelegramBots.FranticGameBot.Frantic.Choices;

namespace Banane9.TelegramBots.FranticGameBot.Frantic.Blacks
{
    public abstract class BlackCard : ICard
    {
        public virtual IEnumerable<IEnumerable<Type>> Choices { get; } = Enumerable.Empty<IEnumerable<Type>>();
        public abstract Color Color { get; }

        // CAACAgIAAxkBAAI13l52zbgUp2G9yBOQQWIZgdH_xXycAALlAAPl0qcCfldiVzuk8X4YBA
        public abstract string StickerId { get; }

        public abstract uint Value { get; }

        public virtual bool CheckPlayability(FranticGame game)
        {
            return game.CurrentPlayer.Hand.Contains(this);
        }

        public virtual void OnPlayed(FranticGame game, IEnumerable<IEnumerable<Choice>> playerChoices)
        { }
    }
}