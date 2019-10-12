using System.Threading;
using System.Threading.Tasks;
using RoutineBot.Repository;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace RoutineBot.Telegram
{
    public class MessageHandler
    {
        readonly ITelegramBotClient client;
        readonly ConversationHolder conversationHolder;
        public static ILogger logger = Program.LogFactory.CreateLogger<MessageHandler>();

        public MessageHandler(ITelegramBotClient client)
        {
            this.client = client;
            this.conversationHolder = new ConversationHolder();
        }

        public async Task HandleMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Start handling messages");
                int currentOffset = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    Update[] updates = await this.client.GetUpdatesAsync(offset: currentOffset, cancellationToken: cancellationToken);
                    if (updates != null && updates.Length > 0)
                    {
                        Task.WaitAll(updates.Select((u) => handleUpdateAsync(u, cancellationToken)).ToArray());
                        currentOffset = updates[updates.Length - 1].Id + 1;
                    }
                    else
                    {
                        await Task.Delay(500, cancellationToken);
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
            finally
            {
                logger.LogInformation("Stopped handling messages");
            }
        }

        private async Task handleUpdateAsync(Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.CallbackQuery || update.Type == UpdateType.Message)
                {
                    long chatId;
                    Message message = null;
                    if (update.Type == UpdateType.CallbackQuery)
                    {
                        chatId = update.CallbackQuery.Message.Chat.Id;
                        if (update.CallbackQuery.Data == TelegramHelper.HomeCommand)
                        {
                            message = TelegramHelper.GetDefaultMessage(chatId);
                        }
                        else
                        {
                            this.conversationHolder.TryCreateConversation(chatId, update.CallbackQuery.Data, out message);
                        }
                    }
                    else if (update.Type == UpdateType.Message)
                    {
                        chatId = update.Message.Chat.Id;
                    }
                    else
                    {
                        throw new Exception("Unsupported update type");
                    }
                    if (message == null)
                    {
                        message = this.conversationHolder.ProcessMessage(chatId, update) ?? TelegramHelper.GetDefaultMessage(chatId);
                    }
                    if (message.UpdateMessageId == null)
                    {
                        await this.client.SendTextMessageAsync(chatId, message.Text, replyMarkup: message.Keyboard, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await this.client.EditMessageTextAsync(chatId, message.UpdateMessageId.Value, message.Text, replyMarkup: (InlineKeyboardMarkup)message.Keyboard, cancellationToken: cancellationToken);
                    }
                }
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

    }
}