using System;
using System.Collections.Generic;
using RoutineBot.Repository.Model;

namespace RoutineBot.Repository
{
    public interface IRemindersRepository
    {
        event Action<TimeSpan, Chat> TimeZoneChanged;
        event Action<Reminder> ReminderAdded;
        event Action<Reminder> ReminderRemoved;

        bool TryGetChat(long chatId, out Chat chat);
        IEnumerable<Chat> GetChats();
        bool ChatExists(long chatId);
        void SetTimeZone(long ChatId, TimeSpan timeZone);
        void StoreReminder(Reminder reminder);
        void RemoveReminder(long chatId, long reminderId);

    }
}