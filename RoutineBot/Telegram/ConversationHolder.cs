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

namespace RoutineBot.Telegram
{
    public class ConversationHolder
    {
        public static ILogger logger = Program.LogFactory.CreateLogger<MessageHandler>();

        Dictionary<long, IConversation> conversations = new Dictionary<long, IConversation>();
        ReaderWriterLockSlim converstaionsLock = new ReaderWriterLockSlim();

        public bool TryCreateConversation(long chatId, string conversationType, out Message message)
        {
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
                    message = null;
                    return false;

            }
            IConversation conv = (IConversation)Activator.CreateInstance(t);
            this.storeConversation(chatId, conv);
            message = conv.Initialize(chatId);
            return true;
        }

        public Message ProcessMessage(long chatId, Update u)
        {
            Message message = null;
            IConversation conversation;
            if (tryGetConversation(chatId, out conversation))
            {
                message = conversation.ProcessUpdate(u);
                if (conversation.Finished)
                {
                    this.removeConversation(chatId, conversation);
                }
            }
            return message;
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