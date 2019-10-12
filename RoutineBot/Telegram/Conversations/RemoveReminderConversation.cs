using System;
using System.Collections.Generic;
using RoutineBot.Repository.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Linq;

namespace RoutineBot.Telegram.Conversations
{
    public class RemoveReminderConversation : IConversation
    {
        public bool Finished { get; private set; } = true;

        public Message Initialize(long chatId)
        {
            List<IEnumerable<InlineKeyboardButton>> buttons = new List<IEnumerable<InlineKeyboardButton>>();
            Repository.Model.Chat chat;
            if (Program.RemindersRepository.TryGetChat(chatId, out chat))
            {
                foreach (Reminder reminder in chat.Reminders)
                {
                    buttons.Add(new List<InlineKeyboardButton>() { new InlineKeyboardButton() { Text = reminder.MessageText, CallbackData = reminder.ReminderId.ToString() } });
                }
            }
            buttons.Add(new List<InlineKeyboardButton>() { new InlineKeyboardButton() { Text = "Back", CallbackData = TelegramHelper.HomeCommand } });
            return new Message() { Text = "Select reminder to remove", Keyboard = new InlineKeyboardMarkup(buttons) };
        }

        public Message ProcessUpdate(Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                long reminderId;
                if (long.TryParse(update.CallbackQuery.Data, out reminderId))
                {
                    Program.RemindersRepository.RemoveReminder(update.CallbackQuery.Message.Chat.Id, reminderId);
                    this.Finished = true;
                }
            }
            return null;
        }
    }
}