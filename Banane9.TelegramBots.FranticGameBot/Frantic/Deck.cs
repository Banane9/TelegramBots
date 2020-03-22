using System;
using System.Collections.Generic;
using System.Linq;

namespace Banane9.TelegramBots.FranticGameBot.Frantic
{
    public static class Deck
    {
        private readonly static Random r = new Random();

        public static void PushRange<T>(this Stack<T> stack, IEnumerable<T> collection)
        {
            foreach (var item in collection)
                stack.Push(item);
        }

        public static T[] Shuffle<T>(T[] deck)
        {
            // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle

            for (var n = deck.Length - 1; n > 0; --n)
            {
                var k = r.Next(n + 1);

                var temp = deck[n];
                deck[n] = deck[k];
                deck[k] = temp;
            }

            return deck;
        }
    }

    public sealed class Deck<T>
    {
        private readonly T[] cards;
        private readonly Stack<T> deck;

        public bool CanDraw => deck.Count > 0;

        public Deck(IEnumerable<T> cards)
        {
            this.cards = cards.ToArray();
            deck = new Stack<T>(Deck.Shuffle(this.cards));
        }

        public IEnumerable<T> Draw(uint amount = 1)
        {
            for (var i = 0; i < amount && CanDraw; ++i)
                yield return deck.Pop();
        }

        public void PlaceOnTop(IEnumerable<T> cards)
        {
            deck.PushRange(Deck.Shuffle(cards.ToArray()));
        }

        public void Reset()
        {
            deck.Clear();
            deck.PushRange(Deck.Shuffle(cards));
        }
    }
}