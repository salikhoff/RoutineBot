using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoutineBot.Repository.Model;

namespace RoutineBot
{
    public class RemindersQueue
    {
        SortedList<DateTime, List<Reminder>> queue = new SortedList<DateTime, List<Reminder>>();
        DateTime currentDateUtc = DateTime.UtcNow;

        public RemindersQueue()
        {
            Program.RemindersRepository.ReminderAdded += this.addReminderEvent;
            Program.RemindersRepository.ReminderRemoved += this.removeReminderEvent;
            Program.RemindersRepository.TimeZoneChanged += this.setTimeZoneEvent;

            IEnumerable<Chat> rs = Program.RemindersRepository.GetChats();
            lock (queue)
            {
                foreach (Chat chat in rs)
                {
                    foreach (Reminder reminder in chat.Reminders)
                    {
                        this.addReminder(reminder);
                    }
                }
            }
        }

        public async Task<IEnumerable<Reminder>> WaitForReminders(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (queue)
                {
                    currentDateUtc = DateTime.UtcNow;
                    if (this.queue.Count > 0)
                    {
                        DateTime nextReminderDate = this.queue.Keys[0];
                        if (currentDateUtc > nextReminderDate)
                        {
                            List<Reminder> list;
                            this.queue.Remove(nextReminderDate, out list);
                            DateTime tommorowReminderDate = nextReminderDate.AddDays(1);
                            List<Reminder> tommorowList;
                            if (this.queue.TryGetValue(tommorowReminderDate, out tommorowList))
                            {
                                tommorowList.AddRange(list);
                            }
                            else
                            {
                                this.queue.Add(tommorowReminderDate, list);
                            }

                            return list.Where(r => toPushOnDay(r, nextReminderDate));
                        }
                    }
                }
                await Task.Delay(100);
            }
            return new Reminder[0];
        }

        void addReminderEvent(Reminder reminder)
        {
            lock (queue)
            {
                this.addReminder(reminder);
            }
        }

        void removeReminderEvent(Reminder reminder)
        {
            lock (queue)
            {
                this.removeReminder(reminder, reminder.Chat.TimeZone);
            }
        }

        void setTimeZoneEvent(TimeSpan oldTimeZone, Chat chat)
        {
            lock (queue)
            {
                foreach (Reminder reminder in chat.Reminders)
                {
                    this.removeReminder(reminder, oldTimeZone);
                    this.addReminder(reminder);
                }
            }
        }

        void addReminder(Reminder reminder)
        {
            DateTime nextPushUtcDate = getNextPushUtcDate(reminder.DayTime, reminder.Chat.TimeZone);
            List<Reminder> list;
            if (!queue.TryGetValue(nextPushUtcDate, out list))
            {
                list = new List<Reminder>();
                queue.Add(nextPushUtcDate, list);
            }
            list.Add(reminder);
        }

        void removeReminder(Reminder reminder, TimeSpan timeZone)
        {
            DateTime nextPushUtcDate = getNextPushUtcDate(reminder.DayTime, timeZone);
            List<Reminder> list;
            if (queue.TryGetValue(nextPushUtcDate, out list))
            {
                list.Remove(list.First(r => r.ReminderId == reminder.ReminderId));
                if (list.Count == 0)
                {
                    queue.Remove(nextPushUtcDate);
                }
            }
        }

        DateTime getNextPushUtcDate(TimeSpan dayTime, TimeSpan timeZone)
        {
            DateTime currentLocalDate = this.currentDateUtc + timeZone;
            DateTime localNextPushDay;
            if (currentLocalDate.TimeOfDay > dayTime)
            {
                localNextPushDay = currentLocalDate.Date.AddDays(1);
            }
            else
            {
                localNextPushDay = currentLocalDate.Date;
            }
            return localNextPushDay.Add(dayTime).Subtract(timeZone);
        }

        bool toPushOnDay(Reminder reminder, DateTime dateUtc)
        {
            DateTime dateLocal = dateUtc + reminder.Chat.TimeZone;
            WeekDays weekDayLocal = 0;
            switch (dateLocal.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    weekDayLocal = WeekDays.Mon;
                    break;
                case DayOfWeek.Tuesday:
                    weekDayLocal = WeekDays.Tue;
                    break;
                case DayOfWeek.Wednesday:
                    weekDayLocal = WeekDays.Wed;
                    break;
                case DayOfWeek.Thursday:
                    weekDayLocal = WeekDays.Thu;
                    break;
                case DayOfWeek.Friday:
                    weekDayLocal = WeekDays.Fri;
                    break;
                case DayOfWeek.Saturday:
                    weekDayLocal = WeekDays.Sat;
                    break;
                case DayOfWeek.Sunday:
                    weekDayLocal = WeekDays.Sun;
                    break;
            }
            return (reminder.WeekDays & weekDayLocal) > 0;
        }
    }
}