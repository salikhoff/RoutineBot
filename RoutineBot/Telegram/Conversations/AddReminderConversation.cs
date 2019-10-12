using System;
using System.Collections.Generic;
using RoutineBot.Repository.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RoutineBot.Telegram.Conversations
{
    public class AddReminderConversation : IConversation
    {
        const string SelectDaysDone = "SelectDaysDone";
        private State state = State.WaitingForMessageText;
        private Reminder reminder;
        public bool Finished { get; private set; } = false;

        public Message Initialize(long chatId)
        {
            if (Program.RemindersRepository.ChatExists(chatId))
            {
                this.reminder = new Reminder();
                this.reminder.ChatId = chatId;
                return new Message() { Text = "Enter new reminder name" };
            }
            return null;
        }

        public Message ProcessUpdate(Update update)
        {
            if (this.state == State.WaitingForMessageText)
            {
                if (update.Type == UpdateType.Message)
                {
                    this.reminder.MessageText = update.Message.Text;
                    this.state = State.WaitingForTime;
                    return new Message() { Text = $"Reminder text:\n{this.reminder.MessageText}\n\nEnter redinder time. Use 24-hour HHmm format." };
                }
            }
            else if (this.state == State.WaitingForTime)
            {
                if (update.Type == UpdateType.Message)
                {
                    string timeText = update.Message.Text;
                    TimeSpan time;
                    if (TelegramHelper.TryParseTime(timeText, out time))
                    {
                        this.reminder.DayTime = time;
                        this.state = State.WaitingForWeekDays;
                        return getWeekDaysMessage(this.reminder.WeekDays);
                    }
                    else
                    {
                        return new Message() { Text = "Could not parse time. Use 24-hour HHmm format." };
                    }
                }
            }
            else if (this.state == State.WaitingForWeekDays)
            {
                if (update.Type == UpdateType.CallbackQuery)
                {
                    if (update.CallbackQuery.Data == SelectDaysDone)
                    {
                        Program.RemindersRepository.StoreReminder(this.reminder);
                        this.Finished = true;
                        return null;
                    }
                    else
                    {
                        WeekDays wdSelected = (WeekDays)Enum.Parse(typeof(WeekDays), update.CallbackQuery.Data);
                        this.reminder.WeekDays ^= wdSelected;
                        return getWeekDaysMessage(this.reminder.WeekDays, update.CallbackQuery.Message.MessageId);
                    }
                }
            }
            return null;
        }

        private Message getWeekDaysMessage(WeekDays wd, int? updateMessageId = null)
        {
            List<IEnumerable<InlineKeyboardButton>> buttons = new List<IEnumerable<InlineKeyboardButton>>();

            List<InlineKeyboardButton> daysButtonsRow = new List<InlineKeyboardButton>();
            foreach (WeekDays day in Enum.GetValues(typeof(WeekDays)))
            {
                string dayName = Enum.GetName(typeof(WeekDays), day);
                bool dayChecked = (wd & day) > 0;
                string buttonText = (dayChecked ? "+" : "-") + dayName;
                daysButtonsRow.Add(new InlineKeyboardButton() { Text = buttonText, CallbackData = dayName });
            }
            buttons.Add(daysButtonsRow);

            buttons.Add(new List<InlineKeyboardButton>() { new InlineKeyboardButton() { Text = "Done", CallbackData = SelectDaysDone } });

            return new Message() { Text = "Select reminder week days", Keyboard = new InlineKeyboardMarkup(buttons), UpdateMessageId = updateMessageId };
        }

        private enum State
        {
            WaitingForMessageText,
            WaitingForTime,
            WaitingForWeekDays
        }
    }
}