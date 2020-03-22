using System;
using System.Collections.Generic;
using System.Linq;
using Banane9.TelegramBots.FranticGameBot.Frantic.Blacks;

namespace Banane9.TelegramBots.FranticGameBot.Frantic
{
    public class Player
    {
        public HashSet<BlackCard> Hand { get; } = new HashSet<BlackCard>();

        public uint HandValue => (uint)Hand.Sum(card => card.Value);

        public string Name { get; }

        public int Score { get; set; } = 0;

        public Player(string name)
        {
            Name = name;
        }
    }
}