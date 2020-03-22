using System;
using System.Collections.Generic;
using System.Linq;
using Banane9.TelegramBots.FranticGameBot.Frantic.Blacks;
using Banane9.TelegramBots.FranticGameBot.Frantic.Choices;
using Banane9.TelegramBots.FranticGameBot.Frantic.Whites;

namespace Banane9.TelegramBots.FranticGameBot.Frantic
{
    public class FranticGame
    {
        private int _currentPlayerIndex = 0;
        public static IEnumerable<Color> RealColors { get; } = new[] { Color.Blue, Color.Green, Color.Red, Color.Yellow };
        public Dictionary<string, BlackCard> Cards { get; }
        public List<Choice> Choices { get; } = new List<Choice>();
        public Player CurrentPlayer => Players[currentPlayerIndex];
        public Deck<BlackCard> DrawingDeck { get; }
        public Deck<WhiteCard> EventDeck { get; }
        public BlackCard LastCard => PlayStack.Peek();
        public List<Player> Players { get; } = new List<Player>();
        public Stack<BlackCard> PlayStack { get; } = new Stack<BlackCard>();
        public uint StartingCardCount { get; } = 7;
        public Queue<IEnumerable<Type>> WaitingChoices { get; } = new Queue<IEnumerable<Type>>();
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
            var cards = generateBlackCards();
            Cards = cards.ToDictionary(card => card.StickerId);
            DrawingDeck = new Deck<BlackCard>(cards);

            EventDeck = new Deck<WhiteCard>(new WhiteCard[0]);
        }

        public void Start()
        {
            WaitingChoices.Clear();
            Choices.Clear();

            WishedColor = Color.None;
            WishedValue = 0;

            DrawingDeck.Reset();
            EventDeck.Reset();
            PlayStack.Clear();

            foreach (var player in Players)
            {
                player.Hand.Clear();
                player.Hand.AddRange(DrawingDeck.Draw(StartingCardCount));
            }
        }

        public bool TryPlay(BlackCard playedCard)
        {
            if (!playedCard.CheckPlayability(this))
                return false;

            CurrentPlayer.Hand.Remove(playedCard);
            PlayStack.Push(playedCard);
            playedCard.OnPlayed(this, Choices);

            WaitingChoices.Clear();
            Choices.Clear();

            ++currentPlayerIndex;

            return true;
        }

        private static IEnumerable<BlackCard> generateBlackCards()
        {
            /* foreach (var color in RealColors)
             {
                 for (uint i = 0; i < 18; ++i)
                     yield return new NumberCard(color, (i % 9) + 1);

                 yield return new NumberCard(color, 10);

                 // colored specials
             }

             for (uint i = 1; i < 11; ++i)
                 yield return new NumberCard(Color.None, 10);
             */

            //for (var i = 1; i <= 11; ++i)
            yield return new FantasticCard();

            // for (var i = 1; i <= 5; ++i)
            yield return new FantasticFourCard();

            // specials

            yield return new TheEndCard();
        }
    }
}