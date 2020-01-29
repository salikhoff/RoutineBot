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
using Telegram.Bot.Types.Enums;

namespace RoutineBot
{
    class Program
    {
        public static ILoggerFactory LogFactory = LoggerFactory.Create((builder) => builder.AddConsole().AddFile("Logs/routinebot-{Date}.txt", fileSizeLimitBytes: 100 * 1024 * 1024));
        public static IRemindersRepository RemindersRepository { get; private set; }
        static ILogger logger = LogFactory.CreateLogger<Program>();
        static int Main(string[] args)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                {
                    logger.LogInformation("Process exit");
                    cancellationTokenSource.Cancel();
                };
                IConfiguration configuration = (new ConfigurationBuilder()).AddUserSecrets(Assembly.GetExecutingAssembly()).Build();
                RemindersRepository = new Repository.DB.ReminderDbRepository();
                ITelegramBotClient telegramClient = new TelegramBotClient(configuration["token"]);
                ConversationHolder conversationHolder = new ConversationHolder();
                telegramClient.OnUpdate += conversationHolder.OnUpdate;
                telegramClient.OnReceiveError += conversationHolder.OnApiError;
                telegramClient.OnReceiveGeneralError += conversationHolder.OnError;
                telegramClient.StartReceiving(new UpdateType[] { UpdateType.Message, UpdateType.CallbackQuery }, cancellationTokenSource.Token);

                NotificationSender notificationSender = new NotificationSender(telegramClient);
                notificationSender.SendNotificationsAsync(cancellationTokenSource.Token);
                
                cancellationTokenSource.Token.WaitHandle.WaitOne();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
            return 0;
        }

    }
}
