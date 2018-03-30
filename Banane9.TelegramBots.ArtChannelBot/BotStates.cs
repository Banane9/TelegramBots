using System;
using System.Linq;
using System.Collections.Generic;
using Telegram.Bot.Types;

namespace Banane9.TelegramBots.ArtChannelBot
{
    internal enum BasicState
    {
        WaitingForCommands,
        WaitingForSub,
        WaitingForUnsub
    }

    internal sealed class WaitingForArtDetails
    {
        public Message FileMessage { get; }

        public WaitingForArtDetails(Message fileMessage)
        {
            FileMessage = fileMessage;
        }
    }
}