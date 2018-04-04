using System;
using System.Linq;
using System.Collections.Generic;

using System.Linq;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.Payments;

namespace TelegramBotLib
{
    public abstract class TelegramBot
    {
        protected readonly TelegramBotClient client;
        private readonly InlineQueryTable inlineQueryTable;
        private readonly Lazy<User> self;

        public User Self
        {
            get { return self.Value; }
        }

        public TelegramBot(string botToken)
        {
            client = new TelegramBotClient(botToken);

            self = new Lazy<User>(() => client.GetMeAsync().Result);
            inlineQueryTable = new InlineQueryTable(client, GetInlineQueryResults);

            client.ApiResponseReceived += (_, apiResponse) => OnApiResponseReceived(apiResponse);
            client.MakingApiRequest += (_, apiRequest) => OnMakingApiRequest(apiRequest);
            client.OnReceiveError += (_, error) => OnReceiveError(error.ApiRequestException);
            client.OnReceiveGeneralError += (_, error) => OnReceiveGeneralError(error.Exception);

            client.OnUpdate += client_OnUpdate;
        }

        protected virtual IEnumerable<InlineQueryResultBase> GetInlineQueryResults(InlineQuery inlineQuery)
        {
            return Enumerable.Empty<InlineQueryResultBase>();
        }

        protected virtual void OnApiResponseReceived(ApiResponseEventArgs apiResponse)
        { }

        protected virtual void OnCallbackQuery(CallbackQuery callbackQuery)
        { }

        protected virtual void OnChannelPost(Message channelPost)
        { }

        protected virtual void OnChannelPostEdited(Message editedChannelPost)
        { }

        protected virtual void OnInlineQuery(InlineQuery inlineQuery)
        {
            inlineQueryTable.Query(inlineQuery);
        }

        protected virtual void OnInlineResultChosen(ChosenInlineResult inlineResult)
        {
            inlineQueryTable.Remove(inlineResult.From.Id);
        }

        protected virtual void OnMakingApiRequest(ApiRequestEventArgs apiRequest)
        { }

        protected virtual void OnMessage(Message message)
        { }

        protected virtual void OnMessageEdited(Message editedMessage)
        { }

        protected virtual void OnPreCheckoutQuery(PreCheckoutQuery checkoutQuery)
        { }

        protected virtual void OnReceiveError(ApiRequestException requestException)
        { }

        protected virtual void OnReceiveGeneralError(Exception exception)
        { }

        protected virtual void OnShippingQuery(ShippingQuery shippingQuery)
        { }

        private void client_OnUpdate(object sender, UpdateEventArgs e)
        {
            var update = e.Update;
            switch (update.Type)
            {
                case UpdateType.CallbackQuery:
                    OnCallbackQuery(update.CallbackQuery);
                    break;

                case UpdateType.ChannelPost:
                    OnChannelPost(update.ChannelPost);
                    break;

                case UpdateType.ChosenInlineResult:
                    OnInlineResultChosen(update.ChosenInlineResult);
                    break;

                case UpdateType.EditedChannelPost:
                    OnChannelPostEdited(update.EditedChannelPost);
                    break;

                case UpdateType.EditedMessage:
                    OnMessageEdited(update.EditedMessage);
                    break;

                case UpdateType.InlineQuery:
                    OnInlineQuery(update.InlineQuery);
                    break;

                case UpdateType.Message:
                    OnMessage(update.Message);
                    break;

                case UpdateType.PreCheckoutQuery:
                    OnPreCheckoutQuery(update.PreCheckoutQuery);
                    break;

                case UpdateType.ShippingQuery:
                    OnShippingQuery(update.ShippingQuery);
                    break;

                default:
                    // Unknown
                    break;
            }
        }
    }
}