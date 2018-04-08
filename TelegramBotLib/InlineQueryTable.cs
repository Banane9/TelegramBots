using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace TelegramBotLib
{
    public class InlineQueryTable
    {
        private readonly Timer cleanupTimer;
        private readonly TelegramBotClient client;
        private readonly Dictionary<int, QueryEntry> dictionary = new Dictionary<int, QueryEntry>();
        private readonly Func<InlineQuery, IEnumerable<InlineQueryResultBase>> getResults;
        public int CacheTime { get; set; }
        public bool IsPersonal { get; set; }
        public int PageSize { get; set; }

        public InlineQueryTable(TelegramBotClient client, Func<InlineQuery, IEnumerable<InlineQueryResultBase>> getResults,
            int pageSize = 5, bool isPersonal = false, int cacheTime = 300)
        {
            this.client = client;
            this.getResults = getResults;

            PageSize = pageSize;
            IsPersonal = isPersonal;
            CacheTime = cacheTime;

            cleanupTimer = new Timer(cleanup, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public void Add(InlineQuery query)
        {
            lock (dictionary)
            {
                if (!int.TryParse(query.Offset, out int offset))
                    offset = 0;

                dictionary[query.From.Id] = new QueryEntry(query.Query, getResults(query), offset);
            }
        }

        public void Query(InlineQuery query)
        {
            var user = query.From.Id;

            if (!dictionary.ContainsKey(user) || dictionary[user].Query != query.Query)
                Add(query);

            var resultChunk = dictionary[user].GetNext(PageSize);
            client.AnswerInlineQueryAsync(query.Id, resultChunk, CacheTime, IsPersonal, dictionary[user].Offset.ToString());
        }

        public void Remove(int user)
        {
            lock (dictionary)
            {
                dictionary.Remove(user);
            }
        }

        private void cleanup(object _)
        {
            lock (dictionary)
            {
                var users = dictionary.Keys.ToArray();

                foreach (var user in users)
                {
                    if (dictionary[user].IsDone)
                        dictionary.Remove(user);
                }
            }
        }

        private class QueryEntry
        {
            private IEnumerable<InlineQueryResultBase> results;
            public bool IsDone => !results.Any();
            public int Offset { get; private set; }
            public string Query { get; }

            public QueryEntry(string query, IEnumerable<InlineQueryResultBase> results, int offset)
            {
                Query = query;
                this.results = results.Skip(offset);
                Offset = offset;
            }

            public IEnumerable<InlineQueryResultBase> GetNext(int count)
            {
                var result = results.Take(count);

                results = results.Skip(count);
                Offset += count;

                return result;
            }
        }
    }
}