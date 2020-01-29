using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RoutineBot.Repository.Model;
using Telegram.Bot;
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

        public async Task Initialize(ITelegramBotClient client, Update update)
        {
            long chatId = update.GetChatId();
            if (Program.RemindersRepository.ChatExists(chatId))
            {
                this.reminder = new Reminder();
                this.reminder.ChatId = chatId;
                await client.SendTextMessageAsync(chatId, "Enter new reminder name");
            }
        }

        public async Task ProcessUpdate(ITelegramBotClient client, Update update)
        {
            long chatId = update.GetChatId();
            if (this.state == State.WaitingForMessageText)
            {
                if (update.Type == UpdateType.Message)
                {
                    this.reminder.MessageText = update.Message.Text;
                    this.state = State.WaitingForTime;
                    await client.SendTextMessageAsync(chatId, $"Reminder text:\n{this.reminder.MessageText}\n\nEnter redinder time. Use 24-hour HHmm format.");
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
                        await sendWeekDaysMessage(client, chatId, this.reminder.WeekDays);
                    }
                    else
                    {
                        await client.SendTextMessageAsync(chatId, "Could not parse time. Use 24-hour HHmm format.");
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
                        await client.SendDefaultMessageAsync(chatId);
                    }
                    else
                    {
                        WeekDays wdSelected = (WeekDays)Enum.Parse(typeof(WeekDays), update.CallbackQuery.Data);
                        this.reminder.WeekDays ^= wdSelected;
                        await sendWeekDaysMessage(client, chatId, this.reminder.WeekDays, update.CallbackQuery.Message.MessageId);
                    }
                }
            }
        }

        private async Task sendWeekDaysMessage(ITelegramBotClient client, long chatId, WeekDays wd, int? updateMessageId = null)
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

            string text = "Select reminder week days";
            InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(buttons);
            if (updateMessageId == null)
            {
                await client.SendTextMessageAsync(chatId, text, replyMarkup: keyboard);
            }
            else
            {
                await client.EditMessageTextAsync(chatId, updateMessageId.Value, text, replyMarkup: keyboard);
            }
        }

        private enum State
        {
            WaitingForMessageText,
            WaitingForTime,
            WaitingForWeekDays
        }
    }
}