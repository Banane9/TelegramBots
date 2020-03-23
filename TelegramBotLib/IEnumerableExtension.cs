using System;
using System.Collections.Generic;
using System.Linq;

namespace TelegramBotLib
{
    public static class IEnumerableExtension
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> list, int chunkSize)
        {
            while (list.Any())
                yield return list.Take(chunkSize);
        }
    }
}