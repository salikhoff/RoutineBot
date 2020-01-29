using System;
using System.Collections.Generic;
using RoutineBot.Repository.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;

namespace RoutineBot.Telegram.Conversations
{
    public class RemoveReminderConversation : IConversation
    {
        public bool Finished { get; private set; } = true;

        public async Task Initialize(ITelegramBotClient client, Update update)
        {
            long chatId = update.GetChatId();
            List<IEnumerable<InlineKeyboardButton>> buttons = new List<IEnumerable<InlineKeyboardButton>>();
            Repository.Model.Chat chat;
            if (Program.RemindersRepository.TryGetChat(chatId, out chat))
            {
                foreach (Reminder reminder in chat.Reminders.OrderBy(r => r.DayTime))
                {
                    buttons.Add(new List<InlineKeyboardButton>() { new InlineKeyboardButton() { Text = reminder.MessageText, CallbackData = reminder.ReminderId.ToString() } });
                }
            }
            buttons.Add(TelegramHelper.GetHomeButton());
            await client.SendTextMessageAsync(chatId, "Select reminder to remove", replyMarkup: new InlineKeyboardMarkup(buttons));
        }

        public async Task ProcessUpdate(ITelegramBotClient client, Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                long reminderId;
                if (long.TryParse(update.CallbackQuery.Data, out reminderId))
                {
                    long chatId = update.CallbackQuery.Message.Chat.Id;
                    Program.RemindersRepository.RemoveReminder(chatId, reminderId);
                    this.Finished = true;
                    await client.SendDefaultMessageAsync(chatId);
                }
            }
        }
    }
}