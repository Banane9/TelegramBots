using System;
using System.Collections.Generic;
using Banane9.TelegramBots.FranticGameBot.Frantic.Blacks;
using Banane9.TelegramBots.FranticGameBot.Frantic.Whites;

namespace Banane9.TelegramBots.FranticGameBot.Frantic
{
    public class FranticGame
    {
        private int _currentPlayerIndex = 0;
        public static IEnumerable<Color> RealColors { get; } = new[] { Color.Blue, Color.Green, Color.Red, Color.Yellow };
        public Player CurrentPlayer => Players[currentPlayerIndex];
        public Deck<BlackCard> DrawingDeck { get; }
        public Deck<WhiteCard> EventDeck { get; }
        public BlackCard LastCard => PlayStack.Peek();
        public List<Player> Players { get; }
        public Stack<BlackCard> PlayStack { get; }
        public Color WishedColor { get; private set; }

        public int WishedValue { get; private set; }

        private int currentPlayerIndex
        {
            get { return _currentPlayerIndex; }
            set
            {
                _currentPlayerIndex = value % Players.Count;
            }
        }

        public FranticGame()
        {
            DrawingDeck = new Deck<BlackCard>(generateBlackCards());
        }

        private static IEnumerable<BlackCard> generateBlackCards()
        {
            foreach (var color in RealColors)
            {
                for (uint i = 0; i < 18; ++i)
                    yield return new NumberCard(color, (i % 9) + 1);

                yield return new NumberCard(color, 10);

                // colored specials
            }

            for (uint i = 1; i < 11; ++i)
                yield return new NumberCard(Color.None, 10);

            for (var i = 1; i <= 11; ++i)
                yield return new FantasticCard();

            for (var i = 1; i <= 5; ++i)
                yield return new FantasticFourCard();

            // specials

            yield return new TheEndCard();
        }
    }
}