
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using RoutineBot.Repository.Model;

namespace RoutineBot.Repository.DB
{
    public class ReminderDbRepository : IRemindersRepository
    {
        private Dictionary<long, Chat> chats = new Dictionary<long, Chat>();
        private ReaderWriterLockSlim chatsLock = new ReaderWriterLockSlim();

        public event Action<TimeSpan, Chat> TimeZoneChanged;
        public event Action<Reminder> ReminderAdded;
        public event Action<Reminder> ReminderRemoved;

        public ReminderDbRepository()
        {
            using (RemindersDbContext context = new RemindersDbContext())
            {
                context.Database.EnsureCreated();

                this.chatsLock.EnterWriteLock();
                try
                {
                    foreach (Chat chat in context.Chats.Include(chat => chat.Reminders))
                    {
                        this.chats.Add(chat.ChatId, chat);
                    }
                }
                finally
                {
                    this.chatsLock.ExitWriteLock();
                }
            }
        }

        public bool TryGetChat(long chatId, out Chat chat)
        {
            this.chatsLock.EnterReadLock();
            try
            {
                if (this.chats.TryGetValue(chatId, out chat))
                {
                    chat = (Chat)chat.Clone();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                this.chatsLock.ExitReadLock();
            }
        }

        public IEnumerable<Chat> GetChats()
        {
            this.chatsLock.EnterReadLock();
            try
            {
                List<Chat> list = new List<Chat>();
                foreach (Chat chat in this.chats.Values)
                {
                    list.Add((Chat)chat.Clone());
                }
                return list;
            }
            finally
            {
                this.chatsLock.ExitReadLock();
            }
        }

        public void RemoveReminder(long chatId, long reminderId)
        {
            this.chatsLock.EnterWriteLock();
            try
            {

                Chat chat;
                if (this.chats.TryGetValue(chatId, out chat))
                {
                    Reminder reminder = chat.Reminders.FirstOrDefault(r => r.ReminderId == reminderId);
                    if (reminder != null)
                    {
                        chat.Reminders.Remove(reminder);
                        using (RemindersDbContext context = new RemindersDbContext())
                        {
                            context.Reminders.Remove(reminder);
                            context.SaveChanges();
                        }
                    }

                    this.ReminderRemoved(reminder);
                }
            }
            finally
            {
                this.chatsLock.ExitWriteLock();
            }
        }

        public void StoreReminder(Reminder reminder)
        {
            this.chatsLock.EnterWriteLock();
            try
            {
                using (RemindersDbContext context = new RemindersDbContext())
                {
                    context.Reminders.Add(reminder);
                    context.SaveChanges();
                }

                Chat chat;
                if (this.chats.TryGetValue(reminder.ChatId, out chat))
                {
                    chat.Reminders.Add(reminder);
                    reminder.Chat = chat;
                }

                this.ReminderAdded(reminder);
            }
            finally
            {
                this.chatsLock.ExitWriteLock();
            }
        }

        public void SetTimeZone(long chatId, TimeSpan timeZone)
        {
            this.chatsLock.EnterUpgradeableReadLock();
            try
            {
                Chat chat;
                if (this.chats.TryGetValue(chatId, out chat))
                {
                    if (chat.TimeZone != timeZone)
                    {
                        this.chatsLock.EnterWriteLock();
                        try
                        {
                            TimeSpan oldTimeZone = chat.TimeZone;
                            chat.TimeZone = timeZone;
                            using (RemindersDbContext context = new RemindersDbContext())
                            {
                                Chat dbchat = context.Chats.Where((c) => c.ChatId == chatId).FirstOrDefault();
                                dbchat.TimeZone = timeZone;
                                context.SaveChanges();
                            }
                            this.TimeZoneChanged(oldTimeZone, chat);
                        }
                        finally
                        {
                            this.chatsLock.ExitWriteLock();
                        }
                    }
                }
                else
                {
                    this.chatsLock.EnterWriteLock();
                    try
                    {
                        chat = new Chat { ChatId = chatId, TimeZone = timeZone, Reminders = new List<Reminder>() };
                        this.chats[chatId] = chat;
                        using (RemindersDbContext context = new RemindersDbContext())
                        {
                            context.Chats.Add(chat);
                            context.SaveChanges();
                        }
                    }
                    finally
                    {
                        this.chatsLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                this.chatsLock.ExitUpgradeableReadLock();
            }
        }

        public bool ChatExists(long chatId)
        {
            this.chatsLock.EnterReadLock();
            try
            {
                return this.chats.ContainsKey(chatId);
            }
            finally
            {
                this.chatsLock.ExitReadLock();
            }
        }
    }
}