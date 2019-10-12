using System.Threading;
using System.Threading.Tasks;
using RoutineBot.Repository;
using Telegram.Bot;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Globalization;
using RoutineBot.Repository.Model;
using System.Linq;

namespace RoutineBot.Telegram
{
    public static class TelegramHelper
    {
        public const string AddReminderCommand = "AddReminder";
        public const string SetTimeZoneCommand = "SetTimeZone";
        public const string RemoveReminderCommand = "RemoveReminder";
        public const string HomeCommand = "Home";

        public static Message GetDefaultMessage(long chatId)
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Routine Bot");

            List<IEnumerable<InlineKeyboardButton>> buttons = new List<IEnumerable<InlineKeyboardButton>>();

            Chat chat;
            if (Program.RemindersRepository.TryGetChat(chatId, out chat))
            {
                InlineKeyboardButton addReminderButton = new InlineKeyboardButton() { Text = "Add reminder", CallbackData = AddReminderCommand };
                buttons.Add(new List<InlineKeyboardButton>() { addReminderButton });

                if (chat.Reminders.Count > 0)
                {
                    messageBuilder.AppendLine().AppendLine("Reminders:");
                    foreach (Reminder reminder in chat.Reminders)
                    {
                        string weekDaysString = string.Join(" | ", Enum.GetValues(typeof(WeekDays)).OfType<WeekDays>().Where(wd => (wd & reminder.WeekDays) > 0));
                        messageBuilder.Append(reminder.MessageText).Append(" (").Append(reminder.DayTime).Append(" ").Append(weekDaysString).Append(")").AppendLine();
                    }

                    InlineKeyboardButton removeReminderButton = new InlineKeyboardButton() { Text = "Remove reminder", CallbackData = RemoveReminderCommand };
                    buttons.Add(new List<InlineKeyboardButton>() { removeReminderButton });
                }
                else
                {
                    messageBuilder.AppendLine().AppendLine("No reminders");
                }

                messageBuilder.AppendLine().Append("Your timezone is: ").Append(chat.TimeZone < TimeSpan.Zero ? "-" : "+").Append(Math.Abs(chat.TimeZone.Hours)).Append(':').AppendLine(Math.Abs(chat.TimeZone.Minutes).ToString("00"));
            }
            else
            {
                messageBuilder.AppendLine().AppendLine("Timezone is not set");
            }

            InlineKeyboardButton timeZoneButton = new InlineKeyboardButton() { Text = "Set timezone", CallbackData = SetTimeZoneCommand };
            buttons.Add(new List<InlineKeyboardButton>() { timeZoneButton });


            return new Message() { Text = messageBuilder.ToString(), Keyboard = new InlineKeyboardMarkup(buttons) };

        }

        public static bool TryParseTime(string text, out TimeSpan t)
        {
            text = text.Trim().Replace(":", "").Replace(".", "").Replace(" ", "");
            if (text.Length == 3)
            {
                text = "0" + text;
            }
            if (text.Length == 4 && TimeSpan.TryParseExact(text, "hhmm", CultureInfo.InvariantCulture, out t))
            {
                return true;
            }
            t = TimeSpan.Zero;
            return false;
        }
    }

}