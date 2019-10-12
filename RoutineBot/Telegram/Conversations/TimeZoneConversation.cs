using System;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace RoutineBot.Telegram.Conversations
{
    public class TimeZoneConversation : IConversation
    {
        public bool Finished { get; private set; } = false;

        public Message Initialize(long chatId)
        {
            InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton() { Text = "Back", CallbackData = TelegramHelper.HomeCommand });
            return new Message() { Text = "Enter your current local time to detect your timezone. Use 24-hour HHmm format.", Keyboard = keyboard };
        }

        public Message ProcessUpdate(Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                string timeText = update.Message.Text;
                TimeSpan time;
                if (TelegramHelper.TryParseTime(timeText, out time))
                {
                    int minutes = Convert.ToInt32((time - update.Message.Date.TimeOfDay).TotalMinutes / 15) * 15;
                    minutes = minutes > 720 ? minutes - 1440 : minutes < -720 ? minutes + 1440 : minutes;
                    TimeSpan timeZone = TimeSpan.FromMinutes(minutes);
                    Program.RemindersRepository.SetTimeZone(update.Message.Chat.Id, timeZone);
                    this.Finished = true;
                    return null;
                }
                else
                {
                    return new Message() { Text = "Could not parse time. Use 24-hour HHmm format." };
                }
            }
            return null;
        }
    }
}