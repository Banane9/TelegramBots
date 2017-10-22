using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace Banane9.TelegramBots.ArtChannelBot.Data
{
    public class User
    {
        public long InternalId { get; private set; }

        public int UserId { get; private set; }

        public User(SQLiteDataReader reader)
        {
            InternalId = reader.GetInt64(0);
            UserId = reader.GetInt32(1);
        }

        public User(long internalId, int userId)
        {
            InternalId = internalId;
            UserId = userId;
        }

        public override bool Equals(object obj)
        {
            var objAsUser = obj as User;

            return objAsUser != null && objAsUser.UserId == UserId && objAsUser.InternalId == InternalId;
        }

        public override int GetHashCode()
        {
            return UserId;
        }

        public override string ToString()
        {
            return "User (" + UserId + ")";
        }
    }
}