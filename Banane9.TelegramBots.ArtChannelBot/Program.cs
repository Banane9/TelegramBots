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
        private static Storage storage;

        private static async void BotClient_OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
            Console.WriteLine("Query: " + e.InlineQuery);
        }

        private static async void BotClient_OnUpdate(object sender, UpdateEventArgs e)
        {
            Console.WriteLine("Update Type: " + e.Update.Type);
            //if (e.Update.Type != UpdateType.ChannelPost)
            //    return;

            var client = (TelegramBotClient)sender;
            switch (e.Update.Type)
            {
                case UpdateType.ChannelPost:
                    if (e.Update.ChannelPost.Photo == null)
                        break;

                    var channel = storage.ArtChannels.SingleOrDefault(chan => e.Update.ChannelPost.Chat.Id == chan.Id);
                    if (channel == null)
                    {
                        channel = new ArtChannel(e.Update.ChannelPost.Chat.Id);
                        storage.ArtChannels.Add(channel);
                    }

                    channel.Artworks.Add(Artwork.ParseFromMessage(e.Update.ChannelPost.Caption, e.Update.ChannelPost.MessageId, e.Update.ChannelPost.Photo[0].FileId));

                    storage.WriteStorage();
                    break;

                case UpdateType.InlineQueryUpdate:
                    var query = e.Update.InlineQuery.Query.ToLowerInvariant().Split(' ');
                    var x = 0;
                    var result = storage.ArtChannels.Select(chan => new
                    {
                        Channel = chan,
                        Art = chan.Artworks.Where(art =>
                        {
                            foreach (var q in query)
                                if (art.Artists.Any(artist => artist.ToLowerInvariant().Contains(q))
                                || art.Characters.Any(character => character.ToLowerInvariant().Contains(q))
                                || art.Tags.Any(tag => tag.Contains(q))
                                || art.Name.ToLowerInvariant().Contains(q))
                                    return true;

                            return false;
                        }).ToArray()
                    }).Where(r => r.Art.Length > 0).SelectMany(r => r.Art.Select(async art => new InlineQueryResultCachedPhoto
                    {
                        Id = (++x).ToString(),
                        FileId = art.FileId,
                        Title = art.Name,
                        Description = string.Join(", ", art.Tags),
                        Caption = $"{art.Name}\r\n{await client.ExportChatInviteLinkAsync(r.Channel.Id)}"
                    })).Select(r => r.Result).ToArray();

                    await client.AnswerInlineQueryAsync(e.Update.InlineQuery.Id, result, isPersonal: true, cacheTime: 60);
                    break;
            }

            //if (e.Update.Type == UpdateType.MessageUpdate)
            //    Console.WriteLine("Message: " + e.Update.Message.Text);
            ////Console.WriteLine("Caption: " + e.Update.ChannelPost.Text);
            //Console.WriteLine("Message id: " + e.Update.ChannelPost.MessageId);
            //Console.WriteLine("Message forward from id: " + e.Update.ChannelPost.ForwardFromMessageId);

            //if (e.Update.Type == UpdateType.InlineQueryUpdate)
            //{
            //    await ((TelegramBotClient)sender).AnswerInlineQueryAsync(e.Update.InlineQuery.Id, new InlineQueryResult[]
            //    {
            //        new InlineQueryResultLocation
            //        {
            //            Id = "1",
            //            Latitude = 40.7058316f, // displayed result
            //            Longitude = -74.2581888f,
            //            Title = "New York",
            //            InputMessageContent = new InputLocationMessageContent // message if result is selected
            //            {
            //                Latitude = 40.7058316f,
            //                Longitude = -74.2581888f,
            //            }
            //        },
            //        new InlineQueryResultArticle
            //        {
            //            Id = "2",
            //            Title = "Oi what",
            //            InputMessageContent = new InputTextMessageContent
            //            {
            //                MessageText = "Oi my boi"
            //            }
            //        }
            //    });
            //}
        }

        private static void Main(string[] args)
        {
            var botClient = new TelegramBotClient("471628940:AAES_fQeiL9UTmzlkJkAWbdgMKuzLyxiEYo");
            botClient.OnInlineQuery += BotClient_OnInlineQuery;
            botClient.OnUpdate += BotClient_OnUpdate;

            Console.Title = botClient.GetMeAsync().Result.FirstName;

            storage = Storage.LoadStorage();
            botClient.StartReceiving();

            Console.ReadLine();

            botClient.StopReceiving();
        }
    }
}