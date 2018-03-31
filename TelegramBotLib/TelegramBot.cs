using System;
using System.Linq;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;

namespace TelegramBotLib
{
    public abstract class TelegramBot
    {
        protected readonly TelegramBotClient client;
        private readonly Lazy<User> self;

        private InlineQueryTable inlineQueryTable = new InlineQueryTable();

        public TimeSpan InlineQueryTimeout
        {
            get { return inlineQueryTable.DefaultTimeout; }
            set { inlineQueryTable.DefaultTimeout = value; }
        }

        public User Self
        {
            get { return self.Value; }
        }

        public TelegramBot(string botToken)
        {
            client = new TelegramBotClient(botToken);

            self = new Lazy<User>(() => client.GetMeAsync().Result);

            client.ApiResponseReceived += (_, apiResponse) => OnApiResponseReceived(apiResponse);
            client.MakingApiRequest += (_, apiRequest) => OnMakingApiRequest(apiRequest);
            client.OnReceiveError += (_, error) => OnReceiveError(error.ApiRequestException);
            client.OnReceiveGeneralError += (_, error) => OnReceiveGeneralError(error.Exception);

            client.OnUpdate += client_OnUpdate;
        }

        protected virtual void InlineQueryTask(InlineQuery inlineQuery)
        { }

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
            inlineQueryTable.Run(inlineQuery.From.Id, () => InlineQueryTask(inlineQuery));
        }

        protected virtual void OnInlineResultChosen(ChosenInlineResult inlineResult)
        {
            inlineQueryTable.Cancel(inlineResult.From.Id);
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