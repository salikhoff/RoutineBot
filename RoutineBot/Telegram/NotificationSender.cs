using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RoutineBot.Repository.Model;
using Telegram.Bot;

namespace RoutineBot.Telegram
{
    public class NotificationSender
    {
        readonly RemindersQueue remindersQueue;
        readonly ITelegramBotClient client;
        public static ILogger logger = Program.LogFactory.CreateLogger<NotificationSender>();

        public NotificationSender(ITelegramBotClient client)
        {
            this.client = client;
            this.remindersQueue = new RemindersQueue();
        }

        public async Task SendNotificationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Start sending notifications");
                while (!cancellationToken.IsCancellationRequested)
                {
                    IEnumerable<Reminder> reminders = await this.remindersQueue.WaitForReminders(cancellationToken);
                    Task.WaitAll(reminders.Select(r => sendReminder(r, cancellationToken)).ToArray());
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
                logger.LogInformation("Stopped sending notifications");
            }
        }

        async Task sendReminder(Reminder r, CancellationToken cancellationToken)
        {
            try
            {
                await this.client.SendTextMessageAsync(r.ChatId, r.MessageText, cancellationToken: cancellationToken);
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                logger.LogError(ex.ToString());
            }
        }
    }
}