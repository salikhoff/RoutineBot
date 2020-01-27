using System;
using System.Threading.Tasks;
using System.Threading;
using RoutineBot.Repository;
using Telegram.Bot;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RoutineBot.Telegram;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace RoutineBot
{
    class Program
    {
        public static ILoggerFactory LogFactory = LoggerFactory.Create((builder) => builder.AddConsole().AddFile("Logs/routinebot-{Date}.txt", fileSizeLimitBytes: 100 * 1024 * 1024));
        public static IRemindersRepository RemindersRepository { get; private set; }
        static ILogger logger = LogFactory.CreateLogger<Program>();
        static int Main(string[] args)
        {
            ManualResetEvent appEvent = new ManualResetEvent(false);
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                {
                    logger.LogInformation("Process exit");
                    cancellationTokenSource.Cancel();
                    appEvent.WaitOne();
                };
                IConfiguration configuration = (new ConfigurationBuilder()).AddUserSecrets(Assembly.GetExecutingAssembly()).Build();
                RemindersRepository = new Repository.DB.ReminderDbRepository();
                ITelegramBotClient telegramClient = new TelegramBotClient(configuration["token"]);
                Task[] backgroundTasks = new Task[]{
                    (new Telegram.MessageHandler(telegramClient)).HandleMessagesAsync(cancellationTokenSource.Token),
                    (new Telegram.NotificationSender(telegramClient)).SendNotificationsAsync(cancellationTokenSource.Token)
                };
                Task.WaitAll(backgroundTasks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
            finally
            {
                appEvent.Set();
            }
            return 0;
        }

    }
}
