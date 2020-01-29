using System;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Threading.Tasks;
using Telegram.Bot;

namespace RoutineBot.Telegram.Conversations
{
    public class TimeZoneConversation : IConversation
    {
        public bool Finished { get; private set; } = false;

        public async Task Initialize(ITelegramBotClient client, Update update)
        {
            await client.SendTextMessageAsync(update.GetChatId(),
                "Enter your current local time to detect your timezone. Use 24-hour HHmm format.",
                replyMarkup: TelegramHelper.GetHomeButtonKeyboard());
        }

        public async Task ProcessUpdate(ITelegramBotClient client, Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                string timeText = update.Message.Text;
                TimeSpan time;
                if (TelegramHelper.TryParseTime(timeText, out time))
                {
                    long chatId = update.Message.Chat.Id;
                    int minutes = Convert.ToInt32((time - update.Message.Date.TimeOfDay).TotalMinutes / 15) * 15;
                    minutes = minutes > 720 ? minutes - 1440 : minutes < -720 ? minutes + 1440 : minutes;
                    TimeSpan timeZone = TimeSpan.FromMinutes(minutes);
                    Program.RemindersRepository.SetTimeZone(chatId, timeZone);
                    this.Finished = true;
                    await client.SendDefaultMessageAsync(chatId);
                }
                else
                {
                    await client.SendTextMessageAsync(update.GetChatId(), "Could not parse time. Use 24-hour HHmm format.");
                }
            }
        }
    }
}