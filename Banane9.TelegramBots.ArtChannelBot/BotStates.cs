using System;
using System.Linq;
using System.Collections.Generic;
using Telegram.Bot.Types;

namespace Banane9.TelegramBots.ArtChannelBot
{
    internal sealed class WaitingForArtDetails
    {
        public Message FileMessage { get; }

        public WaitingForArtDetails(Message fileMessage)
        {
            FileMessage = fileMessage;
        }
    }

    internal sealed class WaitingForCommands
    { }

    internal sealed class WaitingForSub
    {
        public int User { get; }

        public WaitingForSub(int user)
        {
            User = user;
        }
    }

    internal sealed class WaitingForUnsub
    {
        public int User { get; }

        public WaitingForUnsub(int user)
        {
            User = user;
        }
    }
}