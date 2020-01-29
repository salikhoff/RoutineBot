using System.Threading;
using System.Threading.Tasks;
using RoutineBot.Repository;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using Telegram.Bot.Args;

namespace RoutineBot.Telegram
{
    public class ConversationHolder
    {
        static ILogger logger = Program.LogFactory.CreateLogger<ConversationHolder>();

        Dictionary<long, IConversation> conversations = new Dictionary<long, IConversation>();
        ReaderWriterLockSlim converstaionsLock = new ReaderWriterLockSlim();

        public async void OnUpdate(object sender, UpdateEventArgs e)
        {
            try
            {
                await this.handleUpdateAsync((ITelegramBotClient)sender, e.Update);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        public void OnError(object sender, ReceiveGeneralErrorEventArgs e)
        {
            logger.LogError(e.Exception.ToString());
        }

        public void OnApiError(object sender, ReceiveErrorEventArgs e)
        {
            logger.LogError(e.ApiRequestException.ToString());
        }

        private async Task handleUpdateAsync(ITelegramBotClient client, Update update)
        {
            if (!await tryResetConversation(client, update) && !await tryCreateConversation(client, update) && !await tryContinueConversation(client, update))
            {
                await client.SendDefaultMessageAsync(update.GetChatId());
            }

        }

        private async Task<bool> tryResetConversation(ITelegramBotClient client, Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                long chatId = update.CallbackQuery.Message.Chat.Id;
                if (update.CallbackQuery.Data == TelegramHelper.HomeCommand)
                {
                    this.resetConversation(chatId);
                    await client.SendDefaultMessageAsync(chatId);
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> tryCreateConversation(ITelegramBotClient client, Update update)
        {
            if (update.Type != UpdateType.CallbackQuery)
            {
                return false;
            }
            long chatId = update.CallbackQuery.Message.Chat.Id;
            string conversationType = update.CallbackQuery.Data;
            Type t;
            switch (conversationType)
            {
                case TelegramHelper.SetTimeZoneCommand:
                    t = typeof(Conversations.TimeZoneConversation);
                    break;
                case TelegramHelper.AddReminderCommand:
                    t = typeof(Conversations.AddReminderConversation);
                    break;
                case TelegramHelper.RemoveReminderCommand:
                    t = typeof(Conversations.RemoveReminderConversation);
                    break;
                default:
                    return false;

            }
            IConversation conv = (IConversation)Activator.CreateInstance(t);
            this.storeConversation(chatId, conv);
            await conv.Initialize(client, update);
            return true;
        }

        private async Task<bool> tryContinueConversation(ITelegramBotClient client, Update update)
        {
            long chatId = update.GetChatId();
            IConversation conversation;
            if (tryGetConversation(chatId, out conversation))
            {
                await conversation.ProcessUpdate(client, update);
                if (conversation.Finished)
                {
                    this.removeConversation(chatId, conversation);
                }
                return true;
            }
            return false;
        }

        void storeConversation(long chatId, IConversation conv)
        {
            this.converstaionsLock.EnterWriteLock();
            try
            {
                this.conversations[chatId] = conv;
            }
            finally
            {
                this.converstaionsLock.ExitWriteLock();
            }
        }

        void removeConversation(long chatId, IConversation conversation)
        {
            this.converstaionsLock.EnterWriteLock();
            try
            {
                IConversation val;
                if (this.conversations.TryGetValue(chatId, out val) && val == conversation)
                {
                    this.conversations.Remove(chatId);
                }
            }
            finally
            {
                this.converstaionsLock.ExitWriteLock();
            }
        }

        void resetConversation(long chatId)
        {
            this.converstaionsLock.EnterWriteLock();
            try
            {
                this.conversations.Remove(chatId);
            }
            finally
            {
                this.converstaionsLock.ExitWriteLock();
            }
        }

        bool tryGetConversation(long chatId, out IConversation conv)
        {
            this.converstaionsLock.EnterReadLock();
            try
            {
                return this.conversations.TryGetValue(chatId, out conv);
            }
            finally
            {
                this.converstaionsLock.ExitReadLock();
            }
        }
    }
}