using System;
using System.Linq;
using System.Collections.Generic;
using Banane9.TelegramBots.ArtChannelBot.Data;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace Banane9.TelegramBots.ArtChannelBot
{
    internal class Program
    {
        private static ArtDb database = new ArtDb();

        private static async void BotClient_OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
            Console.WriteLine("Query: " + e.InlineQuery.Query);
            var client = (TelegramBotClient)sender;

            var query = e.InlineQuery.Query.Split(' ');
            var x = 0;

            var result = database.SearchArt(query).Select(async artResult => new InlineQueryResultCachedPhoto
            {
                Id = (++x).ToString(),
                FileId = artResult.FileId,
                Title = artResult.ArtName,
                Description = $"{artResult.ArtName} from {artResult.ChannelName}",
                Caption = $"{artResult.ArtName}\r\n{await client.ExportChatInviteLinkAsync(artResult.ChannelId)}"
            }).Select(t => t.Result).ToArray();

            await client.AnswerInlineQueryAsync(e.InlineQuery.Id, result, isPersonal: true, cacheTime: 60);
        }

        private static async void BotClient_OnUpdate(object sender, UpdateEventArgs e)
        {
            Console.WriteLine("Update Type: " + e.Update.Type);

            var client = (TelegramBotClient)sender;
            switch (e.Update.Type)
            {
                case UpdateType.ChannelPost:
                    if (e.Update.ChannelPost.Photo == null)
                        break;

                    var channel = database.GetChannel(e.Update.ChannelPost.Chat);
                    database.AddArt(channel, e.Update.ChannelPost);
                    break;

                case UpdateType.EditedChannelPost:
                    if (e.Update.EditedChannelPost.Photo == null)
                        break;

                    var uChannel = database.GetChannel(e.Update.ChannelPost.Chat);
                    database.UpdateArt(uChannel, e.Update.ChannelPost);
                    break;
            }
        }

        private static void Main(string[] args)
        {
            var botClient = new TelegramBotClient("471628940:AAES_fQeiL9UTmzlkJkAWbdgMKuzLyxiEYo");
            botClient.OnInlineQuery += BotClient_OnInlineQuery;
            botClient.OnUpdate += BotClient_OnUpdate;

            Console.Title = botClient.GetMeAsync().Result.FirstName;

            botClient.StartReceiving();
            Console.ReadLine();
            botClient.StopReceiving();
        }
    }
}